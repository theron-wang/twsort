using System.Text.Json.Serialization;

namespace TWSort.Project;

public class Subvariant
{
    [JsonPropertyName("ss")]
    public string Stem { get; set; } = null!;
    [JsonPropertyName("v")]
    public List<string> Variants { get; set; } = null!;
}