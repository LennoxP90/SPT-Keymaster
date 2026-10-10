using System.Collections.Generic;
using Keymaster.Client.Wire;
using Newtonsoft.Json;
using SPT.Common.Http;

namespace Keymaster.Client
{
  public static class KeymasterApi
  {
    public static IReadOnlyDictionary<string, List<string>> Table = new Dictionary<string, List<string>>();
    public static StateDtoC State = new StateDtoC();
    public static bool Available;

    /// <summary>Fetches the table and this profile's state. Also triggers the server's vault-gift check.</summary>
    public static bool Refresh()
    {
      try
      {
        Table = JsonConvert.DeserializeObject<ConfigResponseC>(RequestHandler.GetJson("/keymaster/config"))?.Table ?? new Dictionary<string, List<string>>();
        State = JsonConvert.DeserializeObject<StateDtoC>(RequestHandler.GetJson("/keymaster/state/get")) ?? new StateDtoC();
        Available = true;
      }
      catch (System.Exception ex)
      {
        Available = false;
        Plugin.Detail("config fetch failed: " + ex.Message);
      }
      return Available;
    }

    public static void SaveState() => RequestHandler.PostJson("/keymaster/state/set", JsonConvert.SerializeObject(State));

    public static void Learn(LearnRequestC req) => RequestHandler.PostJson("/keymaster/learn", JsonConvert.SerializeObject(req));
  }
}
