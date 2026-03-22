using Microsoft.AspNetCore.Mvc;
using Resonate_API.Classes;
using Resonate_API.Models;


namespace Resonate_API.Controllers
{
    [Route("/employee")]
    [EndpointGroupName("v1")]
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
                    x => x.Login == Login && x.Password == DBManager.HashPassword(Password)
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
        [Route("/GETCurrentEmployee")]
        [HttpGet]
        public ActionResult GetCurrentEmployee([FromHeader] string Authorization)
        {
            try
            {
                string token = Authorization?.Replace("Bearer ", "");
                if (string.IsNullOrEmpty(token))
                    return Unauthorized("Токен не предоставлен");
                int? employeeId = JwtToken.GetUserIdFromToken(token);
                if (employeeId == null)
                    return Unauthorized("Недействительный токен");
                var employee = databaseManager.Employees.Find(employeeId.Value);

                if (employee == null)
                    return NotFound("Сотрудник не найден");
                return Ok(new
                {
                    employee.Id,
                    employee.Full_Name,
                    employee.Login,
                    employee.Position
                });
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }
        [Route("/GETEmployees")]
        [HttpGet]
        public ActionResult GetEmployees()
        {
            try
            {
                var employees = databaseManager.Employees
                    .Select(c => new
                    {
                        Id = c.Id,
                        Full_Name = c.Full_Name,
                        Login = c.Login,
                        Password = c.Password,
                        Position = c.Position
                    })
                    .ToList();

                return Ok(employees);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        [Route("/GETEmployeeById")]
        [HttpGet]
        public ActionResult GetEmployeeById(int id)
        {
            try
            {
                var employee = databaseManager.Employees.Find(id);

                if (employee == null)
                    return NotFound($"Сотрудник {id} не найден");

                return Ok(employee);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        [Route("/POSTEmployee")]
        [HttpPost]
        public ActionResult PostEmployee([FromForm] string Full_Name, [FromForm] string Login, [FromForm] string Password, [FromForm] string Position)
        {
            try
            {
                var employees = new Employees
                {
                    Full_Name = Full_Name,
                    Login = Login,
                    Password = DBManager.HashPassword(Password),
                    Position = Position
                };

                databaseManager.Add(employees);
                databaseManager.SaveChanges();

                return CreatedAtAction(nameof(GetEmployeeById),
                new { id = employees.Id },
                new { Id = employees.Id, Full_Name = employees.Full_Name, Login = employees.Login, Password = employees.Password, Position = employees.Position });
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        [Route("/PUTEmployee")]
        [HttpPut]
        public ActionResult PutEmployee([FromForm] int id, [FromForm] string Full_Name,
                                [FromForm] string Login, [FromForm] string Password,
                                [FromForm] string Position)
        {
            try
            {
                var employee = databaseManager.Employees.Find(id);

                if (employee == null)
                    return NotFound($"Сотрудник с ID {id} не найден");

                employee.Full_Name = Full_Name;
                employee.Login = Login;

                if (!string.IsNullOrWhiteSpace(Password))
                {
                    employee.Password = DBManager.HashPassword(Password);
                }

                employee.Position = Position;

                databaseManager.SaveChanges();
                return Ok(new
                {
                    id = employee.Id,
                    Full_Name = employee.Full_Name,
                    Login = employee.Login,
                    Position = employee.Position,
                    Message = "Сотрудник успешно обновлен"
                });
            }
            catch (Exception exp)
            {
                return StatusCode(500, new { Error = exp.Message });
            }
        }

        [Route("/DELETEEmployees")]
        [HttpDelete]
        public ActionResult DeleteEmployees([FromForm] int id)
        {
            try
            {
                var employee = databaseManager.Employees.Find(id);

                if (employee == null)
                    return NotFound($"Сотрудник с ID {id} не найден");

                databaseManager.Remove(employee);
                databaseManager.SaveChanges();

                return Ok($"Сотрудник {employee.Full_Name} удален");
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }
    }
}
