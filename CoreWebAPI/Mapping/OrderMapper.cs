using CoreMVC.Contracts.Orders;
using CoreMVC.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace CoreWebAPI.Mapping;

/// <summary>
/// Compile-time mappings between <see cref="Order"/> and its contract DTOs.
/// Only the write path is mapped here; reads use the hand-written projection in
/// <c>OrdersController</c> because Mapperly cannot project the non-nullable nested
/// <see cref="OrderDto.OrderDetails"/> collection from the null-oblivious EF entities.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class OrderMapper
{
    /// <summary>Copies the editable header fields of <paramref name="dto"/> onto an existing entity.</summary>
    public static partial void Update(CreateOrderDto dto, Order order);
}
