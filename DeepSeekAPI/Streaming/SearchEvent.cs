using DeepSeekAPI.Models;
using System.Collections.Generic;

namespace DeepSeekAPI.Streaming;

public record SearchEvent(
    List<string> Queries,
    List<SearchResult>? Results
) : DeepSeekEvent;