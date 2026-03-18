using Resonate_API.Models.SaleAuxiliaryClasses;

namespace Resonate_API.Models.SupplyAuxiliaryClasses
{
    public class CreateSupplyRequest
    {
        public int supplier_id { get; set; }
        public List<SupplyItemRequest> items { get; set; }
    }
}
