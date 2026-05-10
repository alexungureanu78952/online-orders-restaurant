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

        /// <summary>
        /// Get order details by ID using parameterized stored procedure sp_GetOrderDetails
        /// </summary>
        public virtual async Task<Order> GetOrderDetailsAsync(int orderId)
        {
            return await _context.Orders
                .FromSqlRaw("EXEC dbo.sp_GetOrderDetails @OrderId = {0}", orderId)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get all user orders using parameterized stored procedure sp_GetUserOrders
        /// </summary>
        public virtual async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId, int limit = 50)
        {
            return await _context.Orders
                .FromSqlRaw("EXEC dbo.sp_GetUserOrders @UserId = {0}, @Limit = {1}", userId, limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get all orders with optional filters using parameterized stored procedure sp_GetAllOrders
        /// </summary>
        public virtual async Task<IEnumerable<Order>> GetAllOrdersAsync(string status = null, DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
        {
            return await _context.Orders
                .FromSqlRaw("EXEC dbo.sp_GetAllOrders @Status = {0}, @FromDate = {1}, @ToDate = {2}, @Limit = {3}",
                    status, fromDate, toDate, limit)
                .ToListAsync();
        }

        /// <summary>
        /// Create new order using parameterized stored procedure sp_CreateOrder
        /// Returns OrderId and OrderCode from sp_CreateOrder result set
        /// </summary>
        public virtual async Task<(int OrderId, string OrderCode)> CreateOrderAsync(int userId, decimal subTotal, decimal shippingFee,
            decimal discountAmount, string deliveryAddress = null, string notes = null)
        {
            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.sp_CreateOrder";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    // Add parameterized inputs
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

                    // Execute and read result
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

            throw new InvalidOperationException("sp_CreateOrder did not return expected result set");
        }

        /// <summary>
        /// Update order status using parameterized stored procedure sp_UpdateOrderStatus
        /// </summary>
        public virtual async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_UpdateOrderStatus @OrderId = {0}, @NewStatus = {1}",
                orderId, newStatus);
            return result > 0;
        }
    }
}
