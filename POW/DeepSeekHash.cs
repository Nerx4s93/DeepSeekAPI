using System;
using System.IO;
using System.Text;
using Wasmtime;

namespace DeepSeekAPI.POW;

public class DeepSeekHash : IDisposable
{
    private readonly Engine _engine;
    private readonly Store _store;
    private readonly Linker _linker;
    private readonly Instance _instance;
    private readonly Memory _memory;

    private readonly Func<int, int, int> _malloc;
    private readonly Func<int, int> _addStack;
    private readonly Function _wasmSolve;

    public DeepSeekHash(string wasmPath)
    {
        _engine = new Engine();
        _store = new Store(_engine);
        _linker = new Linker(_engine);

        _linker.DefineWasi();

        var module = Module.FromFile(_engine, wasmPath);
        _instance = _linker.Instantiate(_store, module);

        _memory = _instance.GetMemory("memory")!;

        _malloc = _instance.GetFunction<int, int, int>("__wbindgen_export_0")!;
        _addStack = _instance.GetFunction<int, int>("__wbindgen_add_to_stack_pointer")!;
        _wasmSolve = _instance.GetFunction("wasm_solve")!;
    }

    private (int ptr, int len) WriteToMemory(string text)
    {
        var encoded = Encoding.UTF8.GetBytes(text);
        var length = encoded.Length;

        var ptr = _malloc(length, 1);
        var mem = _memory.GetSpan<byte>(0);

        encoded.CopyTo(mem.Slice(ptr, length));
        return (ptr, length);
    }

    public long? CalculateHash(string algorithm, string challenge, string salt,
                              int difficulty, int expireAt)
    {
        var prefix = $"{salt}_{expireAt}_";
        var retptr = _addStack(-16);

        try
        {
            var (challengePtr, challengeLen) = WriteToMemory(challenge);
            var (prefixPtr, prefixLen) = WriteToMemory(prefix);

            _wasmSolve.Invoke(
                retptr,
                challengePtr,
                challengeLen,
                prefixPtr,
                prefixLen,
                (float)difficulty
            );

            var mem = _memory.GetSpan<byte>(0);
            var status = BitConverter.ToInt32(mem.Slice(retptr, 4));

            if (status == 0)
            {
                return null;
            }

            var value = BitConverter.ToDouble(mem.Slice(retptr + 8, 8));

            return (long)value;
        }
        finally
        {
            _addStack(16);
        }
    }

    public void Dispose()
    {
        _store?.Dispose();
        _engine?.Dispose();
    }
}