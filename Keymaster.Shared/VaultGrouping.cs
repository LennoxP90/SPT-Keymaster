using System;
using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  public sealed class VaultGroup
  {
    public string Title { get; set; } = "";
    public List<string> Tpls { get; set; } = new List<string>();
  }

  public static class VaultGrouping
  {
    public const string Other = "Other";

    /// <summary>Each template under its home map's entry, in table order; within a group, table order. A home with no table entry gets its own group after the table groups. Keys with no home go to Other. A template renders once.</summary>
    public static List<VaultGroup> Group(IEnumerable<string> tpls, IReadOnlyDictionary<string, List<string>> table, IReadOnlyDictionary<string, string> homes)
    {
      var list = tpls.Distinct().ToList();
      var remaining = new HashSet<string>(list);
      var groups = new List<VaultGroup>();
      foreach (var entry in table)
      {
        var mine = entry.Value.Where(t => remaining.Contains(t) && homes.TryGetValue(t, out var h) && h == entry.Key).Distinct().ToList();
        foreach (var t in mine) remaining.Remove(t);
        if (mine.Count > 0) groups.Add(new VaultGroup { Title = entry.Key, Tpls = mine });
      }
      foreach (var home in list.Where(remaining.Contains).Select(t => homes.TryGetValue(t, out var h) ? h : null).Where(h => h != null).Distinct().ToList())
      {
        var mine = list.Where(t => remaining.Contains(t) && homes.TryGetValue(t, out var h) && h == home).ToList();
        foreach (var t in mine) remaining.Remove(t);
        groups.Add(new VaultGroup { Title = home!, Tpls = mine });
      }
      if (remaining.Count > 0) groups.Add(new VaultGroup { Title = Other, Tpls = list.Where(remaining.Contains).ToList() });
      return groups;
    }
  }

  /// <summary>Vault headers: the game's own map name for each entry; a variant shows its base map's name.</summary>
  public static class MapTitle
  {
    /// <param name="mapName">The game's localized name for a location id, or null when it has none.</param>
    public static string For(string entry, Func<string, string?> mapName)
    {
      if (entry == VaultGrouping.Other) return entry;
      var at = entry.IndexOf('@');
      var id = at < 0 ? entry : entry.Substring(0, at);
      return mapName(id) ?? id;
    }

    /// <summary>Groups renamed for display; groups that end up with the same name merge, in first-seen order, each key once.</summary>
    public static List<VaultGroup> Named(IEnumerable<VaultGroup> groups, Func<string, string?> mapName) =>
      groups.GroupBy(g => For(g.Title, mapName))
        .Select(n => new VaultGroup { Title = n.Key, Tpls = n.SelectMany(g => g.Tpls).Distinct().ToList() })
        .ToList();
  }
}
