using Resonate_API.Models.SaleAuxiliaryClasses;

namespace Resonate_API.Models.SupplyAuxiliaryClasses
{
    public class UpdateSupplyFullRequest
    {
        public UpdateSupplyInfoRequest? Supply { get; set; }
        public List<UpdateSupplyItemFullRequest>? Items
        {
            get; set;
        }
    }
}
