using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Servers;

namespace Keymaster.Server;

public record VaultHolderRow(MongoId ProfileId, string Nickname, int Level, bool HasCharacter, bool HoldsVault, bool GiftWaiting, long? GiftSentAt);

/// <summary>Every profile on the server and its vault state, for the web page. Read through SaveServer, never from disk.</summary>
[Injectable(InjectionType.Singleton)]
public class KeymasterVaultHolders(SaveServer saves, KeymasterStore store)
{
  public List<VaultHolderRow> List()
  {
    var states = store.ReadStates();
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    return saves.GetProfiles()
      .Where(p => p.Value.ProfileInfo?.InvalidOrUnloadableProfile != true)
      .Select(p =>
      {
        var pmc = p.Value.CharacterData?.PmcData;
        var hasCharacter = pmc?.Inventory?.Items != null;
        return new VaultHolderRow(
          p.Key,
          pmc?.Info?.Nickname ?? p.Value.ProfileInfo?.Username ?? p.Key.ToString(),
          pmc?.Info?.Level ?? 0,
          hasCharacter,
          hasCharacter && !KeymasterGift.NeedsVault(pmc!),
          KeymasterGift.HasUnclaimedGift(p.Value, now),
          states.GetValueOrDefault(p.Key.ToString())?.GiftSentAt);
      })
      .OrderBy(r => r.Nickname, StringComparer.OrdinalIgnoreCase)
      .ToList();
  }
}
