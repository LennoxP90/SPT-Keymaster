using System.Collections.Generic;
using System.Linq;

namespace Keymaster.Shared
{
  public static class MergedUses
  {
    /// <summary>Summed uses left, or null when any copy is unlimited (-1): no count is shown then.</summary>
    public static int? Sum(IEnumerable<int> usesLeft)
    {
      var list = usesLeft.ToList();
      return list.Any(u => u < 0) ? (int?)null : list.Sum();
    }
  }
}
