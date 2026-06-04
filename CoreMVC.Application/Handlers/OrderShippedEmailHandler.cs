using CoreMVC.Application.Interfaces;
using CoreMVC.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreMVC.Application.Handlers;

/// <summary>
/// Sends a shipment notification email when an order is shipped.
/// </summary>
public class OrderShippedEmailHandler(IEmailSender emailSender, ILogger<OrderShippedEmailHandler> logger)
    : INotificationHandler<OrderShippedEvent>
{
    public async Task Handle(OrderShippedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending shipment email for Order {OrderId}, shipped on {ShippedDate}.",
            notification.OrderId, notification.ShippedDate);

        var subject = $"Your order #{notification.OrderId} has been shipped";
        var body = $"""
            <h2>Order Shipped</h2>
            <p>Your order <strong>#{notification.OrderId}</strong> was shipped on {notification.ShippedDate:D}.</p>
            """;

        // TODO: Replace with actual customer email lookup
        await emailSender.SendEmailAsync("customer@example.com", subject, body);
    }
}
