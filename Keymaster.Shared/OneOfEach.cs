using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  /// <summary>A profile holds at most one KM Case and one KM Vault.</summary>
  public static class OneOfEach
  {
    static readonly string[] Limited = { KeymasterIds.CaseTpl, KeymasterIds.VaultTpl };

    public static bool IsLimited(string tpl) => Limited.Contains(tpl);

    /// <summary>False when the incoming items would leave the profile with a second case or vault.</summary>
    public static bool MayReceive(IEnumerable<string> heldTpls, IEnumerable<string> incomingTpls)
    {
      var held = heldTpls.ToList();
      return incomingTpls.Where(IsLimited).GroupBy(t => t).All(g => g.Count() == 1 && !held.Contains(g.Key));
    }

    public static bool MayBuy(IEnumerable<string> heldTpls, string tpl, int count) =>
      MayReceive(heldTpls, Enumerable.Repeat(tpl, count));

    /// <summary>Only a player flea offer carries a flea offer id; a trader's offer bought on the flea carries the trader's assort item id.</summary>
    public static bool UsesOfferLookup(string? buyType) => buyType == "buy_from_ragfair_pmc";

    /// <summary>Neither item may be listed on the flea.</summary>
    public static bool MayList(IEnumerable<string> listedTpls) => !listedTpls.Any(IsLimited);
  }
}
