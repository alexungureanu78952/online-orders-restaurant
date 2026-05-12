using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantOrderManagement.Services.Interfaces
{
    /// <summary>
    /// Data Transfer Objects for reporting
    /// </summary>
    public class OrderSummaryDto
    {
        public string Status { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal AvgOrderValue { get; set; }
        public DateTime FirstOrder { get; set; }
        public DateTime LastOrder { get; set; }
    }

    public class RevenueSummaryDto
    {
        public string Status { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSubtotal { get; set; }
        public decimal TotalShipping { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgOrderTotal { get; set; }
    }

    public class InventorySummaryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int TotalStock { get; set; }
        public int AvgStockPerProduct { get; set; }
        public int MinStock { get; set; }
        public int MaxStock { get; set; }
        public int LowStockCount { get; set; }
    }

    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
    }

    /// <summary>
    /// Service for generating business reports
    /// Provides order summaries, revenue analytics, and inventory insights
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Get order summary by status for a date range
        /// </summary>
        Task<IEnumerable<OrderSummaryDto>> GetOrderSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get revenue summary by status for a date range
        /// </summary>
        Task<IEnumerable<RevenueSummaryDto>> GetRevenueSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get current inventory levels by category
        /// </summary>
        Task<IEnumerable<InventorySummaryDto>> GetInventorySummaryAsync();

        /// <summary>
        /// Get detailed orders for a date range with customer and revenue info
        /// </summary>
        Task<IEnumerable<OrderDetailDto>> GetOrderDetailsByDateRangeAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Calculate total revenue for a date range
        /// </summary>
        Task<decimal> GetTotalRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get order count for a date range
        /// </summary>
        Task<int> GetOrderCountAsync(DateTime? fromDate = null, DateTime? toDate = null);
    }
}
