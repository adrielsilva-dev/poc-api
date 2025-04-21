using Data;
using Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.ServiceBus;
using System.Text;
using System.Text.Json;
namespace api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
  private readonly OrderContext _context;
  private readonly IConfiguration _config;
  private readonly string serviceBusConnectionString;

  public OrdersController(OrderContext context, IConfiguration config)
  {
    _context = context;
    _config = config;
    serviceBusConnectionString = _config.GetValue<string>("AzureBusConnectionString");
  }

  // GET: api/orders
  [HttpGet]
  public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
  {
    return await _context.Orders.ToListAsync();
  }

  // GET: api/orders/{id}
  [HttpGet("{id}")]
  public async Task<ActionResult<Order>> GetOrder(Guid id)
  {
    var order = await _context.Orders.FindAsync(id);

    if (order == null)
    {
      return NotFound();
    }

    return order;
  }

  // POST: api/orders
  [HttpPost]
  public async Task<ActionResult<Order>> PostOrder(OrderDto orderDto)
  {
    var order = new Order
    {
      Cliente = orderDto.Cliente,
      Produto = orderDto.Produto,
      Valor = orderDto.Valor
    };

    _context.Orders.Add(order);
    await _context.SaveChangesAsync();
    await SendMessageToQueue(order);

    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
  }

  // PUT: api/orders/{id}
  [HttpPut("{id}")]
  public async Task<IActionResult> PutOrder(Guid id, Order order)
  {
    if (id != order.Id)
    {
      return BadRequest();
    }

    _context.Entry(order).State = EntityState.Modified;

    try
    {
      await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
      if (!OrderExists(id))
      {
        return NotFound();
      }
      else
      {
        throw;
      }
    }

    return NoContent();
  }

    private bool OrderExists(Guid id)
  {
    return _context.Orders.Any(e => e.Id == id);
  }

  // DELETE: api/orders/5
  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteOrder(Guid id)
  {
    var order = await _context.Orders.FindAsync(id);
    if (order == null)
    {
      return NotFound();
    }

    _context.Orders.Remove(order);
    await _context.SaveChangesAsync();

    return NoContent();
  }

       private async Task SendMessageToQueue(Order order)
     {
         var queueName = "orders";

         var client = new QueueClient(serviceBusConnectionString, queueName, ReceiveMode.PeekLock);
         string messageBody = JsonSerializer.Serialize(order);
         var message = new Message(Encoding.UTF8.GetBytes(messageBody));
         
         await client.SendAsync(message);
         await client.CloseAsync();
     }
}