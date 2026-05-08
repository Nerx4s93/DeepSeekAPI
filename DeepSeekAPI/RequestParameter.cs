using System;
using System.Collections.Generic;

namespace DeepSeekAPI;

internal class RequestParameter
{
    public required string Name { get; set; }
    public object? Value { get; set; }
    public object? DefaultValue { get; set; }

    public bool ShouldInclude => Value != null && !Value.Equals(DefaultValue);

    public string GetQueryString()
    {
        switch (Value)
        {
            case Enum enumValue:
                {
                    return $"{Name}={enumValue.ToString().ToLower()}";
                }
            case List<int> intList:
                {
                    var ids = string.Join(",", intList);
                    return $"{Name}={Uri.EscapeDataString(ids)}";
                }
            case List<string> stringList:
                {
                    var ids = string.Join(",", stringList);
                    return $"{Name}={Uri.EscapeDataString(ids)}";
                }
            default:
                {
                    return $"{Name}={Uri.EscapeDataString(Value?.ToString() ?? "")}";
                }
        }
    }
}