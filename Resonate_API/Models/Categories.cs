using System.ComponentModel.DataAnnotations;

namespace Resonate_API.Models
{
    public class Categories
    {
        [Key]
        public int Id { get; set; }
        public string Name {  get; set; }
        public string Description {  get; set; }
    }
}
