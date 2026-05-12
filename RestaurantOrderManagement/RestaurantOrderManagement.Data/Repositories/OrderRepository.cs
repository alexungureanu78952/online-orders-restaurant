using Microsoft.EntityFrameworkCore;
using RestaurantOrderManagement.Data.Context;
using RestaurantOrderManagement.Data.Models;
using System.Data;

namespace RestaurantOrderManagement.Data.Repositories
{
    public class OrderRepository : GenericRepository<Order>
    {
        public OrderRepository(RestaurantDbContext context) : base(context)
        {
        }

        
        public virtual async Task<Order?> GetOrderDetailsAsync(int orderId)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "dbo.sp_GetOrderDetails";
                command.CommandType = CommandType.StoredProcedure;

                var orderIdParam = command.CreateParameter();
                orderIdParam.ParameterName = "@OrderId";
                orderIdParam.Value = orderId;
                command.Parameters.Add(orderIdParam);

                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return null;

                var order = new Order
                {
                    OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    OrderCode = reader.GetString(reader.GetOrdinal("OrderCode")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                    ShippingFee = reader.GetDecimal(reader.GetOrdinal("ShippingFee")),
                    DiscountAmount = reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),
                    TotalCost = reader.GetDecimal(reader.GetOrdinal("TotalCost")),
                    EstimatedDeliveryTime = reader.IsDBNull(reader.GetOrdinal("EstimatedDeliveryTime"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("EstimatedDeliveryTime")),
                    ActualDeliveryTime = reader.IsDBNull(reader.GetOrdinal("ActualDeliveryTime"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("ActualDeliveryTime")),
                    DeliveryAddress = reader.IsDBNull(reader.GetOrdinal("DeliveryAddress"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("DeliveryAddress")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("Notes"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Notes")),
                    ModifiedDate = DateTime.UtcNow
                };

                order.User = new User
                {
                    UserId = order.UserId,
                    FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.IsDBNull(reader.GetOrdinal("LastName"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("Email"))
                };

                if (await reader.NextResultAsync())
                {
                    var items = new List<OrderItem>();
                    while (await reader.ReadAsync())
                    {
                        items.Add(new OrderItem
                        {
                            OrderItemId = reader.GetInt32(reader.GetOrdinal("OrderItemId")),
                            OrderId = order.OrderId,
                            ProductId = reader.IsDBNull(reader.GetOrdinal("ProductId"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("ProductId")),
                            MenuId = reader.IsDBNull(reader.GetOrdinal("MenuId"))
                                ? null
                                : reader.GetInt32(reader.GetOrdinal("MenuId")),
                            ItemType = reader.IsDBNull(reader.GetOrdinal("ItemType"))
                                ? "Preparat"
                                : reader.GetString(reader.GetOrdinal("ItemType")),
                            ItemName = reader.IsDBNull(reader.GetOrdinal("ItemName"))
                                ? string.Empty
                                : reader.GetString(reader.GetOrdinal("ItemName")),
                            Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                            UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                            ItemTotal = reader.GetDecimal(reader.GetOrdinal("ItemTotal")),
                            CreatedDate = DateTime.UtcNow
                        });
                    }

                    order.OrderItems = items;
                }

                return order;
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }

        
        public virtual async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId, int limit = 50)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "dbo.sp_GetUserOrders";
                command.CommandType = CommandType.StoredProcedure;

                var userIdParam = command.CreateParameter();
                userIdParam.ParameterName = "@UserId";
                userIdParam.Value = userId;
                command.Parameters.Add(userIdParam);

                var limitParam = command.CreateParameter();
                limitParam.ParameterName = "@Limit";
                limitParam.Value = limit;
                command.Parameters.Add(limitParam);

                using var reader = await command.ExecuteReaderAsync();
                var orders = new List<Order>();

                while (await reader.ReadAsync())
                {
                    orders.Add(new Order
                    {
                        OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                        UserId = userId,
                        OrderCode = reader.GetString(reader.GetOrdinal("OrderCode")),
                        OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                        ShippingFee = reader.GetDecimal(reader.GetOrdinal("ShippingFee")),
                        DiscountAmount = reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),
                        TotalCost = reader.GetDecimal(reader.GetOrdinal("TotalCost")),
                        EstimatedDeliveryTime = reader.IsDBNull(reader.GetOrdinal("EstimatedDeliveryTime"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("EstimatedDeliveryTime")),
                        ActualDeliveryTime = reader.IsDBNull(reader.GetOrdinal("ActualDeliveryTime"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("ActualDeliveryTime"))
                    });
                }

                return orders;
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }

        
        public virtual async Task<IEnumerable<Order>> GetAllOrdersAsync(string? status = null, DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "dbo.sp_GetAllOrders";
                command.CommandType = CommandType.StoredProcedure;

                var statusParam = command.CreateParameter();
                statusParam.ParameterName = "@Status";
                statusParam.Value = string.IsNullOrWhiteSpace(status) ? DBNull.Value : status;
                command.Parameters.Add(statusParam);

                var fromDateParam = command.CreateParameter();
                fromDateParam.ParameterName = "@FromDate";
                fromDateParam.Value = fromDate ?? (object)DBNull.Value;
                command.Parameters.Add(fromDateParam);

                var toDateParam = command.CreateParameter();
                toDateParam.ParameterName = "@ToDate";
                toDateParam.Value = toDate ?? (object)DBNull.Value;
                command.Parameters.Add(toDateParam);

                var limitParam = command.CreateParameter();
                limitParam.ParameterName = "@Limit";
                limitParam.Value = limit;
                command.Parameters.Add(limitParam);

                using var reader = await command.ExecuteReaderAsync();
                var orders = new List<Order>();

                while (await reader.ReadAsync())
                {
                    var order = new Order
                    {
                        OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                        OrderCode = reader.GetString(reader.GetOrdinal("OrderCode")),
                        OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                        Status = reader.GetString(reader.GetOrdinal("Status")),
                        SubTotal = reader.GetDecimal(reader.GetOrdinal("SubTotal")),
                        ShippingFee = reader.GetDecimal(reader.GetOrdinal("ShippingFee")),
                        DiscountAmount = reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),
                        TotalCost = reader.GetDecimal(reader.GetOrdinal("TotalCost"))
                    };

                    order.User = new User
                    {
                        FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.IsDBNull(reader.GetOrdinal("LastName"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("LastName")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Email"))
                    };

                    orders.Add(order);
                }

                return orders;
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }

        
        public virtual async Task<(int OrderId, string OrderCode)> CreateOrderAsync(int userId, decimal subTotal, decimal shippingFee,
            decimal discountAmount, string? deliveryAddress = null, string? notes = null)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.sp_CreateOrder";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    
                    var userIdParam = command.CreateParameter();
                    userIdParam.ParameterName = "@UserId";
                    userIdParam.Value = userId;
                    command.Parameters.Add(userIdParam);

                    var subTotalParam = command.CreateParameter();
                    subTotalParam.ParameterName = "@SubTotal";
                    subTotalParam.Value = subTotal;
                    command.Parameters.Add(subTotalParam);

                    var shippingFeeParam = command.CreateParameter();
                    shippingFeeParam.ParameterName = "@ShippingFee";
                    shippingFeeParam.Value = shippingFee;
                    command.Parameters.Add(shippingFeeParam);

                    var discountParam = command.CreateParameter();
                    discountParam.ParameterName = "@DiscountAmount";
                    discountParam.Value = discountAmount;
                    command.Parameters.Add(discountParam);

                    var addressParam = command.CreateParameter();
                    addressParam.ParameterName = "@DeliveryAddress";
                    addressParam.Value = deliveryAddress ?? (object)DBNull.Value;
                    command.Parameters.Add(addressParam);

                    var notesParam = command.CreateParameter();
                    notesParam.ParameterName = "@Notes";
                    notesParam.Value = notes ?? (object)DBNull.Value;
                    command.Parameters.Add(notesParam);

                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int orderId = reader.GetInt32(reader.GetOrdinal("OrderId"));
                            string orderCode = reader.GetString(reader.GetOrdinal("OrderCode"));
                            return (orderId, orderCode);
                        }
                    }
                }
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }

            throw new InvalidOperationException("sp_CreateOrder did not return expected result set");
        }

        
        public virtual async Task AddOrderItemsAsync(int orderId, IEnumerable<OrderItem> items)
        {
            foreach (var item in items)
            {
                item.OrderId = orderId;
                await _context.OrderItems.AddAsync(item);
            }

            await _context.SaveChangesAsync();
        }

        
        public virtual async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_UpdateOrderStatus @OrderId = {0}, @NewStatus = {1}",
                orderId, newStatus);
            return await _context.Orders.AnyAsync(order => order.OrderId == orderId);
        }

        private IQueryable<Order> OrdersWithDetails()
        {
            return _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Menu!)
                        .ThenInclude(m => m.MenuProducts)
                            .ThenInclude(mp => mp.Product);
        }

        private async Task<IEnumerable<Order>> LoadOrderDetailsAsync(IReadOnlyCollection<Order> orders)
        {
            if (orders.Count == 0)
                return orders;

            var orderIds = orders.Select(order => order.OrderId).ToList();
            return await OrdersWithDetails()
                .AsNoTracking()
                .Where(order => orderIds.Contains(order.OrderId))
                .OrderByDescending(order => order.OrderDate)
                .ToListAsync();
        }
    }
}
