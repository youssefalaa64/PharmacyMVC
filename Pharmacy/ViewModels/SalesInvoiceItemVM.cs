namespace Pharmacy.ViewModels
{
    public class SalesInvoiceItemVM
    {
        public int Id { get; set; }
        public int ProductBatchId { get; set; }
        public string? ProductName { get; set; }
        public string? BatchNumber { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
