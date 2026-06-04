namespace CoreMVC.Application.Orders.Queries;

/// <summary>
/// Read-only projection of an Order for query responses.
/// </summary>
public record OrderDto(
    int OrderId,
    string? CustomerId,
    DateTime? OrderDate,
    DateTime? RequiredDate,
    DateTime? ShippedDate,
    decimal? Freight,
    string? ShipName,
    string? ShipCity,
    string? ShipCountry);
