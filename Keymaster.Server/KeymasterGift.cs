using Keymaster.Shared;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Commerce;

namespace Keymaster.Server;

[Injectable(InjectionType.Singleton)]
public class KeymasterGift(ISptLogger<KeymasterGift> logger, KeymasterServerConfig config, KeymasterStore store, ProfileHelper profiles, SaveServer saves, MailSendService mail)
{
  static readonly MongoId Vault = new(KeymasterIds.VaultTpl);

  public void CheckFor(MongoId sessionId)
  {
    if (!config.Gift.Enabled) return;
    if (sessionId.IsEmpty || !saves.ProfileExists(sessionId)) return;
    var pmc = profiles.GetPmcProfile(sessionId);
    if (pmc?.Inventory?.Items == null) return;
    var needsVault = NeedsVault(pmc);

    var sent = store.UpdateState(sessionId, s =>
    {
      var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      if (!GiftPolicy.ShouldSend(!needsVault, s.GiftSentAt, now, GiftPolicy.StorageSeconds, config.Gift.Resend)) return false;
      s.GiftSentAt = now;
      return true;
    });
    // Recorded before mailing: a write that keeps failing then sends nothing, rather than a vault per config call.
    if (sent) Mail(sessionId, pmc, "vault gift sent to");
  }

  /// <summary>The web page's resend: ignores the gift settings and the 30-day wait, never mails a profile that holds a vault.</summary>
  public bool Resend(MongoId profileId)
  {
    if (profileId.IsEmpty || !saves.ProfileExists(profileId)) return false;
    var pmc = profiles.GetPmcProfile(profileId);
    if (pmc?.Inventory?.Items == null || !NeedsVault(pmc)) return false;
    // Same order as the gift: giftSentAt first, then the mail.
    store.UpdateState(profileId, s => { s.GiftSentAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); return true; });
    return Mail(profileId, pmc, "vault resent from the web page to");
  }

  public static bool NeedsVault(PmcData pmc) =>
    VaultHolders.NeedsVault((pmc.Inventory?.Items ?? []).Select(i => i.Template.ToString()));

  /// <summary>True when an unexpired, unclaimed mail in the profile carries a vault.</summary>
  public static bool HasUnclaimedGift(SptProfile profile, long nowUnix) =>
    (profile.DialogueRecords?.Values.AsEnumerable() ?? []).SelectMany(d => d.Messages ?? []).Any(m =>
      m.RewardCollected != true
      && (m.MaxStorageTime is not { } keep || (m.DateTime ?? 0) + keep > nowUnix)
      && (m.Items?.Data ?? []).Any(i => i.Template == Vault));

  bool Mail(MongoId sessionId, PmcData pmc, string what)
  {
    try
    {
      mail.SendSystemMessageToPlayer(sessionId, "Your KM Vault has arrived. Keep it in your stash: it holds your whole key collection, sorted by map, and your KM Case is filled from it before each raid.", [new Item { Id = new MongoId(), Template = Vault }], GiftPolicy.StorageSeconds);
      logger.Info($"Keymaster: {what} {pmc.Info?.Nickname}");
      return true;
    }
    catch (Exception ex)
    {
      logger.Warning($"Keymaster: vault recorded but not mailed to {pmc.Info?.Nickname}: {ex.Message}");
      return false;
    }
  }
}
