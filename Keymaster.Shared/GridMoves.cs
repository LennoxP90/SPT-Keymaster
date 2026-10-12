using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  /// <summary>A 1x1 item at a cell of a grid: ParentId + SlotId name the grid.</summary>
  public sealed class GridItem
  {
    public string Id { get; set; } = "";
    public string ParentId { get; set; } = "";
    public string SlotId { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
  }

  public sealed class GridRef
  {
    public string ParentId { get; set; } = "";
    public string SlotId { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
  }

  public sealed class GridMove
  {
    public string Id { get; set; } = "";
    public GridRef To { get; set; } = new GridRef();
    public int X { get; set; }
    public int Y { get; set; }
  }

  /// <summary>
  /// Moves of 1x1 items between grids, for the server's transit swap. Only existing items move: nothing is created or
  /// removed, so a key cannot be duplicated. items must hold every item in every grid involved, or a cell may be shared.
  /// </summary>
  public static class GridMoves
  {
    /// <summary>Each id to its grid's first free cell, row by row, in order. An id with no room, unknown or asked twice stays put.</summary>
    public static List<GridMove> Place(IReadOnlyList<GridItem> items, IEnumerable<(string Id, GridRef To)> wanted)
    {
      var byId = items.GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First());
      var taken = new HashSet<(string, string, int, int)>(items.Select(Cell));
      var moves = new List<GridMove>();
      foreach (var (id, to) in wanted)
      {
        if (!byId.TryGetValue(id, out var item) || moves.Any(m => m.Id == id)) continue;
        var free = Free(to, taken);
        if (free == null) continue;
        taken.Remove(Cell(item));
        taken.Add((to.ParentId, to.SlotId, free.Value.X, free.Value.Y));
        moves.Add(new GridMove { Id = id, To = to, X = free.Value.X, Y = free.Value.Y });
      }
      return moves;
    }

    /// <summary>Null when the moves keep every item exactly once, each in its own cell inside its grid; otherwise what is wrong.</summary>
    public static string? Problem(IReadOnlyList<GridItem> items, IReadOnlyList<GridMove> moves)
    {
      if (items.Select(i => i.Id).Distinct().Count() != items.Count) return "an item id appears twice before the moves";
      if (moves.Select(m => m.Id).Distinct().Count() != moves.Count) return "an item is moved twice";
      var ids = new HashSet<string>(items.Select(i => i.Id));
      var unknown = moves.FirstOrDefault(m => !ids.Contains(m.Id));
      if (unknown != null) return "move of unknown item " + unknown.Id;
      var outside = moves.FirstOrDefault(m => m.X < 0 || m.Y < 0 || m.X >= m.To.Width || m.Y >= m.To.Height);
      if (outside != null) return $"{outside.Id} placed outside its grid at {outside.X},{outside.Y}";
      var byId = moves.ToDictionary(m => m.Id);
      var after = items.Select(i => byId.TryGetValue(i.Id, out var m) ? (m.To.ParentId, m.To.SlotId, m.X, m.Y) : Cell(i)).ToList();
      var shared = after.GroupBy(c => c).FirstOrDefault(g => g.Count() > 1);
      return shared == null ? null : $"two items in {shared.Key.Item1}/{shared.Key.Item2} {shared.Key.Item3},{shared.Key.Item4}";
    }

    static (string, string, int, int) Cell(GridItem i) => (i.ParentId, i.SlotId, i.X, i.Y);

    static (int X, int Y)? Free(GridRef g, HashSet<(string, string, int, int)> taken)
    {
      for (var y = 0; y < g.Height; y++)
        for (var x = 0; x < g.Width; x++)
          if (!taken.Contains((g.ParentId, g.SlotId, x, y))) return (x, y);
      return null;
    }
  }
}
