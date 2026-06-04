using CoreMVC.SharedKernel;

namespace CoreMVC.Domain.Events;

public record OrderShippedEvent(int OrderId, DateTime ShippedDate) : IDomainEvent;
