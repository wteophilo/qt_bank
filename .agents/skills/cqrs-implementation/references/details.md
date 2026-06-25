# cqrs-implementation — templates and worked examples

## Templates

### Template 1: Command Infrastructure

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Command base
public abstract class Command
{
    public string CommandId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

// Concrete commands
public class CreateOrder : Command
{
    public string CustomerId { get; }
    public List<OrderItemDto> Items { get; }
    public Dictionary<string, string> ShippingAddress { get; }

    public CreateOrder(string customerId, List<OrderItemDto> items, Dictionary<string, string> shippingAddress)
    {
        CustomerId = customerId;
        Items = items;
        ShippingAddress = shippingAddress;
    }
}

public class AddOrderItem : Command
{
    public string OrderId { get; }
    public string ProductId { get; }
    public int Quantity { get; }
    public decimal Price { get; }

    public AddOrderItem(string orderId, string productId, int quantity, decimal price)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        Price = price;
    }
}

public class CancelOrder : Command
{
    public string OrderId { get; }
    public string Reason { get; }

    public CancelOrder(string orderId, string reason)
    {
        OrderId = orderId;
        Reason = reason;
    }
}

// Command handler base
public interface ICommandHandler<in TCommand> where TCommand : Command
{
    Task HandleAsync(TCommand command);
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : Command
{
    Task<TResult> HandleAsync(TCommand command);
}

// Command bus
public class CommandBus
{
    private readonly Dictionary<Type, object> _handlers = new Dictionary<Type, object>();

    public void Register<TCommand>(ICommandHandler<TCommand> handler) where TCommand : Command
    {
        _handlers[typeof(TCommand)] = handler;
    }

    public void Register<TCommand, TResult>(ICommandHandler<TCommand, TResult> handler) where TCommand : Command
    {
        _handlers[typeof(TCommand)] = handler;
    }

    public async Task DispatchAsync(Command command)
    {
        var commandType = command.GetType();
        if (!_handlers.TryGetValue(commandType, out var handler))
        {
            throw new InvalidOperationException($"No handler registered for {commandType.Name}");
        }

        if (handler is ICommandHandler<Command> baseHandler)
        {
            await baseHandler.HandleAsync(command);
            return;
        }

        // Use reflection for typed command handlers
        var method = handler.GetType().GetMethod("HandleAsync", new[] { commandType });
        if (method == null)
        {
            throw new InvalidOperationException($"Handler for {commandType.Name} does not implement HandleAsync.");
        }

        var task = (Task)method.Invoke(handler, new object[] { command });
        await task.ConfigureAwait(false);
    }

    public async Task<TResult> DispatchAsync<TResult>(Command command)
    {
        var commandType = command.GetType();
        if (!_handlers.TryGetValue(commandType, out var handler))
        {
            throw new InvalidOperationException($"No handler registered for {commandType.Name}");
        }

        var method = handler.GetType().GetMethod("HandleAsync", new[] { commandType });
        if (method == null)
        {
            throw new InvalidOperationException($"Handler for {commandType.Name} does not implement HandleAsync.");
        }

        var task = (Task<TResult>)method.Invoke(handler, new object[] { command });
        return await task.ConfigureAwait(false);
    }
}

// Command handler implementation
public class CreateOrderHandler : ICommandHandler<CreateOrder, string>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventStore _eventStore;

    public CreateOrderHandler(IOrderRepository orderRepository, IEventStore eventStore)
    {
        _orderRepository = orderRepository;
        _eventStore = eventStore;
    }

