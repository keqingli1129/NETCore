using CoreMVC.Application.Interfaces;
using CoreMVC.Domain.Entities;
using MediatR;

namespace CoreMVC.Application.Orders.Commands;

/// <summary>
/// Handles <see cref="CreateOrderCommand"/> by creating and persisting a new <see cref="Order"/>.
/// </summary>
public class CreateOrderCommandHandler(IOrderRepository repository)
    : IRequestHandler<CreateOrderCommand, int>
{
    public async Task<int> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            CustomerId = request.CustomerId,
            OrderDate = DateTime.UtcNow,
            RequiredDate = request.RequiredDate,
            Freight = request.Freight,
            ShipName = request.ShipName,
            ShipCity = request.ShipCity,
            ShipCountry = request.ShipCountry
        };

        await repository.AddAsync(order, cancellationToken);

        return order.OrderId;
    }
}
