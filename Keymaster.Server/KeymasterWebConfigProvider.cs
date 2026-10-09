using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Web.Models.Configs;
using SPTarkov.Server.Web.Services;

namespace Keymaster.Server;

/// <summary>Puts config/config.json into the server's web Config Editor, one registration per section.</summary>
[Injectable(InjectionType.Singleton)]
public class KeymasterWebConfigProvider(ISptLogger<KeymasterWebConfigProvider> logger, KeymasterServerConfig config, KeymasterTraderAssort traderAssort) : IConfigEditorConfigProvider
{
  public IEnumerable<ConfigEditorConfigRegistration> GetConfigs()
  {
    yield return Build("keymaster-trader", "Keymaster - Therapist offer (applies immediately)", ["/gift"]);
    yield return Build("keymaster-gift", "Keymaster - vault gift (applies on next login)", ["/trader"]);
  }

  ConfigEditorConfigRegistration Build(string id, string displayName, string[] hidden) => new()
  {
    Id = id,
    DisplayName = displayName,
    RuntimeConfig = config.ToFile(),
    RuntimeType = typeof(KeymasterConfigFile),
    FilePath = config.FilePath,
    FileName = "config.json",
    IgnoredSectionPaths = new HashSet<string>(hidden),
    LoadFromDiskAsync = _ => new ValueTask<object?>(config.ReadFromDisk()),
    SaveToDiskAsync = SaveAsync,
    ApplyToRuntimeAsync = ApplyAsync,
  };

  async ValueTask SaveAsync(object edited, CancellationToken cancellationToken)
  {
    if (!await config.SaveToDiskAsync(AsFile(edited), cancellationToken))
      logger.Error("Keymaster: no config path resolved, the web editor cannot save");
  }

  ValueTask ApplyAsync(object edited, CancellationToken cancellationToken)
  {
    config.Apply(AsFile(edited));
    traderAssort.ApplyConfig();
    return default;
  }

  static KeymasterConfigFile AsFile(object edited) =>
    edited as KeymasterConfigFile
    ?? JsonSerializer.Deserialize<KeymasterConfigFile>(JsonSerializer.Serialize(edited))
    ?? new KeymasterConfigFile();
}
