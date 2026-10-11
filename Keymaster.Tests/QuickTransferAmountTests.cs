using Keymaster.Shared;
using Xunit;

public class QuickTransferAmountTests
{
  [Fact] public void WholeStackFits() => Assert.Equal(3000, MoneyPlanner.QuickTransferAmount(1000, 3000, 8000));
  [Fact] public void CarriedElsewhere_CountsAgainstTheCap() => Assert.Equal(13200, MoneyPlanner.QuickTransferAmount(0, 444000000, 500000, 486800));
  [Fact] public void CarriedElsewhere_AtCap_NothingMoves() => Assert.Equal(0, MoneyPlanner.QuickTransferAmount(0, 1000, 500000, 500000));
  [Fact] public void PartFits_TakesExactlyTheRoom() => Assert.Equal(2000, MoneyPlanner.QuickTransferAmount(6000, 5000, 8000));
  [Fact] public void FullCell_Zero() => Assert.Equal(0, MoneyPlanner.QuickTransferAmount(8000, 5000, 8000));
  [Fact] public void OverCap_Zero() => Assert.Equal(0, MoneyPlanner.QuickTransferAmount(9000, 5, 8000));
  [Fact] public void EmptyCell_CapsBigStack() => Assert.Equal(500000, MoneyPlanner.QuickTransferAmount(0, 750000, 500000));
}
