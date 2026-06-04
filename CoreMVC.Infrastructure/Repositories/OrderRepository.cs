using CoreMVC.Application.Interfaces;
using CoreMVC.Domain.Entities;
using CoreMVC.Infrastructure.Data;
using CoreMVC.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreMVC.Infrastructure.Repositories;

/// <summary>
/// Repository for <see cref="Order"/> that dispatches domain events via MediatR after saving.
/// </summary>
public class OrderRepository(ApplicationDbContext dbContext, IMediator mediator) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        dbContext.Orders.Add(order);
        await SaveAndDispatchAsync(order, cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        dbContext.Orders.Update(order);
        await SaveAndDispatchAsync(order, cancellationToken);
    }

    private async Task SaveAndDispatchAsync(Order order, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);

        var domainEvents = order.DomainEvents.ToList();
        order.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
