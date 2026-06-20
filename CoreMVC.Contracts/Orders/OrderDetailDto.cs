namespace CoreMVC.Contracts.Orders;

public record OrderDetailDto
{
    public int ProductId { get; init; }
    public string? ProductName { get; init; }
    public decimal UnitPrice { get; init; }
    public short Quantity { get; init; }
    public float Discount { get; init; }
}
