using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Keymaster.Shared;

namespace Keymaster.Client
{
  [BepInPlugin("com.lennoxp90.keymaster", "Keymaster", "0.1.0")]
  public class Plugin : BaseUnityPlugin
  {
    static ManualLogSource _log;

    void Awake()
    {
      _log = Logger;
      KeymasterSettings.Bind(Config);
      if (!KeymasterApi.Refresh()) { Log("server half not found; Keymaster is off"); return; }
      new LearnPatch().Enable();
      new ArmPatch().Enable();
      new FlushPatch().Enable();
      new GameEndPatch().Enable();
      new MenuBackPatch().Enable();
      new ContextMenuPatch().Enable();
      new AccessKeyPatch().Enable();
      new UI.OpenItemPatch().Enable();
      new UI.CloseWindowsPatch().Enable();
      new UI.MergedUsesPatch().Enable();
    }

    public static void Log(string line) => _log.LogInfo("Keymaster: " + line);
    public static void Warn(string line) => _log.LogWarning("Keymaster: " + line);
    public static void Detail(string line) { if (KeymasterSettings.Verbose.Value) _log.LogInfo("Keymaster: " + line); }
  }

  public static class KeymasterSettings
  {
    public static ConfigEntry<int> Rub, Usd, Eur, Gp;
    public static ConfigEntry<CopyChoice> Copy;
    public static ConfigEntry<bool> Verbose;

    public static void Bind(ConfigFile c)
    {
      Rub = c.Bind("Money", "Roubles target", 0, new ConfigDescription("Topped up before each PMC raid", new AcceptableValueRange<int>(0, 1000000)));
      Usd = c.Bind("Money", "Dollars target", 0, new ConfigDescription("", new AcceptableValueRange<int>(0, 50000)));
      Eur = c.Bind("Money", "Euros target", 0, new ConfigDescription("", new AcceptableValueRange<int>(0, 50000)));
      Gp = c.Bind("Money", "GP coins target", 0, new ConfigDescription("", new AcceptableValueRange<int>(0, 100)));
      Copy = c.Bind("Keys", "Key copy", CopyChoice.Freshest, "Which copy goes into the case when you own several");
      Verbose = c.Bind("Debug", "Verbose logging", false, "One log line per moved item");
    }

    public static int Target(string currencyTpl) =>
      currencyTpl == KeymasterIds.Rub ? Rub.Value : currencyTpl == KeymasterIds.Usd ? Usd.Value : currencyTpl == KeymasterIds.Eur ? Eur.Value : Gp.Value;
  }
}