    public async Task<string> HandleAsync(CreateOrder command)
    {
        // Validate
        if (command.Items == null || command.Items.Count == 0)
        {
            throw new ArgumentException("Order must have at least one item");
        }

        // Create aggregate
        var order = Order.Create(
            command.CustomerId,
            command.Items,
            command.ShippingAddress
        );

        // Persist events
        await _eventStore.AppendEventsAsync(
            $"Order-{order.Id}",
            "Order",
            order.UncommittedEvents
        );

        return order.Id;
    }
}
```

### Template 2: Query Infrastructure

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Query base
public abstract class Query
{
}

// Concrete queries
public class GetOrderById : Query
{
    public string OrderId { get; }

    public GetOrderById(string orderId)
    {
        OrderId = orderId;
    }
}

public class GetCustomerOrders : Query
{
    public string CustomerId { get; }
    public string Status { get; }
    public int Page { get; }
    public int PageSize { get; }

    public GetCustomerOrders(string customerId, string status = null, int page = 1, int pageSize = 20)
    {
        CustomerId = customerId;
        Status = status;
        Page = page;
        PageSize = pageSize;
    }
}

public class SearchOrders : Query
{
    public string QueryString { get; }
    public Dictionary<string, string> Filters { get; }
    public string SortBy { get; }
    public string SortOrder { get; }

    public SearchOrders(string queryString, Dictionary<string, string> filters = null, string sortBy = "created_at", string sortOrder = "desc")
    {
        QueryString = queryString;
        Filters = filters;
        SortBy = sortBy;
        SortOrder = sortOrder;
    }
}

// Query result types
public class OrderView
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
}

public class PaginatedResult<T>
{
    public List<T> Items { get; }
    public int Total { get; }
    public int Page { get; }
    public int PageSize { get; }

    public PaginatedResult(List<T> items, int total, int page, int pageSize)
    {
        Items = items;
        Total = total;
        Page = page;
        PageSize = pageSize;
    }

    public int TotalPages => (Total + PageSize - 1) / PageSize;
}

// Query handler base
public interface IQueryHandler<in TQuery, TResult> where TQuery : Query
{
    Task<TResult> HandleAsync(TQuery query);
}

// Query bus
public class QueryBus
{
    private readonly Dictionary<Type, object> _handlers = new Dictionary<Type, object>();

    public void Register<TQuery, TResult>(IQueryHandler<TQuery, TResult> handler) where TQuery : Query
    {
        _handlers[typeof(TQuery)] = handler;
    }

    public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query) where TQuery : Query
    {
        if (!_handlers.TryGetValue(typeof(TQuery), out var handler))
        {
            throw new InvalidOperationException($"No handler registered for {typeof(TQuery).Name}");
        }

        return await ((IQueryHandler<TQuery, TResult>)handler).HandleAsync(query);
    }
}

// Query handler implementation (using a conceptual Dapper/SQL connection factory)
public class GetOrderByIdHandler : IQueryHandler<GetOrderById, OrderView>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetOrderByIdHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<OrderView> HandleAsync(GetOrderById query)
    {
        using var conn = _dbConnectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<OrderView>(
            @"SELECT order_id AS OrderId, customer_id AS CustomerId, status AS Status, 
                     total_amount AS TotalAmount, item_count AS ItemCount, 
                     created_at AS CreatedAt, shipped_at AS ShippedAt
              FROM order_views
              WHERE order_id = @OrderId",
            new { query.OrderId }
        );
    }
}

public class GetCustomerOrdersHandler : IQueryHandler<GetCustomerOrders, PaginatedResult<OrderView>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetCustomerOrdersHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<PaginatedResult<OrderView>> HandleAsync(GetCustomerOrders query)
    {
        using var conn = _dbConnectionFactory.CreateConnection();

        var sqlParams = new DynamicParameters();
        sqlParams.Add("CustomerId", query.CustomerId);

        string whereClause = "customer_id = @CustomerId";
        if (!string.IsNullOrEmpty(query.Status))
        {
            whereClause += " AND status = @Status";
            sqlParams.Add("Status", query.Status);
        }

        // Get total count
        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM order_views WHERE {whereClause}",
            sqlParams
        );

        // Get paginated results
        var offset = (query.Page - 1) * query.PageSize;
        sqlParams.Add("Limit", query.PageSize);
        sqlParams.Add("Offset", offset);

        var rows = await conn.QueryAsync<OrderView>(
            $@"SELECT order_id AS OrderId, customer_id AS CustomerId, status AS Status, 
                      total_amount AS TotalAmount, item_count AS ItemCount, 
                      created_at AS CreatedAt, shipped_at AS ShippedAt
               FROM order_views
               WHERE {whereClause}
               ORDER BY created_at DESC
               LIMIT @Limit OFFSET @Offset",
            sqlParams
        );

        return new PaginatedResult<OrderView>(
            new List<OrderView>(rows),
            total,
            query.Page,
            query.PageSize
        );
    }
}
```

