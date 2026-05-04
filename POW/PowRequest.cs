namespace DeepSeekAPI.POW;

public class PowRequest
{
    public string algorithm { get; set; } = string.Empty;
    public string challenge { get; set; } = string.Empty;
    public string salt { get; set; } = string.Empty;
    public string signature { get; set; } = string.Empty;
    public int difficulty { get; set; }
    public long expire_at { get; set; }
    public string target_path { get; set; } = string.Empty;
}