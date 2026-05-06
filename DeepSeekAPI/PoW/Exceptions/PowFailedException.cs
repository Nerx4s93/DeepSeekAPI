using System;

namespace DeepSeekAPI.PoW.Exceptions;

public class PowFailedException() : Exception("Proof of Work validation failed (WASM returned null)");