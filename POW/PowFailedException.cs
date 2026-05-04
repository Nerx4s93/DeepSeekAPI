using System;

namespace DeepSeekAPI.POW;

public class PowFailedException() : Exception("Proof of Work validation failed (WASM returned null)");