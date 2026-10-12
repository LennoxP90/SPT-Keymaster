using System.Collections.Generic;
using System.Linq;
using Keymaster.Shared;
using Xunit;

public class GridMovesTests
{
  static readonly GridRef Case = new GridRef { ParentId = "case", SlotId = "main", Width = 2, Height = 1 };
  static readonly GridRef Vault = new GridRef { ParentId = "vault", SlotId = "keys", Width = 2, Height = 2 };
  static GridItem I(string id, GridRef g, int x, int y) => new GridItem { Id = id, ParentId = g.ParentId, SlotId = g.SlotId, X = x, Y = y };

  [Fact]
  public void Transit_SwapsThroughAFullCase_EachItemOnce()
  {
    // The case is full with two Customs keys; the Factory key and Labs card wait in the vault.
    var items = new List<GridItem> { I("cust1", Case, 0, 0), I("cust2", Case, 1, 0), I("fac", Vault, 0, 0), I("labs", Vault, 1, 0) };
    var moves = GridMoves.Place(items, new[] { ("cust1", Vault), ("cust2", Vault), ("fac", Case), ("labs", Case) });
    Assert.Equal(4, moves.Count);
    Assert.Null(GridMoves.Problem(items, moves));
    Assert.Equal(new[] { "fac", "labs" }, moves.Where(m => m.To == Case).Select(m => m.Id).OrderBy(x => x));
  }

  [Fact]
  public void NoRoom_LeavesTheItemWhereItIs()
  {
    var items = new List<GridItem> { I("a", Case, 0, 0), I("b", Case, 1, 0), I("c", Vault, 0, 0) };
    Assert.Empty(GridMoves.Place(items, new[] { ("c", Case) }));
  }

  [Fact]
  public void SameIdTwice_MovesOnce()
  {
    var items = new List<GridItem> { I("a", Vault, 0, 0) };
    Assert.Single(GridMoves.Place(items, new[] { ("a", Case), ("a", Case) }));
  }

  [Fact]
  public void Problem_CatchesTwoItemsInOneCell()
  {
    var items = new List<GridItem> { I("a", Case, 0, 0), I("b", Vault, 0, 0) };
    Assert.NotNull(GridMoves.Problem(items, new[] { new GridMove { Id = "b", To = Case, X = 0, Y = 0 } }));
  }

  [Fact]
  public void Problem_CatchesAnUnknownOrRepeatedMove()
  {
    var items = new List<GridItem> { I("a", Vault, 0, 0) };
    Assert.NotNull(GridMoves.Problem(items, new[] { new GridMove { Id = "ghost", To = Case } }));
    Assert.NotNull(GridMoves.Problem(items, new[] { new GridMove { Id = "a", To = Case }, new GridMove { Id = "a", To = Case, X = 1 } }));
  }

  [Fact]
  public void Problem_CatchesAMoveOutsideTheGrid()
  {
    var items = new List<GridItem> { I("a", Vault, 0, 0) };
    Assert.NotNull(GridMoves.Problem(items, new[] { new GridMove { Id = "a", To = Case, X = 2, Y = 0 } }));
  }

  [Fact]
  public void SecondRun_HasNothingLeftToMove()
  {
    var items = new List<GridItem> { I("fac", Case, 0, 0), I("cust", Vault, 0, 0) };
    var plan = LoadPlanner.Plan(new[] { "facTpl" },
      new[] { new KeyCopy { ItemId = "fac", Tpl = "facTpl", Where = KeyHome.Case }, new KeyCopy { ItemId = "cust", Tpl = "custTpl", Where = KeyHome.Vault } },
      CopyChoice.Freshest, mapChanged: true);
    Assert.Empty(plan.ToCase);
    Assert.Empty(plan.ToHome);
  }
}
