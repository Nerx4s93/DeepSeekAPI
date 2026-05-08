using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepSeekAPI;

public class QueryParametersBuilder
{
    private readonly List<RequestParameter> _parameters = [];

    public QueryParametersBuilder AddParameter(string name, object value, object? defaultValue = null)
    {
        _parameters.Add(new RequestParameter
        {
            Name = name,
            Value = value,
            DefaultValue = defaultValue
        });
        return this;
    }

    public QueryParametersBuilder AddParameterIf(bool condition, string name, object value, object? defaultValue = null)
    {
        if (condition)
        {
            AddParameter(name, value, defaultValue);
        }
        return this;
    }

    public string Build()
    {
        var validParameters = _parameters
            .Where(p => p.ShouldInclude)
            .Select(p => p.GetQueryString())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (validParameters.Count == 0)
        {
            return string.Empty;
        }

        var queryString = new StringBuilder("?");
        queryString.Append(string.Join("&", validParameters));
        return queryString.ToString();
    }

    public static QueryParametersBuilder Create()
    {
        return new QueryParametersBuilder();
    }
}