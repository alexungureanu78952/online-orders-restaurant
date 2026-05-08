using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RestaurantOrderManagement.Data.Context;
using RestaurantOrderManagement.Services.Interfaces;

namespace RestaurantOrderManagement.Services.Implementations
{
    /// <summary>
    /// Service for generating business reports
    /// Uses parameterized stored procedures for data retrieval
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly RestaurantDbContext _context;

        public ReportService(RestaurantDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get order summary by status for a date range
        /// </summary>
        public async Task<IEnumerable<OrderSummaryDto>> GetOrderSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var results = await _context.Database
                    .SqlQuery<OrderSummaryDto>(
                        "EXEC dbo.sp_GetOrderSummary @FromDate = {0}, @ToDate = {1}",
                        fromDate, toDate)
                    .ToListAsync();

                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve order summary report", ex);
            }
        }

        /// <summary>
        /// Get revenue summary by status for a date range
        /// </summary>
        public async Task<IEnumerable<RevenueSummaryDto>> GetRevenueSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var results = await _context.Database
                    .SqlQuery<RevenueSummaryDto>(
                        "EXEC dbo.sp_GetRevenueSummary @FromDate = {0}, @ToDate = {1}",
                        fromDate, toDate)
                    .ToListAsync();

                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve revenue summary report", ex);
            }
        }

        /// <summary>
        /// Get current inventory levels by category
        /// </summary>
        public async Task<IEnumerable<InventorySummaryDto>> GetInventorySummaryAsync()
        {
            try
            {
                var results = await _context.Database
                    .SqlQuery<InventorySummaryDto>("EXEC dbo.sp_GetInventorySummary")
                    .ToListAsync();

                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve inventory summary report", ex);
            }
        }

        /// <summary>
        /// Get detailed orders for a date range with customer and revenue info
        /// </summary>
        public async Task<IEnumerable<OrderDetailDto>> GetOrderDetailsByDateRangeAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var results = await _context.Database
                    .SqlQuery<OrderDetailDto>(
                        "EXEC dbo.sp_GetOrdersByDateRange @FromDate = {0}, @ToDate = {1}",
                        fromDate, toDate)
                    .ToListAsync();

                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve order details report", ex);
            }
        }

        /// <summary>
        /// Calculate total revenue for a date range
        /// </summary>
        public async Task<decimal> GetTotalRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var summaries = await GetRevenueSummaryAsync(fromDate, toDate);
                return summaries.Sum(s => s.TotalRevenue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to calculate total revenue", ex);
            }
        }

        /// <summary>
        /// Get order count for a date range
        /// </summary>
        public async Task<int> GetOrderCountAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var summaries = await GetOrderSummaryAsync(fromDate, toDate);
                return summaries.Sum(s => s.OrderCount);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get order count", ex);
            }
        }
    }
}
