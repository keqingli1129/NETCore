using CoreMVC.Application.Orders.Commands;
using CoreMVC.Application.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CoreWebAPI.Controllers;

/// <summary>
/// Demonstrates the CQRS pattern: commands for writes, queries for reads, dispatched via MediatR.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class OrdersCqrsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// QUERY side — reads an order by ID.
    /// Flow: Controller → MediatR → GetOrderByIdQueryHandler → IOrderRepository → DB
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>
    /// COMMAND side — creates a new order.
    /// Flow: Controller → MediatR → CreateOrderCommandHandler → IOrderRepository → DB
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = orderId }, new { orderId });
    }
}
