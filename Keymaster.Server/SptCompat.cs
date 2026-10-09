global using SPTarkov.Common.Models.Logging;
global using SPTarkov.Server.Core.Helpers.Server;
global using SPTarkov.Server.Core.Helpers.Profile;
global using SPTarkov.Server.Core.Services.Modding.Custom;
global using KmTemplateTable = SPTarkov.Server.Core.Models.Spt.Tables.TemplateTable;
global using KmLocationTable = SPTarkov.Server.Core.Models.Spt.Tables.LocationTable;

namespace Keymaster.Server;

public static class KeymasterLoadOrder
{
  public const int Items = SPTarkov.Server.Core.DI.OnLoadOrder.Preload + 1;
  public const int Trader = SPTarkov.Server.Core.DI.OnLoadOrder.TraderRegistration + 30;
  public const int Filters = SPTarkov.Server.Core.DI.OnLoadOrder.PostLoad + 100000;
  public const int KeyTable = SPTarkov.Server.Core.DI.OnLoadOrder.SaveCallbacks - 1;
}
