namespace PontelloApp.ViewModels
{
    public class ProductVariantVM
    {
        public int ProductId { get; set; }

        public string SKU_ExternalID { get; set; }
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }

        public List<VariantOptionVM> Options { get; set; } = new();
    }
}
