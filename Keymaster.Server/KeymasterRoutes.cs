using Keymaster.Server.Wire;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace Keymaster.Server;

public sealed class StateRequest : StateDto, IRequestData { }
public sealed class LearnRequestData : LearnRequest, IRequestData { }

/// <summary>SPT routes by path, not verb: state read and write are separate paths.</summary>
[Injectable]
public class KeymasterRoutes(JsonUtil jsonUtil, HttpResponseUtil httpResponseUtil, KeyTableService table, KeymasterStore state, KeymasterGift gift)
  : StaticRouter(
      jsonUtil,
      [
        new RouteAction<EmptyRequestData>("/keymaster/config", async (url, info, sessionId, output, ct) =>
        {
          gift.CheckFor(sessionId);
          return httpResponseUtil.NoBody(new ConfigResponse { Table = table.Table.ToDictionary(e => e.Key, e => e.Value), Homes = table.Homes.ToDictionary(e => e.Key, e => e.Value) });
        }),
        new RouteAction<EmptyRequestData>("/keymaster/state/get", async (url, info, sessionId, output, ct) =>
          httpResponseUtil.NoBody(state.Get(sessionId))),
        new RouteAction<StateRequest>("/keymaster/state/set", async (url, info, sessionId, output, ct) =>
          httpResponseUtil.NoBody(state.Set(sessionId, info))),
        new RouteAction<LearnRequestData>("/keymaster/learn", async (url, info, sessionId, output, ct) =>
        {
          table.Learn(info);
          return httpResponseUtil.NoBody(new { });
        }),
      ]);
