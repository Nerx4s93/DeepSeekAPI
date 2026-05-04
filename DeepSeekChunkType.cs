namespace DeepSeekAPI;

public enum DeepSeekChunkType
{
    Text,           // обычный текст (append)
    SearchStatus,   // SEARCHING / FINISHED
    SearchResults,  // список ссылок
    State,          // полный update (v.response...)
    Meta,           // updated_at, request ids
    Unknown
}