### Template 3: ASP.NET Core CQRS Application

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// Request/Response models
public class CreateOrderRequest
{
    public string CustomerId { get; set; }
    public List<OrderItemDto> Items { get; set; }
    public Dictionary<string, string> ShippingAddress { get; set; }
}

public class OrderResponse
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddItemRequest
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class CancelOrderRequest
{
    public string Reason { get; set; }
}

[ApiController]
public class OrdersController : ControllerBase
{
    private readonly CommandBus _commandBus;
    private readonly QueryBus _queryBus;

    public OrdersController(CommandBus commandBus, QueryBus queryBus)
    {
        _commandBus = commandBus;
        _queryBus = queryBus;
    }

    // Command endpoints (POST, PUT, DELETE)
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var command = new CreateOrder(
            request.CustomerId,
            request.Items,
            request.ShippingAddress
        );
        var orderId = await _commandBus.DispatchAsync<string>(command);
        return Ok(new { order_id = orderId });
    }

    [HttpPost("orders/{orderId}/items")]
    public async Task<IActionResult> AddItem(string orderId, [FromBody] AddItemRequest request)
    {
        var command = new AddOrderItem(
            orderId,
            request.ProductId,
            request.Quantity,
            request.Price
        );
        await _commandBus.DispatchAsync(command);
        return Ok(new { status = "item_added" });
    }

    [HttpDelete("orders/{orderId}")]
    public async Task<IActionResult> CancelOrder(string orderId, [FromBody] CancelOrderRequest request)
    {
        var command = new CancelOrder(orderId, request.Reason);
        await _commandBus.DispatchAsync(command);
        return Ok(new { status = "cancelled" });
    }

    // Query endpoints (GET)
    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrder(string orderId)
    {
        var query = new GetOrderById(orderId);
        var result = await _queryBus.DispatchAsync<GetOrderById, OrderView>(query);
        if (result == null)
        {
            return NotFound("Order not found");
        }
        return Ok(result);
    }

    [HttpGet("customers/{customerId}/orders")]
    public async Task<IActionResult> GetCustomerOrders(
        string customerId,
        [FromQuery] string status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetCustomerOrders(customerId, status, page, pageSize);
        var result = await _queryBus.DispatchAsync<GetCustomerOrders, PaginatedResult<OrderView>>(query);
        return Ok(result);
    }

    [HttpGet("orders/search")]
    public async Task<IActionResult> SearchOrders(
        [FromQuery] string q,
        [FromQuery] string sortBy = "created_at")
    {
        var query = new SearchOrders(q, sortBy: sortBy);
        var result = await _queryBus.DispatchAsync<SearchOrders, List<OrderView>>(query);
        return Ok(result);
    }
}
```

### Template 4: Read Model Synchronization

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public interface IProjection
{
    string Name { get; }
    IEnumerable<string> Handles();
    Task ApplyAsync(Event @event);
    Task ClearAsync();
}

public interface IEventStore
{
    Task<List<Event>> ReadAllAsync(long fromPosition, int limit);
}

public interface ICheckpointRepository
{
    Task<long> GetCheckpointAsync(string projectionName);
    Task SaveCheckpointAsync(string projectionName, long position);
}

public class Event
{
    public string EventType { get; set; }
    public long GlobalPosition { get; set; }
}

public class ReadModelSynchronizer
{
    private readonly IEventStore _eventStore;
    private readonly ICheckpointRepository _checkpointRepository;
    private readonly ILogger<ReadModelSynchronizer> _logger;
    private readonly Dictionary<string, IProjection> _projections;

    public ReadModelSynchronizer(
        IEventStore eventStore,
        ICheckpointRepository checkpointRepository,
        IEnumerable<IProjection> projections,
        ILogger<ReadModelSynchronizer> logger)
    {
        _eventStore = eventStore;
        _checkpointRepository = checkpointRepository;
        _logger = logger;
        _projections = projections.ToDictionary(p => p.Name);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var projection in _projections.Values)
            {
                await SyncProjectionAsync(projection);
            }
            await Task.Delay(100, cancellationToken);
        }
    }

    private async Task SyncProjectionAsync(IProjection projection)
    {
        var checkpoint = await _checkpointRepository.GetCheckpointAsync(projection.Name);

        var events = await _eventStore.ReadAllAsync(
            fromPosition: checkpoint,
            limit: 100
        );

        foreach (var @event in events)
        {
            if (projection.Handles().Contains(@event.EventType))
            {
                try
                {
                    await projection.ApplyAsync(@event);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Projection error while processing event in {ProjectionName}", projection.Name);
                    continue;
                }
            }

            await _checkpointRepository.SaveCheckpointAsync(projection.Name, @event.GlobalPosition);
        }
    }

    public async Task RebuildProjectionAsync(string projectionName)
    {
        if (!_projections.TryGetValue(projectionName, out var projection))
        {
            throw new ArgumentException($"Projection {projectionName} not found.");
        }

        // Clear existing data
        await projection.ClearAsync();

        // Reset checkpoint
        await _checkpointRepository.SaveCheckpointAsync(projectionName, 0);

        // Rebuild
        while (true)
        {
            var checkpoint = await _checkpointRepository.GetCheckpointAsync(projectionName);
            var events = await _eventStore.ReadAllAsync(checkpoint, 1000);

            if (events == null || events.Count == 0)
            {
                break;
            }

            foreach (var @event in events)
            {
                if (projection.Handles().Contains(@event.EventType))
                {
                    await projection.ApplyAsync(@event);
                }
            }

            await _checkpointRepository.SaveCheckpointAsync(
                projectionName,
                events[events.Count - 1].GlobalPosition
            );
        }
    }
}
```

