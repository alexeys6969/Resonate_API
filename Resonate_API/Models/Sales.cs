using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resonate_API.Models
{
    public class Sales
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; }
        public int Employee_id { get; set; }
        [ForeignKey("Employee_id")]
        public virtual Employees Employee { get; set; }
        public DateTime Sale_Date {  get; set; }
        public decimal Total_Amount {  get; set; }
    }
}
