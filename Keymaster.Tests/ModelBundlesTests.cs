using Keymaster.Shared;
using Xunit;

public class ModelBundlesTests
{
  [Fact] public void Case_Path() => Assert.Equal("keymaster/km_case.bundle", ModelBundles.CasePath);
  [Fact] public void Vault_Path() => Assert.Equal("keymaster/km_vault.bundle", ModelBundles.VaultPath);
  [Fact] public void IsOurs_MatchesOnlyOurBundles()
  {
    Assert.True(ModelBundles.IsOurs(ModelBundles.CasePath));
    Assert.False(ModelBundles.IsOurs("assets/content/items/containers/item_container_lopouch.bundle"));
    Assert.False(ModelBundles.IsOurs(null));
  }
}
