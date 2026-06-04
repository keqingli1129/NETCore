using CoreMVC.Domain.Entities;

namespace CoreMVC.Application.Interfaces;

/// <summary>
/// Repository for <see cref="Order"/> that dispatches domain events after persistence.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}
