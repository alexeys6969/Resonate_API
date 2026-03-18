using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Resonate_API.Models
{
    public class Supply_Items
    {
        [Key]
        public int Id { get; set; }
        public int Supply_id { get; set; }
        [ForeignKey("Supply_id")]
        public virtual Supplies Supply { get; set; }
        public int Product_id { get; set; }
        [ForeignKey("Product_id")]
        public virtual Products Product { get; set; }
        public int Quantity { get; set; }
        public decimal Purchase_Price { get; set; }
    }
}
