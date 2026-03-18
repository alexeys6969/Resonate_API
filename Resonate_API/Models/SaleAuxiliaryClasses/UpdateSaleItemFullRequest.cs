namespace Resonate_API.Models.SaleAuxiliaryClasses
{
    public class UpdateSaleItemFullRequest
    {
        public int Id { get; set; } // 0 для новых товаров
        public int Product_id { get; set; }
        public int Quantity { get; set; }
        public decimal? Price_At_Sale { get; set; }
        public string Action { get; set; } = "update"; // "update", "delete", "add"
    }
}
