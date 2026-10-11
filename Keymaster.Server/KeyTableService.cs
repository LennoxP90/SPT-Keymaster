using System.Text.Json;
using Keymaster.Server.Wire;
using Keymaster.Shared;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;

namespace Keymaster.Server;

[Injectable(InjectionType.Singleton, TypePriority = KeymasterLoadOrder.KeyTable)]
public class KeyTableService(ISptLogger<KeyTableService> logger, KeymasterStore store, KmTemplateTable templates, KmLocationTable locations) : IOnLoad
{
  static readonly JsonSerializerOptions SeedOptions = new() { PropertyNameCaseInsensitive = true };

  readonly object gate = new();
  KeysFile seed = new();
  public IReadOnlyDictionary<string, List<string>> Table { get; private set; } = new Dictionary<string, List<string>>();
  public IReadOnlyDictionary<string, string> Homes { get; private set; } = new Dictionary<string, string>();

  public Task OnLoadAsync(CancellationToken cancellationToken)
  {
    var seedPath = Path.Combine(store.ModFolder, "db", "keys-seed.json");
    if (File.Exists(seedPath))
      seed = JsonSerializer.Deserialize<KeysFile>(File.ReadAllText(seedPath), SeedOptions) ?? new KeysFile();
    Rebuild();
    var learned = store.ReadLearned();
    var build = seed.Entries.Values.Select(e => e.EftBuild).FirstOrDefault() ?? "none";
    logger.Info($"Keymaster: {Table.Values.SelectMany(v => v).Distinct().Count()} keys across {Table.Count} maps (seed {build}, {learned.Entries.Count} learned)");
    return Task.CompletedTask;
  }

  public void Learn(LearnRequest req)
  {
    lock (gate)
    {
      var key = KeyTable.EntryKey(req.Location, req.Variant);
      var learned = store.ReadLearned();
      var before = learned.Entries.TryGetValue(key, out var old) ? old.Keys : seed.Entries.GetValueOrDefault(key)?.Keys;
      if (req.Keys == null || req.Keys.Count == 0) { logger.Debug($"Keymaster: ignored an empty learn for {key}"); return; }
      var keys = req.Keys.Distinct().OrderBy(k => k).ToList();
      if (!LearnPolicy.Replaces(before, keys)) return;
      learned.Entries[key] = new KeysEntry { EftBuild = req.EftBuild, Keys = keys };
      store.WriteLearned(learned);
      Rebuild();
      logger.Info($"Keymaster: learned {key}: {keys.Count} keys");
    }
  }

  void Rebuild()
  {
    var input = new KeyTableInput { Seed = seed, Learned = store.ReadLearned(), KnownTpls = KeyTemplates() };
    foreach (var (id, loc) in locations.GetDictionary())
    {
      var b = loc?.Base;
      if (b == null) continue;
      var name = (b.Id ?? id).ToLowerInvariant();
      if (b.AccessKeys != null) input.AccessKeys[name] = b.AccessKeys.ToList();
      // Transit.Target is the destination's hex _Id, not a name; Transit.Location is the name.
      if (b.Transits != null) input.TransitTargets[name] = b.Transits.Where(t => t.Location != null).Select(t => t.Location!.ToLowerInvariant()).Distinct().ToList();
    }
    Table = KeyTable.Build(input);
    Homes = KeyTable.Homes(input);
  }

  HashSet<string> KeyTemplates()
  {
    var key = new MongoId(KeymasterIds.KeyCategory);
    var set = new HashSet<string>();
    foreach (var item in templates.Items.Values)
      for (var t = item; t != null; t = templates.Items.GetValueOrDefault(t.Parent))
        if (t.Parent == key) { set.Add(item.Id.ToString()); break; }
    return set;
  }
}
