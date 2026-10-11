namespace Keymaster.Shared
{
  /// <summary>Bundle keys of the two steel navy models. A key is both the item's Prefab.path and its bundles.json entry.</summary>
  public static class ModelBundles
  {
    public const string Prefix = "keymaster/";
    public const string CasePath = Prefix + "km_case.bundle";
    public const string VaultPath = Prefix + "km_vault.bundle";

    public static bool IsOurs(string path) => path != null && path.StartsWith(Prefix, System.StringComparison.Ordinal);
  }
}
