using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Keymaster.Server.Wire
{
  public class ConfigResponse
  {
    [JsonPropertyName("table")] public Dictionary<string, List<string>> Table { get; set; } = new();
    [JsonPropertyName("homes")] public Dictionary<string, string> Homes { get; set; } = new();
  }

  public class StateDto
  {
    [JsonPropertyName("currentMap")] public string? CurrentMap { get; set; }
    [JsonPropertyName("alwaysBring")] public List<string> AlwaysBring { get; set; } = new();
  }

  public class LearnRequest
  {
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("variant")] public string? Variant { get; set; }
    [JsonPropertyName("eftBuild")] public string EftBuild { get; set; } = "";
    [JsonPropertyName("keys")] public List<string> Keys { get; set; } = new();
  }
}