### Template 5: Eventual Consistency Handling

```csharp
using System;
using System.Diagnostics;
using System.Threading.Tasks;

public class ConsistencyQueryResponse<T>
{
    public T Data { get; set; }
    public string Warning { get; set; }
}

public class ConsistentQueryHandler
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly QueryBus _queryBus;

    public ConsistentQueryHandler(IDbConnectionFactory dbConnectionFactory, QueryBus queryBus)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _queryBus = queryBus;
    }

    public async Task<ConsistencyQueryResponse<TResult>> QueryAfterCommandAsync<TQuery, TResult>(
        TQuery query,
        int expectedVersion,
        string streamId,
        double timeoutSeconds = 5.0) where TQuery : Query
    {
        var stopwatch = Stopwatch.StartNew();
        var timeoutMilliseconds = timeoutSeconds * 1000;

        while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds)
        {
            // Check if read model is caught up
            var projectionVersion = await GetProjectionVersionAsync(streamId);

            if (projectionVersion >= expectedVersion)
            {
                var data = await _queryBus.DispatchAsync<TQuery, TResult>(query);
                return new ConsistencyQueryResponse<TResult> { Data = data };
            }

            // Wait a bit and retry
            await Task.Delay(100);
        }

        // Timeout - return stale data with warning
        var staleData = await _queryBus.DispatchAsync<TQuery, TResult>(query);
        return new ConsistencyQueryResponse<TResult>
        {
            Data = staleData,
            Warning = "Data may be stale"
        };
    }

    private async Task<int> GetProjectionVersionAsync(string streamId)
    {
        using var conn = _dbConnectionFactory.CreateConnection();
        var version = await conn.ExecuteScalarAsync<int?>(
            "SELECT last_event_version FROM projection_state WHERE stream_id = @StreamId",
            new { StreamId = streamId }
        );
        return version ?? 0;
    }
}
```
