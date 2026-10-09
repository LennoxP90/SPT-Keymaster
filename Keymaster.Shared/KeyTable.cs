using System;
using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  public sealed class KeyTableInput
  {
    public KeysFile? Seed { get; set; }
    public KeysFile? Learned { get; set; }
    public ISet<string> KnownTpls { get; set; } = new HashSet<string>();
    /// <summary>Base location -> locations it can transit to.</summary>
    public IDictionary<string, List<string>> TransitTargets { get; set; } = new Dictionary<string, List<string>>();
    /// <summary>Base location -> its own AccessKeys.</summary>
    public IDictionary<string, List<string>> AccessKeys { get; set; } = new Dictionary<string, List<string>>();
  }

  public static class KeyTable
  {
    public static string EntryKey(string location, string? variant)
    {
      var loc = location.Trim().ToLowerInvariant();
      return string.IsNullOrWhiteSpace(variant) ? loc : loc + "@" + variant!.Trim().ToLowerInvariant();
    }

    static string BaseOf(string entryKey)
    {
      var at = entryKey.IndexOf('@');
      return at < 0 ? entryKey : entryKey.Substring(0, at);
    }

    public static Dictionary<string, List<string>> Build(KeyTableInput input)
    {
      var merged = new Dictionary<string, List<string>>();
      foreach (var file in new[] { input.Seed, input.Learned })
      {
        if (file == null) continue;
        foreach (var e in file.Entries) merged[e.Key] = e.Value.Keys.ToList();
      }

      var table = new Dictionary<string, List<string>>();
      foreach (var e in merged)
      {
        var keys = new List<string>();
        void Add(string tpl) { if (input.KnownTpls.Contains(tpl) && !keys.Contains(tpl)) keys.Add(tpl); }

        foreach (var tpl in e.Value) Add(tpl);
        var baseLoc = BaseOf(e.Key);
        if (input.AccessKeys.TryGetValue(baseLoc, out var own)) foreach (var tpl in own) Add(tpl);
        if (input.TransitTargets.TryGetValue(baseLoc, out var targets))
          foreach (var target in targets)
            if (input.AccessKeys.TryGetValue(target.ToLowerInvariant(), out var cards))
              foreach (var tpl in cards) Add(tpl);

        table[e.Key] = keys;
      }
      return table;
    }

    public static IReadOnlyList<string> Resolve(IReadOnlyDictionary<string, List<string>> table, string location, string? variant)
    {
      if (table.TryGetValue(EntryKey(location, variant), out var exact)) return exact;
      if (!string.IsNullOrWhiteSpace(variant) && table.TryGetValue(EntryKey(location, null), out var fallback)) return fallback;
      return Array.Empty<string>();
    }
  }
}
