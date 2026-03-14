using System.ComponentModel.DataAnnotations;

namespace Resonate_API.Models
{
    public class Employees
    {
        [Key]
        public int Id { get; set; }
        public string Full_Name { get; set; }
        public string Login {  get; set; }
        public string Password { get; set; }
        public string Position { get; set; }

    }
}
