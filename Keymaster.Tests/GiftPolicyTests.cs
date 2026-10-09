using Keymaster.Shared;
using Xunit;

public class GiftPolicyTests
{
  const long Day = 86400;

  [Fact] public void FirstTime_Sends() => Assert.True(GiftPolicy.ShouldSend(false, null, 1000, 30 * Day, true));
  [Fact] public void FirstTime_AlreadyHasVault_DoesNotSend() => Assert.False(GiftPolicy.ShouldSend(true, null, 1000, 30 * Day, true));
  [Fact] public void Sent_StillClaimable_DoesNotResend() => Assert.False(GiftPolicy.ShouldSend(false, 0, 10 * Day, 30 * Day, true));
  [Fact] public void Sent_Expired_NoVault_Resends() => Assert.True(GiftPolicy.ShouldSend(false, 0, 31 * Day, 30 * Day, true));
  [Fact] public void Sent_Expired_ResendOff_DoesNotSend() => Assert.False(GiftPolicy.ShouldSend(false, 0, 31 * Day, 30 * Day, false));
}
