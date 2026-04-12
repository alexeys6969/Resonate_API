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

        /// <summary>
        /// Выполняет аутентификацию сотрудника и возвращает JWT-токен для доступа к API.
        /// </summary>
        /// <param name="Login">Логин сотрудника (уникальный идентификатор входа).</param>
        /// <param name="Password">Пароль сотрудника в открытом виде (будет хеширован на сервере).</param>
        /// <returns>Объект, содержащий JWT-токен авторизации.</returns>
        /// <response code="200">Аутентификация успешна. Возвращён токен.</response>
        /// <response code="401">Неверный логин или пароль. Доступ запрещён.</response>
        /// <response code="500">Внутренняя ошибка сервера при проверке учётных данных.</response>
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
        /// <summary>
        /// Возвращает данные текущего авторизованного сотрудника на основе JWT-токена из заголовка запроса.
        /// </summary>
        /// <param name="Authorization">Заголовок авторизации в формате "Bearer {token}".</param>
        /// <returns>Базовая информация о сотруднике: Id, Full_Name, Position.</returns>
        /// <response code="200">Данные сотрудника успешно получены.</response>
        /// <response code="401">Токен не предоставлен или является недействительным.</response>
        /// <response code="404">Сотрудник, привязанный к токену, не найден в базе.</response>
        /// <response code="500">Внутренняя ошибка сервера при обработке запроса.</response>
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
                    employee.Position
                });
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }
        /// <summary>
        /// Возвращает список всех сотрудников. Объём данных зависит от роли запрашивающего пользователя.
        /// Администратор получает полные данные (включая логин и хеш пароля), остальные — только базовую информацию.
        /// </summary>
        /// <param name="token">JWT-токен авторизации для определения роли пользователя.</param>
        /// <returns>Массив объектов сотрудников, отфильтрованный по уровню доступа.</returns>
        /// <response code="200">Список сотрудников успешно получен.</response>
        /// <response code="401/403">Токен отсутствует или пользователь не имеет прав доступа.</response>
        /// <response code="500">Внутренняя ошибка сервера при выполнении запроса.</response>
        [Route("/GETEmployees")]
        [HttpGet]
        public ActionResult GetEmployees(string token)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .Where(x => x.Id == curUserId).First();
                object employees = null;
                if(currentUser.Position == "Администратор")
                {
                    employees = databaseManager.Employees
                        .Select(c => new
                        {
                            Id = c.Id,
                            Full_Name = c.Full_Name,
                            Position = c.Position,
                            Login = c.Login,
                            Password = c.Password
                        })
                        .ToList();

                } else
                {
                    employees = databaseManager.Employees
                        .Select(c => new
                        {
                            Id = c.Id,
                            Full_Name = c.Full_Name,
                            Position = c.Position
                        })
                        .ToList();
                }
                return Ok(employees);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Возвращает данные конкретного сотрудника по его ID. Доступ к конфиденциальным полям определяется ролью.
        /// </summary>
        /// <param name="id">Уникальный идентификатор сотрудника.</param>
        /// <param name="token">JWT-токен авторизации для проверки прав доступа.</param>
        /// <returns>Объект сотрудника с полями, доступными текущей роли.</returns>
        /// <response code="200">Данные сотрудника успешно получены.</response>
        /// <response code="404">Сотрудник с указанным ID не найден.</response>
        /// <response code="500">Внутренняя ошибка сервера при выполнении запроса.</response>
        [Route("/GETEmployeeById")]
        [HttpGet]
        public ActionResult GetEmployeeById(int id, string token)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .Where(x => x.Id == curUserId).First();
                object employee = null;
                if (currentUser.Position == "Администратор")
                {
                    employee = databaseManager.Employees
                    .Select(c => new
                    {
                        Id = c.Id,
                        Full_Name = c.Full_Name,
                        Position = c.Position,
                        Login = c.Login,
                        Password = c.Password
                    })
                    .Where(x => x.Id == id);
                } else
                {
                    employee = databaseManager.Employees
                    .Select(c => new
                    {
                        Id = c.Id,
                        Full_Name = c.Full_Name,
                        Position = c.Position
                    })
                    .Where(x => x.Id == id);
                }
                if (employee == null)
                    return NotFound($"Сотрудник {id} не найден");

                return Ok(employee);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Создаёт нового сотрудника в системе. Пароль автоматически хешируется перед сохранением.
        /// </summary>
        /// <param name="Full_Name">ФИО сотрудника.</param>
        /// <param name="Login">Уникальный логин для входа в систему.</param>
        /// <param name="Password">Пароль в открытом виде (будет захеширован).</param>
        /// <param name="Position">Должность сотрудника.</param>
        /// <returns>Созданный объект сотрудника с присвоенным ID.</returns>
        /// <response code="201">Сотрудник успешно создан. URI нового ресурса указан в заголовке Location.</response>
        /// <response code="409">Нарушение уникальности (логин уже существует).</response>
        /// <response code="500">Внутренняя ошибка сервера при сохранении данных.</response>
        [Route("/POSTEmployee")]
        [HttpPost]
        public ActionResult PostEmployee(string token, [FromForm] string Full_Name, [FromForm] string Login, [FromForm] string Password, [FromForm] string Position)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .Where(x => x.Id == curUserId).First();
                if(currentUser.Position != "Администратор")
                    return StatusCode(403, "Доступ запрещён");
                else
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
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Обновляет данные существующего сотрудника. Если пароль не указан, он остаётся без изменений.
        /// </summary>
        /// <param name="id">Идентификатор сотрудника для обновления.</param>
        /// <param name="Full_Name">Новое ФИО сотрудника.</param>
        /// <param name="Login">Новый логин.</param>
        /// <param name="Password">Новый пароль (опционально). Если пустой — пароль не меняется.</param>
        /// <param name="Position">Новая должность.</param>
        /// <returns>Обновлённый объект сотрудника с подтверждением операции.</returns>
        /// <response code="200">Данные сотрудника успешно обновлены.</response>
        /// <response code="404">Сотрудник с указанным ID не найден.</response>
        /// <response code="500">Внутренняя ошибка сервера при обновлении данных.</response>
        [Route("/PUTEmployee")]
        [HttpPut]
        public ActionResult PutEmployee(string token, [FromForm] int id, [FromForm] string Full_Name, [FromForm] string Login, [FromForm] string Password, [FromForm] string Position)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .Where(x => x.Id == curUserId).First();
                if (currentUser.Position != "Администратор")
                    return StatusCode(403, "Доступ запрещён");
                else
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
                    
            }
            catch (Exception exp)
            {
                return StatusCode(500, new { Error = exp.Message });
            }
        }

        /// <summary>
        /// Удаляет сотрудника из системы по его идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор сотрудника, подлежащего удалению.</param>
        /// <returns>Строковое сообщение, подтверждающее успешное удаление.</returns>
        /// <response code="200">Сотрудник успешно удалён.</response>
        /// <response code="404">Сотрудник с указанным ID не найден.</response>
        /// <response code="409">Удаление невозможно: сотрудник связан с другими записями (например, продажи или смены).</response>
        /// <response code="500">Внутренняя ошибка сервера при удалении данных.</response>
        [Route("/DELETEEmployees")]
        [HttpDelete]
        public ActionResult DeleteEmployees(string token, [FromForm] int id)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .Where(x => x.Id == curUserId).First();
                if (currentUser.Position != "Администратор")
                    return StatusCode(403, "Доступ запрещён");
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
