using System;
using System.Collections.Generic;
using System.Data;
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
                return await QueryStoredProcedureAsync<OrderSummaryDto>(
                    "dbo.sp_GetOrderSummary",
                    ("@FromDate", fromDate),
                    ("@ToDate", toDate));
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
                return await QueryStoredProcedureAsync<RevenueSummaryDto>(
                    "dbo.sp_GetRevenueSummary",
                    ("@FromDate", fromDate),
                    ("@ToDate", toDate));
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
                return await QueryStoredProcedureAsync<InventorySummaryDto>("dbo.sp_GetInventorySummary");
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
                return await QueryStoredProcedureAsync<OrderDetailDto>(
                    "dbo.sp_GetOrdersByDateRange",
                    ("@FromDate", fromDate),
                    ("@ToDate", toDate));
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

        private async Task<List<T>> QueryStoredProcedureAsync<T>(string storedProcedure, params (string Name, object? Value)[] parameters)
            where T : new()
        {
            var results = new List<T>();
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = storedProcedure;
                command.CommandType = CommandType.StoredProcedure;

                foreach (var (name, value) in parameters)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = name;
                    parameter.Value = value ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }

                using var reader = await command.ExecuteReaderAsync();
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanWrite)
                    .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

                while (await reader.ReadAsync())
                {
                    var item = new T();

                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        if (!properties.TryGetValue(reader.GetName(i), out var property) || reader.IsDBNull(i))
                            continue;

                        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                        var value = reader.GetValue(i);
                        property.SetValue(item, Convert.ChangeType(value, targetType));
                    }

                    results.Add(item);
                }
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }

            return results;
        }
    }
}
