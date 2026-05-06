using System;

namespace DeepSeekAPI.Exceptions;

public class DeepSeekError(string message) : Exception(message);