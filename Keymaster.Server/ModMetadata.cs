using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;

namespace Keymaster.Server;

// IModBlazorMetadata puts the Keymaster card in MOD PAGES and routes the page in Keymaster.Server.Web.
public record ModMetadata : IModMetadata, IModBlazorMetadata
{
  public string ModGuid { get; init; } = "com.lennoxp90.keymaster.server";
  public string Name { get; init; } = "Keymaster";
  public string Author { get; init; } = "LennoxP90";
  public List<string>? Contributors { get; init; }
  public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
  public SemanticVersioning.Range SptVersion { get; init; } = new(">=4.1.3 <4.2.0");
  public bool HasPrepatcher { get; init; } = false;
  public List<string>? Incompatibilities { get; init; }
  public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
  public string? Url { get; init; } = "https://github.com/LennoxP90/SPT-Keymaster";
  public string License { get; init; } = "MIT";

  public string? WWWRootUrl { get; init; }
  public string? HomePage { get; init; } = "/keymaster";
  public string? HomePageDescription { get; init; } = "Configure Keymaster and manage KM Vaults";
}
