using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantOrderManagement.WPF.ViewModels
{
    public partial class ReportsViewModel : ObservableObject
    {
        private readonly IReportService _reportService;

        [ObservableProperty]
        private DateTime reportFromDate = DateTime.Now.AddMonths(-1);

        [ObservableProperty]
        private DateTime reportToDate = DateTime.Now;

        [ObservableProperty]
        private ObservableCollection<OrderSummaryDisplay> orderSummaryData = new();

        [ObservableProperty]
        private int totalOrdersCount = 0;

        [ObservableProperty]
        private decimal totalOrdersAvgValue = 0;

        [ObservableProperty]
        private ObservableCollection<RevenueSummaryDisplay> revenueSummaryData = new();

        [ObservableProperty]
        private decimal totalRevenue = 0;

        [ObservableProperty]
        private decimal totalShippingFee = 0;

        [ObservableProperty]
        private decimal totalDiscount = 0;

        [ObservableProperty]
        private ObservableCollection<InventorySummaryDisplay> inventorySummaryData = new();

        [ObservableProperty]
        private ObservableCollection<OrderDetailDisplay> orderDetailsData = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private int selectedReportTab = 0;

        public ReportsViewModel(IReportService reportService)
        {
            _reportService = reportService;
        }

        [RelayCommand]
        public async Task LoadReportsAsync()
        {
            await GenerateOrderSummaryAsync();
            await GenerateRevenueSummaryAsync();
            await GenerateInventorySummaryAsync();
            await GenerateOrderDetailsAsync();
            SelectedReportTab = 0;
        }

        [RelayCommand]
        public async Task GenerateOrderSummaryAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                var (fromDate, toDate) = GetInclusiveReportRange();
                var summaries = await _reportService.GetOrderSummaryAsync(fromDate, toDate);
                var summaryList = summaries.ToList();

                OrderSummaryData.Clear();
                foreach (var summary in summaryList)
                {
                    OrderSummaryData.Add(new OrderSummaryDisplay
                    {
                        Status = summary.Status,
                        OrderCount = summary.OrderCount,
                        AvgOrderValue = summary.AvgOrderValue,
                        FirstOrder = summary.FirstOrder,
                        LastOrder = summary.LastOrder
                    });
                }

                TotalOrdersCount = summaryList.Sum(s => s.OrderCount);
                TotalOrdersAvgValue = summaryList.Count == 0
                    ? 0
                    : summaryList.Average(s => s.AvgOrderValue);

                SelectedReportTab = 0;
                SuccessMessage = $"Order summary loaded: {TotalOrdersCount} orders";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to generate order summary: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task GenerateRevenueSummaryAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                var (fromDate, toDate) = GetInclusiveReportRange();
                var summaries = await _reportService.GetRevenueSummaryAsync(fromDate, toDate);
                var summaryList = summaries.ToList();

                RevenueSummaryData.Clear();
                foreach (var summary in summaryList)
                {
                    RevenueSummaryData.Add(new RevenueSummaryDisplay
                    {
                        Status = summary.Status,
                        OrderCount = summary.OrderCount,
                        TotalSubtotal = summary.TotalSubtotal,
                        TotalShipping = summary.TotalShipping,
                        TotalDiscount = summary.TotalDiscount,
                        TotalRevenue = summary.TotalRevenue,
                        AvgOrderTotal = summary.AvgOrderTotal
                    });
                }

                TotalRevenue = summaryList.Sum(s => s.TotalRevenue);
                TotalShippingFee = summaryList.Sum(s => s.TotalShipping);
                TotalDiscount = summaryList.Sum(s => s.TotalDiscount);

                SelectedReportTab = 1;
                SuccessMessage = $"Revenue summary loaded: ${TotalRevenue:F2} total revenue";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to generate revenue summary: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task GenerateInventorySummaryAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                var summaries = await _reportService.GetInventorySummaryAsync();

                InventorySummaryData.Clear();
                foreach (var summary in summaries)
                {
                    InventorySummaryData.Add(new InventorySummaryDisplay
                    {
                        CategoryId = summary.CategoryId,
                        CategoryName = summary.CategoryName,
                        ProductCount = summary.ProductCount,
                        TotalStock = summary.TotalStock,
                        AvgStockPerProduct = summary.AvgStockPerProduct,
                        MinStock = summary.MinStock,
                        MaxStock = summary.MaxStock,
                        LowStockCount = summary.LowStockCount
                    });
                }

                SelectedReportTab = 2;
                SuccessMessage = "Inventory summary loaded";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to generate inventory summary: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task GenerateOrderDetailsAsync()
        {
            try
            {
                IsLoading = true;
                ClearMessages();

                var (fromDate, toDate) = GetInclusiveReportRange();
                var details = await _reportService.GetOrderDetailsByDateRangeAsync(fromDate, toDate);

                OrderDetailsData.Clear();
                foreach (var detail in details)
                {
                    OrderDetailsData.Add(new OrderDetailDisplay
                    {
                        OrderId = detail.OrderId,
                        OrderCode = detail.OrderCode,
                        CustomerName = detail.CustomerName,
                        Email = detail.Email,
                        CreatedDate = detail.CreatedDate,
                        Status = detail.Status,
                        SubTotal = detail.SubTotal,
                        ShippingFee = detail.ShippingFee,
                        DiscountAmount = detail.DiscountAmount,
                        Total = detail.Total,
                        ItemCount = detail.ItemCount
                    });
                }

                SelectedReportTab = 3;
                SuccessMessage = $"Order details loaded: {OrderDetailsData.Count} orders";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to generate order details: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ExportOrderSummaryToCsvAsync()
        {
            try
            {
                if (OrderSummaryData.Count == 0)
                {
                    ErrorMessage = "No order summary data to export. Generate a report first.";
                    return;
                }

                string fileName = $"OrderSummary_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string csvContent = GenerateOrderSummaryCSV();
                await SaveCsvFileAsync(fileName, csvContent);

                SuccessMessage = $"Order summary exported to {fileName}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to export order summary: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task ExportRevenueSummaryToCsvAsync()
        {
            try
            {
                if (RevenueSummaryData.Count == 0)
                {
                    ErrorMessage = "No revenue summary data to export. Generate a report first.";
                    return;
                }

                string fileName = $"RevenueSummary_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string csvContent = GenerateRevenueSummaryCSV();
                await SaveCsvFileAsync(fileName, csvContent);

                SuccessMessage = $"Revenue summary exported to {fileName}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to export revenue summary: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task ExportInventorySummaryToCsvAsync()
        {
            try
            {
                if (InventorySummaryData.Count == 0)
                {
                    ErrorMessage = "No inventory summary data to export. Generate a report first.";
                    return;
                }

                string fileName = $"InventorySummary_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string csvContent = GenerateInventorySummaryCSV();
                await SaveCsvFileAsync(fileName, csvContent);

                SuccessMessage = $"Inventory summary exported to {fileName}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to export inventory summary: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task ExportOrderDetailsToCsvAsync()
        {
            try
            {
                if (OrderDetailsData.Count == 0)
                {
                    ErrorMessage = "No order details data to export. Generate a report first.";
                    return;
                }

                string fileName = $"OrderDetails_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string csvContent = GenerateOrderDetailsCSV();
                await SaveCsvFileAsync(fileName, csvContent);

                SuccessMessage = $"Order details exported to {fileName}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to export order details: {ex.Message}";
            }
        }

        private string GenerateOrderSummaryCSV()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Status,Order Count,Avg Order Value,First Order,Last Order");

            foreach (var item in OrderSummaryData)
            {
                sb.AppendLine($"{EscapeCSVField(item.Status)},{item.OrderCount}," +
                    $"${item.AvgOrderValue:F2}," +
                    $"{item.FirstOrder:yyyy-MM-dd HH:mm:ss}," +
                    $"{item.LastOrder:yyyy-MM-dd HH:mm:ss}");
            }

            sb.AppendLine();
            sb.AppendLine($"Total Orders,{TotalOrdersCount}");
            sb.AppendLine($"Average Order Value,${TotalOrdersAvgValue:F2}");

            return sb.ToString();
        }

        private string GenerateRevenueSummaryCSV()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Status,Order Count,Subtotal,Shipping,Discount,Total Revenue,Avg Order Total");

            foreach (var item in RevenueSummaryData)
            {
                sb.AppendLine($"{EscapeCSVField(item.Status)},{item.OrderCount}," +
                    $"${item.TotalSubtotal:F2}," +
                    $"${item.TotalShipping:F2}," +
                    $"${item.TotalDiscount:F2}," +
                    $"${item.TotalRevenue:F2}," +
                    $"${item.AvgOrderTotal:F2}");
            }

            sb.AppendLine();
            sb.AppendLine($"Total Revenue,${TotalRevenue:F2}");
            sb.AppendLine($"Total Shipping,${TotalShippingFee:F2}");
            sb.AppendLine($"Total Discounts,${TotalDiscount:F2}");

            return sb.ToString();
        }

        private string GenerateInventorySummaryCSV()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Category,Product Count,Total Stock,Avg Stock,Min Stock,Max Stock,Low Stock Items");

            foreach (var item in InventorySummaryData)
            {
                sb.AppendLine($"{EscapeCSVField(item.CategoryName)},{item.ProductCount}," +
                    $"{item.TotalStock}g,{item.AvgStockPerProduct}g," +
                    $"{item.MinStock}g,{item.MaxStock}g,{item.LowStockCount}");
            }

            return sb.ToString();
        }

        private string GenerateOrderDetailsCSV()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Order Code,Customer,Email,Date,Status,Subtotal,Shipping,Discount,Total,Items");

            foreach (var item in OrderDetailsData)
            {
                sb.AppendLine($"{EscapeCSVField(item.OrderCode)}," +
                    $"{EscapeCSVField(item.CustomerName)}," +
                    $"{EscapeCSVField(item.Email)}," +
                    $"{item.CreatedDate:yyyy-MM-dd HH:mm:ss}," +
                    $"{EscapeCSVField(item.Status)}," +
                    $"${item.SubTotal:F2}," +
                    $"${item.ShippingFee:F2}," +
                    $"${item.DiscountAmount:F2}," +
                    $"${item.Total:F2}," +
                    $"{item.ItemCount}");
            }

            return sb.ToString();
        }

        private string EscapeCSVField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
                return "\"" + field.Replace("\"", "\"\"") + "\"";

            return field;
        }

        private async Task SaveCsvFileAsync(string fileName, string content)
        {
            try
            {
                string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string reportsFolder = Path.Combine(documentsFolder, "RestaurantOrderManagement_Reports");

                if (!Directory.Exists(reportsFolder))
                    Directory.CreateDirectory(reportsFolder);

                string filePath = Path.Combine(reportsFolder, fileName);
                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save CSV file: {ex.Message}", ex);
            }
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }

        private (DateTime FromDate, DateTime ToDate) GetInclusiveReportRange()
        {
            var fromDate = ReportFromDate.Date;
            var toDate = ReportToDate.Date.AddDays(1).AddTicks(-1);

            if (toDate < fromDate)
            {
                (fromDate, toDate) = (toDate.Date, fromDate.Date.AddDays(1).AddTicks(-1));
            }

            return (fromDate, toDate);
        }

    }

    public class OrderSummaryDisplay
    {
        public string Status { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal AvgOrderValue { get; set; }
        public DateTime FirstOrder { get; set; }
        public DateTime LastOrder { get; set; }
    }

    public class RevenueSummaryDisplay
    {
        public string Status { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSubtotal { get; set; }
        public decimal TotalShipping { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgOrderTotal { get; set; }
    }

    public class InventorySummaryDisplay
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

    public class OrderDetailDisplay
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
}
