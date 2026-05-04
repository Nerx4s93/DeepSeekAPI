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
        var answer = _hasher.CalculateHash(
            config.Algorithm,
            config.Challenge,
            config.Salt,
            config.Difficulty,
            config.ExpireAt
        );

        if (answer is null)
        {
            throw new PowFailedException();
        }

        var result = new PowResponse
        {
            Algorithm = config.Algorithm,
            Challenge = config.Challenge,
            Salt = config.Salt,
            Answer = answer.Value,
            Signature = config.Signature,
            TargetPath = config.TargetPath
        };

        var json = JsonSerializer.Serialize(result);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}