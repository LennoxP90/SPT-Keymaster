using Keymaster.Shared;
using Xunit;

public class VaultHoldersTests
{
  const string Key = "5780cf7f2459777de4559322";

  [Fact] public void EmptyProfile_NeedsVault() => Assert.True(VaultHolders.NeedsVault(new string[0]));
  [Fact] public void OtherItemsOnly_NeedsVault() => Assert.True(VaultHolders.NeedsVault(new[] { Key, KeymasterIds.CaseTpl }));
  [Fact] public void HoldsVault_DoesNotNeedOne() => Assert.False(VaultHolders.NeedsVault(new[] { Key, KeymasterIds.VaultTpl }));
}
