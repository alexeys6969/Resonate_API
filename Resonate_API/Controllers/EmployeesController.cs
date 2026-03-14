using Microsoft.AspNetCore.Mvc;
using Resonate_API.Classes;
using Resonate_API.Models;


namespace Resonate_API.Controllers
{
    [Route("/employee")]
    public class EmployeesController : Controller
    {
        private DBManager databaseManager;
        public EmployeesController()
        {
            databaseManager = new DBManager();
        }

        [Route("/login")]
        [HttpPost]
        public ActionResult Login([FromForm] string Login, [FromForm] string Password)
        {
            try
            {
                Employees? AuthEmployee = databaseManager.Employees.Where(
                    x => x.Login == Login && x.Password == Password
                    ).FirstOrDefault();
                if (AuthEmployee == null)
                    return StatusCode(401);
                else
                {
                    string Token = JwtToken.Generate(AuthEmployee);
                    databaseManager.SaveChanges();
                    return Ok(new { token = Token });
                }
            }
            catch (Exception exp)
            {
                return StatusCode(501, exp.Message);
            }
        }
    }
}
