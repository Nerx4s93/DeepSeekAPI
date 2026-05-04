using System;

namespace DeepSeekAPI.POW.Exceptions;

public class PowFailedException() : Exception("Proof of Work validation failed (WASM returned null)");