using ImmoSearch.Api.Notifications;
using ImmoSearch.Domain.Models;
using ImmoSearch.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ImmoSearch.Api.Endpoints;

public static class NotificationsEndpoints
{
    public static void Map(IEndpointRouteBuilder app) => app.MapPost("/notifications/webpush/subscribe", async (
        IWebPushSubscriptionRepository repository,
        [FromBody] WebPushSubscriptionDto payload,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(payload.Endpoint) ||
            string.IsNullOrWhiteSpace(payload.P256dh) ||
            string.IsNullOrWhiteSpace(payload.Auth))
        {
            return Results.BadRequest("Missing subscription fields");
        }

        var subscription = new WebPushSubscription
        {
            Endpoint = payload.Endpoint,
            P256dh = payload.P256dh,
            Auth = payload.Auth,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await repository.AddOrUpdateAsync(subscription, cancellationToken);
        return Results.Ok();
    }).WithName("SubscribeWebPush");

    public static void MapTest(IEndpointRouteBuilder app) => app.MapPost("/notifications/webpush/test", async (
        IWebPushSubscriptionRepository repository,
        IWebPushSender sender,
        CancellationToken cancellationToken) =>
    {
        var subs = await repository.GetAllAsync(cancellationToken);
        if (subs.Count == 0) return Results.BadRequest("No subscriptions");

        foreach (var sub in subs)
        {
            await sender.SendAsync(sub, "ImmoSearch Test", "Test notification", null, cancellationToken);
        }

        return Results.Ok(new { sent = subs.Count });
    }).WithName("TestWebPush");
}

public sealed record WebPushSubscriptionDto(string Endpoint, string P256dh, string Auth);
