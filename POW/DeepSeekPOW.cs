using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DeepSeekAPI.POW;

public class DeepSeekPOW
{
    private readonly DeepSeekHash _hasher;

    public DeepSeekPOW(string wasmPath) => _hasher = new DeepSeekHash(wasmPath);

    public string SolveChallenge(PowRequest config)
    {
        long? answer = _hasher.CalculateHash(
            config.algorithm,
            config.challenge,
            config.salt,
            config.difficulty,
            config.expire_at
        );

        var result = new Dictionary<string, object>
        {
            ["algorithm"] = config.algorithm,
            ["challenge"] = config.challenge,
            ["salt"] = config.salt,
            ["answer"] = answer!,
            ["signature"] = config.signature,
            ["target_path"] = config.target_path
        };

        var json = JsonSerializer.Serialize(result);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}