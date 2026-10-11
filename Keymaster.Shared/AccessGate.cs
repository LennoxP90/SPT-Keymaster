namespace Keymaster.Shared
{
  /// <summary>The raid-start card gate, as the map screen's own check (MainMenuShowOperation.method_53) applies it.</summary>
  public static class AccessGate
  {
    /// <summary>True when a PMC raid must not start: the map has access keys, the profile is at or above the map's
    /// minimum level for them (none set means always), and no card is worn.</summary>
    public static bool MustBlock(int accessKeyCount, int level, int? minLevel, bool cardWorn) =>
      accessKeyCount > 0 && !(level < minLevel) && !cardWorn;
  }
}
