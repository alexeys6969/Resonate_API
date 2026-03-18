using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Resonate_API.Models
{
    public class Suppliers
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Contact_Info { get; set; }
    }
}
