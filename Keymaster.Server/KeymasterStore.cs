using System.Text.Json;
using System.Text.Json.Serialization;
using Keymaster.Server.Wire;
using Keymaster.Shared;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace Keymaster.Server;

/// <summary>data/keys-learned.json and data/state.json beside the mod. One lock; writes are whole-file.</summary>
[Injectable(InjectionType.Singleton)]
public class KeymasterStore
{
  // KeysFile has no attributes, so the camelCase files written by the tour and by hand only bind case-insensitively.
  static readonly JsonSerializerOptions Opts = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

  readonly object gate = new();
  readonly string dir;

  public KeymasterStore(ModHelper modHelper)
  {
    dir = Path.Combine(modHelper.GetAbsolutePathToModFolder(typeof(KeymasterStore).Assembly), "data");
    Directory.CreateDirectory(dir);
  }

  public string ModFolder => Path.GetDirectoryName(dir)!;

  public KeysFile ReadLearned() => Read<KeysFile>("keys-learned.json") ?? new KeysFile();
  public void WriteLearned(KeysFile f) => Write("keys-learned.json", f);

  public Dictionary<string, ProfileState> ReadStates() => Read<Dictionary<string, ProfileState>>("state.json") ?? new();
  public void WriteStates(Dictionary<string, ProfileState> s) => Write("state.json", s);

  public StateDto Get(MongoId sessionId)
  {
    var s = ReadStates().GetValueOrDefault(sessionId.ToString()) ?? new ProfileState();
    return new StateDto { CurrentMap = s.CurrentMap, AlwaysBring = s.AlwaysBring };
  }

  /// <summary>Read-modify-write of one profile's state under the store lock. mutate returns false to skip the write.</summary>
  public bool UpdateState(MongoId sessionId, Func<ProfileState, bool> mutate)
  {
    lock (gate)
    {
      var states = ReadStates();
      var s = states.GetValueOrDefault(sessionId.ToString()) ?? new ProfileState();
      if (!mutate(s)) return false;
      states[sessionId.ToString()] = s;
      WriteStates(states);
      return true;
    }
  }

  public StateDto Set(MongoId sessionId, StateDto dto)
  {
    UpdateState(sessionId, s =>
    {
      s.CurrentMap = dto.CurrentMap;
      s.AlwaysBring = dto.AlwaysBring ?? new List<string>();
      return true;
    });
    return Get(sessionId);
  }

  T? Read<T>(string name)
  {
    lock (gate)
    {
      var p = Path.Combine(dir, name);
      return File.Exists(p) ? JsonSerializer.Deserialize<T>(File.ReadAllText(p), Opts) : default;
    }
  }

  void Write<T>(string name, T value)
  {
    lock (gate)
    {
      var p = Path.Combine(dir, name);
      File.WriteAllText(p + ".tmp", JsonSerializer.Serialize(value, Opts));
      File.Move(p + ".tmp", p, true);
    }
  }
}

public class ProfileState
{
  [JsonPropertyName("currentMap")] public string? CurrentMap { get; set; }
  [JsonPropertyName("alwaysBring")] public List<string> AlwaysBring { get; set; } = new();
  [JsonPropertyName("giftSentAt")] public long? GiftSentAt { get; set; }
}
