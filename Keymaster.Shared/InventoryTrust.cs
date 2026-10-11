namespace Keymaster.Shared
{
  /// <summary>Whether the client's inventory reads can be trusted enough to move anything.</summary>
  public static class InventoryTrust
  {
    public const string BrokenWarning =
      "the game's JSON defaults are missing (JsonConvert.DefaultSettings is null), so item counts read wrong; " +
      "Keymaster moves nothing this session. Another plugin likely failed to load: see the Unity Player.log " +
      "for a TypeInitializationException in UnityConverterInitializer";

    /// <summary>Null when trusted, else the one warning to log before standing down.</summary>
    public static string StandDown(bool jsonDefaultsSet) => jsonDefaultsSet ? null : BrokenWarning;
  }
}
