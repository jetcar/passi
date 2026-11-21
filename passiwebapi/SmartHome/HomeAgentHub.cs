using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebApiDto.SmartHome;

namespace passi_webapi.SmartHome;

[Authorize(AuthenticationSchemes = SmartHomeRegistration.AgentScheme)]
public sealed class HomeAgentHub(SmartHomeService homes) : Hub
{
    private AgentIdentity Agent => Context.Items.TryGetValue(typeof(AgentIdentity), out var value)
        ? (AgentIdentity)value : throw new HubException("unauthorized");

    public override async Task OnConnectedAsync()
    {
        var agent = Context.GetHttpContext()?.Items[typeof(AgentIdentity)] as AgentIdentity;
        if (agent == null) { Context.Abort(); return; }
        Context.Items[typeof(AgentIdentity)] = agent;
        try { await homes.ConnectAsync(agent, Context.ConnectionId); }
        catch (SmartHomeException) { Context.Abort(); return; }
        await base.OnConnectedAsync();
    }

    public async Task<ExchangeResponse> Exchange(ExchangeRequest request)
    {
        try { return await homes.ExchangeAsync(Agent, Context.ConnectionId, request); }
        catch (SmartHomeException error)
        {
            if (error.StatusCode is 401 or 403) Context.Abort();
            throw new HubException(error.Code);
        }
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (Context.Items.TryGetValue(typeof(AgentIdentity), out var value))
        {
            try { await homes.DisconnectAsync((AgentIdentity)value, Context.ConnectionId); }
            catch (SmartHomeException) { /* A replaced/revoked connection cannot mutate its successor. */ }
        }
        await base.OnDisconnectedAsync(exception);
    }
}
