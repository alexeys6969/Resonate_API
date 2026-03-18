namespace Resonate_API.Models.SaleAuxiliaryClasses
{
    public class CreateSaleRequest
    {
        public int employee_id { get; set; }
        public List<SaleItemRequest> items { get; set; }
    }
}
