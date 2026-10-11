using Keymaster.Shared;
using Xunit;

public class OneOfEachTests
{
  const string Key = "5780cf7f2459777de4559322";
  static readonly string Case = KeymasterIds.CaseTpl, Vault = KeymasterIds.VaultTpl;

  [Fact] public void NoneHeld_OneCase_Allowed() => Assert.True(OneOfEach.MayReceive(new[] { Key }, new[] { Case }));
  [Fact] public void CaseHeld_SecondCase_Refused() => Assert.False(OneOfEach.MayReceive(new[] { Case }, new[] { Case }));
  [Fact] public void VaultHeld_SecondVault_Refused() => Assert.False(OneOfEach.MayReceive(new[] { Vault }, new[] { Vault }));
  [Fact] public void CaseHeld_Vault_Allowed() => Assert.True(OneOfEach.MayReceive(new[] { Case }, new[] { Vault }));
  [Fact] public void TwoCasesAtOnce_Refused() => Assert.False(OneOfEach.MayReceive(new string[0], new[] { Case, Case }));
  [Fact] public void BothHeld_OtherItems_Allowed() => Assert.True(OneOfEach.MayReceive(new[] { Case, Vault }, new[] { Key }));
  [Fact] public void Purchase_CountTwo_Refused() => Assert.False(OneOfEach.MayBuy(new string[0], Case, 2));
  [Fact] public void Purchase_CaseHeld_Refused() => Assert.False(OneOfEach.MayBuy(new[] { Case }, Case, 1));
  [Fact] public void Purchase_FirstCase_Allowed() => Assert.True(OneOfEach.MayBuy(new[] { Key }, Case, 1));
  [Fact] public void Purchase_OtherItem_Allowed() => Assert.True(OneOfEach.MayBuy(new[] { Case }, Key, 5));
  [Fact] public void FleaPmcBuy_UsesOfferLookup() => Assert.True(OneOfEach.UsesOfferLookup("buy_from_ragfair_pmc"));
  [Fact] public void FleaTraderBuy_UsesTraderAssort() => Assert.False(OneOfEach.UsesOfferLookup("buy_from_ragfair_trader"));
  [Fact] public void TraderBuy_UsesTraderAssort() => Assert.False(OneOfEach.UsesOfferLookup("buy_from_trader"));
  [Fact] public void Listing_Case_Refused() => Assert.False(OneOfEach.MayList(new[] { Key, Case }));
  [Fact] public void Listing_Vault_Refused() => Assert.False(OneOfEach.MayList(new[] { Vault }));
  [Fact] public void Listing_OtherItems_Allowed() => Assert.True(OneOfEach.MayList(new[] { Key }));
}
