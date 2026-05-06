namespace DeepSeekAPI.Models.Chat;

public class ChatSettings
{
    public ModelType ModelType { get; set; } = ModelType.Default;
    public bool Thinking { get; set; }
    public bool Search { get; set; }
}
