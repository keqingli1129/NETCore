using CoreMVC.Application.Interfaces;
using MediatR;

namespace CoreMVC.Application.Orders.Queries;

/// <summary>
/// Handles <see cref="GetOrderByIdQuery"/> by fetching the order and projecting to <see cref="OrderDto"/>.
/// </summary>
public class GetOrderByIdQueryHandler(IOrderRepository repository)
    : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
            return null;

        return new OrderDto(
            order.OrderId,
            order.CustomerId,
            order.OrderDate,
            order.RequiredDate,
            order.ShippedDate,
            order.Freight,
            order.ShipName,
            order.ShipCity,
            order.ShipCountry);
    }
}
