using CoreMVC.Domain.Events;

namespace CoreMVC.Domain.Entities;

public partial class Order
{
    /// <summary>
    /// Marks the order as shipped and raises an <see cref="OrderShippedEvent"/>.
    /// </summary>
    public void MarkAsShipped(DateTime shippedDate)
    {
        ShippedDate = shippedDate;
        Raise(new OrderShippedEvent(OrderId, shippedDate));
    }
}
