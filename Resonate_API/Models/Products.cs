using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resonate_API.Models
{
    public class Products
    {
        [Key]
        public int Id { get; set; }
        public string Article {  get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Category_Id { get; set; }
        [ForeignKey("Category_Id")]
        public virtual Categories Category { get; set; }
        public decimal Price {  get; set; }
        public int Stock_Quantity {  get; set; }
    }
}
