using Keymaster.Shared;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Commerce;

namespace Keymaster.Server;

[Injectable(InjectionType.Singleton)]
public class KeymasterGift(ISptLogger<KeymasterGift> logger, KeymasterServerConfig config, KeymasterStore store, ProfileHelper profiles, SaveServer saves, MailSendService mail)
{
  public void CheckFor(MongoId sessionId)
  {
    if (!config.Gift.Enabled) return;
    if (sessionId.IsEmpty || !saves.ProfileExists(sessionId)) return;
    var pmc = profiles.GetPmcProfile(sessionId);
    if (pmc?.Inventory?.Items == null) return;
    var vault = new MongoId(KeymasterIds.VaultTpl);
    var hasVault = pmc.Inventory.Items.Any(i => i.Template == vault);

    var sent = store.UpdateState(sessionId, s =>
    {
      var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      if (!GiftPolicy.ShouldSend(hasVault, s.GiftSentAt, now, GiftPolicy.StorageSeconds, config.Gift.Resend)) return false;
      s.GiftSentAt = now;
      return true;
    });
    // Recorded before mailing: a write that keeps failing then sends nothing, rather than a vault per config call.
    if (!sent) return;
    try
    {
      mail.SendSystemMessageToPlayer(sessionId, "Keymaster: your key vault. It stays in your stash.", [new Item { Id = new MongoId(), Template = vault }], GiftPolicy.StorageSeconds);
      logger.Info($"Keymaster: vault gift sent to {pmc.Info?.Nickname}");
    }
    catch (Exception ex) { logger.Warning($"Keymaster: vault gift recorded but not mailed to {pmc.Info?.Nickname}: {ex.Message}"); }
  }
}
