using System.Linq;
using System.Collections.Generic;
using Keymaster.Client.Wire;
using Keymaster.Shared;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace Keymaster.Client
{
  public static class KeymasterApi
  {
    public static IReadOnlyDictionary<string, List<string>> Table = new Dictionary<string, List<string>>();
    public static IReadOnlyDictionary<string, string> Homes = new Dictionary<string, string>();
    public static StateDtoC State = new StateDtoC();
    public static bool Available;

    /// <summary>Fetches the table and this profile's state. Also triggers the server's vault-gift check. Only the first
    /// load (initial) can turn Keymaster off; a later failure keeps the cached table and state.</summary>
    public static bool Refresh(bool initial = false)
    {
      try
      {
        var config = JsonConvert.DeserializeObject<ConfigResponseC>(RequestHandler.GetJson("/keymaster/config"));
        Table = config?.Table ?? new Dictionary<string, List<string>>();
        Homes = config?.Homes ?? new Dictionary<string, string>();
        State = JsonConvert.DeserializeObject<StateDtoC>(RequestHandler.GetJson("/keymaster/state/get")) ?? new StateDtoC();
        Available = true;
      }
      catch (System.Exception ex)
      {
        if (initial) { Available = false; Plugin.Detail("config fetch failed: " + ex.Message); return false; }
        Plugin.Warn("refresh failed, using the last key table and state: " + ex.Message);
      }
      return Available;
    }

    /// <summary>The key table's list for the map the case was last loaded for, in table order. Empty before the first load.</summary>
    public static IReadOnlyList<string> CurrentMapKeys()
    {
      var current = State.CurrentMap;
      if (current == null) return new string[0];
      var at = current.IndexOf('@');
      return KeyTable.Resolve(Table, at < 0 ? current : current.Substring(0, at), at < 0 ? null : current.Substring(at + 1));
    }

    /// <summary>The current map's keys, then always-bring: what belongs in the case's KEYS section.</summary>
    public static List<string> WantedKeys() => CurrentMapKeys().Concat(State.AlwaysBring).Distinct().ToList();

    public static void SaveState() => RequestHandler.PostJson("/keymaster/state/set", JsonConvert.SerializeObject(State));

    public static void Learn(LearnRequestC req) => RequestHandler.PostJson("/keymaster/learn", JsonConvert.SerializeObject(req));
  }
}
