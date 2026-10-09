using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;

namespace Keymaster.Server;

/// <summary>config/config.json. Singleton so consumers share one read; an unreadable file keeps the defaults.</summary>
[Injectable(InjectionType.Singleton)]
public class KeymasterServerConfig
{
  static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

  readonly ISptLogger<KeymasterServerConfig> logger;
  readonly ModHelper modHelper;

  public KeymasterTraderSettings Trader { get; } = new();
  public KeymasterGiftSettings Gift { get; } = new();
  public string FilePath { get; } = string.Empty;

  public KeymasterServerConfig(ISptLogger<KeymasterServerConfig> logger, ModHelper modHelper)
  {
    this.logger = logger;
    this.modHelper = modHelper;
    try
    {
      FilePath = Path.Combine(modHelper.GetAbsolutePathToModFolder(typeof(KeymasterServerConfig).Assembly), "config", "config.json");
    }
    catch (Exception ex)
    {
      logger.Error("Keymaster: could not locate the mod folder, using built-in defaults.", ex);
      return;
    }
    Apply(ReadFromDisk());
  }

  public KeymasterConfigFile ReadFromDisk()
  {
    if (string.IsNullOrEmpty(FilePath)) return ToFile();
    try
    {
      var file = modHelper.GetJsonDataFromFile<KeymasterConfigFile>(Path.GetDirectoryName(FilePath)!, Path.GetFileName(FilePath));
      return new KeymasterConfigFile
      {
        Trader = file?.Trader ?? new KeymasterTraderSettings(),
        Gift = file?.Gift ?? new KeymasterGiftSettings(),
      };
    }
    catch (Exception ex)
    {
      logger.Error("Keymaster: could not read config/config.json, keeping the current settings.", ex);
      return ToFile();
    }
  }

  public void Apply(KeymasterConfigFile file)
  {
    Trader.PriceRoubles = file.Trader.PriceRoubles;
    Trader.LoyaltyLevel = file.Trader.LoyaltyLevel;
    Gift.Enabled = file.Gift.Enabled;
    Gift.Resend = file.Gift.Resend;
  }

  public async Task<bool> SaveToDiskAsync(KeymasterConfigFile file, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrEmpty(FilePath)) return false;
    await File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(file, WriteOptions), cancellationToken);
    return true;
  }

  public KeymasterConfigFile ToFile() => new() { Trader = Trader, Gift = Gift };
}

public class KeymasterConfigFile
{
  [JsonPropertyName("trader")] public KeymasterTraderSettings Trader { get; set; } = new();
  [JsonPropertyName("gift")] public KeymasterGiftSettings Gift { get; set; } = new();
}

public class KeymasterTraderSettings
{
  [JsonPropertyName("priceRoubles")] public int PriceRoubles { get; set; } = 2000000;
  [JsonPropertyName("loyaltyLevel")] public int LoyaltyLevel { get; set; } = 1;
}

public class KeymasterGiftSettings
{
  [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
  [JsonPropertyName("resend")] public bool Resend { get; set; } = true;
}
