using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resonate_API.Models
{
    public class Supplies
    {
        [Key]
        public int Id { get; set; }
        public int Supplier_id { get; set; }
        [ForeignKey("Supplier_id")]
        public virtual Suppliers Suppliers { get; set; }
        public DateTime Supply_Date { get; set; }
        public decimal Total_Amount { get; set; }
    }
}
