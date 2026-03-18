namespace Resonate_API.Models.SupplyAuxiliaryClasses
{
    public class UpdateSupplyItemFullRequest
    {
        public int Id { get; set; } // 0 для новых товаров
        public int Product_id { get; set; }
        public int Quantity { get; set; }
        public decimal? Purchase_Price { get; set; }
        public string Action { get; set; } = "update"; // "update", "delete", "add"
    }
}
