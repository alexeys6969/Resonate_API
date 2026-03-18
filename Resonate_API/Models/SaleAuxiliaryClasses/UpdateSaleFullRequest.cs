namespace Resonate_API.Models.SaleAuxiliaryClasses
{
    public class UpdateSaleFullRequest
    {
        public UpdateSaleInfoRequest? Sale { get; set; }
        public List<UpdateSaleItemFullRequest>? Items
        {
            get; set;
        }
    }
}
