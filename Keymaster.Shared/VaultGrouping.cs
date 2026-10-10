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
}
