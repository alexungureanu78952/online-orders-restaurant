using Microsoft.EntityFrameworkCore;
using RestaurantOrderManagement.Data.Context;
using RestaurantOrderManagement.Data.Models;

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
        public async Task<Order> GetOrderDetailsAsync(int orderId)
        {
            return await _context.Orders
                .FromSqlRaw("EXEC dbo.sp_GetOrderDetails @OrderId = {0}", orderId)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get all user orders using parameterized stored procedure sp_GetUserOrders
        /// </summary>
        public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId, int limit = 50)
        {
            return await _context.Orders
                .FromSqlRaw("EXEC dbo.sp_GetUserOrders @UserId = {0}, @Limit = {1}", userId, limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get all orders with optional filters using parameterized stored procedure sp_GetAllOrders
        /// </summary>
        public async Task<IEnumerable<Order>> GetAllOrdersAsync(string status = null, DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
        {
            return await _context.Orders
                .FromSqlRaw("EXEC dbo.sp_GetAllOrders @Status = {0}, @FromDate = {1}, @ToDate = {2}, @Limit = {3}",
                    status, fromDate, toDate, limit)
                .ToListAsync();
        }

        /// <summary>
        /// Create new order using parameterized stored procedure sp_CreateOrder
        /// </summary>
        public async Task<(int OrderId, string OrderCode)> CreateOrderAsync(int userId, decimal subTotal, decimal shippingFee, 
            decimal discountAmount, string deliveryAddress = null, string notes = null)
        {
            var result = await _context.Database.ExecuteScalarAsync(
                "EXEC dbo.sp_CreateOrder @UserId = {0}, @SubTotal = {1}, @ShippingFee = {2}, @DiscountAmount = {3}, @DeliveryAddress = {4}, @Notes = {5}",
                userId, subTotal, shippingFee, discountAmount, deliveryAddress, notes);
            
            // Parse result to get OrderId and OrderCode
            // In real implementation, you would handle the multiple result sets properly
            return (0, string.Empty);
        }

        /// <summary>
        /// Update order status using parameterized stored procedure sp_UpdateOrderStatus
        /// </summary>
        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var result = await _context.Database.ExecuteAsync(
                "EXEC dbo.sp_UpdateOrderStatus @OrderId = {0}, @NewStatus = {1}",
                orderId, newStatus);
            return result > 0;
        }
    }
}
