namespace Zevoryn.Control.Api.Publishing;

using Microsoft.AspNetCore.SignalR;
using Zevoryn.Control.Api.Hubs;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Contracts;

public sealed class SignalRControlEventPublisher(IHubContext<ControlEventsHub> hub) : IControlEventPublisher
{
    public Task PublishSaaSEventAsync(SaaSEventDto @event, CancellationToken cancellationToken) => hub.Clients.All.SendAsync("saasEventReceived", @event, cancellationToken);
}
