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
      var item = new Item { Id = new MongoId(), Template = vault };
      mail.SendSystemMessageToPlayer(sessionId, "Keymaster: your key vault. It stays in your stash.", [item], GiftPolicy.StorageSeconds);
      s.GiftSentAt = now;
      return true;
    });
    if (sent) logger.Info($"Keymaster: vault gift sent to {pmc.Info?.Nickname}");
  }
}
