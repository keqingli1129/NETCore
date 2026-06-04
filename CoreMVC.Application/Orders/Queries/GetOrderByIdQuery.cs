using MediatR;

namespace CoreMVC.Application.Orders.Queries;

/// <summary>
/// Query to retrieve a single order by its ID.
/// </summary>
public record GetOrderByIdQuery(int OrderId) : IRequest<OrderDto?>;
