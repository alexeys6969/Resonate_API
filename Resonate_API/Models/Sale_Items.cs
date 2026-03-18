using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Resonate_API.Models
{
    public class Sale_Items
    {
        [Key]
        public int Id { get; set; }
        public int Sale_id { get; set; }
        [ForeignKey("Sale_id")]
        public virtual Sales Sale { get; set; }
        public int Product_id { get; set; }
        [ForeignKey("Product_id")]
        public virtual Products Product { get; set; }
        public int Quantity { get; set; }
        public decimal Price_At_Sale { get; set; }
    }
}
