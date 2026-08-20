namespace Pharmacy.ViewModels
{
    public class AdminDashboardVM
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalProductBatches { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalSalesInvoices { get; set; }
        public decimal TotalSales { get; set; }
        // Order Statistics
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
    }
}
