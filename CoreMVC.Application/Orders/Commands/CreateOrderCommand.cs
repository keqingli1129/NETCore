using MediatR;

namespace CoreMVC.Application.Orders.Commands;

/// <summary>
/// Command to create a new order.
/// </summary>
public record CreateOrderCommand(
    string CustomerId,
    DateTime? RequiredDate,
    decimal? Freight,
    string? ShipName,
    string? ShipCity,
    string? ShipCountry) : IRequest<int>;
