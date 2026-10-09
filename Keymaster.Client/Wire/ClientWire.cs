using System.Collections.Generic;
using Newtonsoft.Json;

namespace Keymaster.Client.Wire
{
  public sealed class ConfigResponseC
  {
    [JsonProperty("table")] public Dictionary<string, List<string>> Table { get; set; } = new Dictionary<string, List<string>>();
  }

  public sealed class StateDtoC
  {
    [JsonProperty("currentMap")] public string? CurrentMap { get; set; }
    [JsonProperty("alwaysBring")] public List<string> AlwaysBring { get; set; } = new List<string>();
  }

  public sealed class LearnRequestC
  {
    [JsonProperty("location")] public string Location { get; set; } = "";
    [JsonProperty("variant")] public string? Variant { get; set; }
    [JsonProperty("eftBuild")] public string EftBuild { get; set; } = "";
    [JsonProperty("keys")] public List<string> Keys { get; set; } = new List<string>();
  }
}
