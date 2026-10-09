using Keymaster.Shared;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace Keymaster.Server;

/// <summary>
/// Grid filter edits. They only mutate templates, so they run after every mod has registered its containers
/// (custom items cannot be created this late, edits can).
/// </summary>
[Injectable(TypePriority = KeymasterLoadOrder.Filters)]
public class KeymasterFilters(ISptLogger<KeymasterFilters> logger, KmTemplateTable templates) : IOnLoad
{
  const string ItemRoot = "54009119af1c881c07000029";

  public Task OnLoadAsync(CancellationToken cancellationToken)
  {
    var caseAllowedIn = AllowCaseWhereSiccIs();
    var vaultExcludedFrom = ExcludeVaultOutsideStash();
    logger.Debug($"Keymaster: case allowed in {caseAllowedIn} filters, vault excluded from {vaultExcludedFrom}");
    return Task.CompletedTask;
  }

  int AllowCaseWhereSiccIs()
  {
    var sicc = new MongoId(KeymasterIds.SiccTpl);
    var caseId = new MongoId(KeymasterIds.CaseTpl);
    var n = 0;
    foreach (var item in templates.Items.Values)
      foreach (var filter in Filters(item))
        if (filter.Filter is { } f && f.Contains(sicc) && f.Add(caseId)) n++;
    return n;
  }

  int ExcludeVaultOutsideStash()
  {
    var vault = new MongoId(KeymasterIds.VaultTpl);
    var n = 0;
    foreach (var item in templates.Items.Values)
    {
      if (IsStash(item)) continue;
      foreach (var grid in item.Properties?.Grids ?? [])
        if (grid.Properties is { } p && (p.Filters == null || !p.Filters.Any()))
          p.Filters = new List<GridFilter> { new GridFilter { Filter = new HashSet<MongoId> { new(ItemRoot) }, ExcludedFilter = new HashSet<MongoId>() } };
      foreach (var filter in Filters(item))
      {
        filter.ExcludedFilter ??= new HashSet<MongoId>();
        if (filter.ExcludedFilter.Add(vault)) n++;
      }
    }
    return n;
  }

  bool IsStash(TemplateItem item)
  {
    var stash = new MongoId(KeymasterIds.StashClass);
    for (var t = item; t != null; t = templates.Items.GetValueOrDefault(t.Parent))
      if (t.Id == stash) return true;
    return false;
  }

  static IEnumerable<GridFilter> Filters(TemplateItem item) =>
    (item.Properties?.Grids ?? []).SelectMany(g => g.Properties?.Filters ?? []);
}
