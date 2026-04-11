using Microsoft.AspNetCore.Mvc;
using Resonate_API.Classes;
using Resonate_API.Models;

namespace Resonate_API.Controllers
{
    [Route("/supplier")]
    [EndpointGroupName("v5")]
    public class SupplierController : Controller
    {
        private DBManager databaseManager;
        public SupplierController()
        {
            databaseManager = new DBManager();
        }

        /// <summary>
        /// Возвращает список всех поставщиков с базовой контактной информацией.
        /// </summary>
        /// <returns>Массив объектов поставщиков, содержащих:
        /// <list type="bullet">
        /// <item><description>Id — уникальный идентификатор поставщика</description></item>
        /// <item><description>Name — наименование организации</description></item>
        /// <item><description>Contact — контактные данные (телефон, email, адрес)</description></item>
        /// </list>
        /// </returns>
        /// <response code="200">Список поставщиков успешно получен.</response>
        /// <response code="500">Внутренняя ошибка сервера при выполнении запроса.</response>
        [Route("/GETSuppliers")]
        [HttpGet]
        public ActionResult GetSuppliers()
        {
            try
            {
                var suppliers = databaseManager.Suppliers
                    .Select(c => new
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Contact = c.Contact_Info
                    })
                    .ToList();

                return Ok(suppliers);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Возвращает детальную информацию о конкретном поставщике по его идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор поставщика в базе данных.</param>
        /// <returns>Объект поставщика с полными контактными данными.</returns>
        /// <response code="200">Поставщик найден и возвращён.</response>
        /// <response code="404">Поставщик с указанным ID не существует.</response>
        /// <response code="500">Внутренняя ошибка сервера при выполнении запроса.</response>
        [Route("/GETSupplierById")]
        [HttpGet]
        public ActionResult GetSupplierById(int id)
        {
            try
            {
                var supplier = databaseManager.Suppliers
                    .Where(c => c.Id == id).First();

                if (supplier == null)
                    return NotFound($"Поставщик с ID {id} не найден");

                return Ok(supplier);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Создаёт нового поставщика в системе.
        /// </summary>
        /// <param name="name">Наименование поставщика (организации). Обязательное поле.</param>
        /// <param name="contact">Контактная информация: телефон, email, адрес. Может быть пустым.</param>
        /// <returns>Созданный объект поставщика с присвоенным системой ID.</returns>
        /// <response code="201">Поставщик успешно создан. URI нового ресурса указан в заголовке Location.</response>
        /// <response code="400">Ошибка валидации данных (например, дублирующееся название).</response>
        /// <response code="500">Внутренняя ошибка сервера при сохранении данных.</response>
        [Route("/POSTSupplier")]
        [HttpPost]
        public ActionResult PostCategory([FromForm] string name, [FromForm] string contact)
        {
            try
            {
                var supplier = new Suppliers
                {
                    Name = name,
                    Contact_Info = contact
                };
                databaseManager.Add(supplier);
                databaseManager.SaveChanges();

                return CreatedAtAction(nameof(GetSupplierById),
                new { id = supplier.Id },
                new { Id = supplier.Id, Name = supplier.Name, Contact = supplier.Contact_Info });
            }
            catch (Exception exp)
            {
                return StatusCode(500, new
                {
                    message = exp.Message,
                    innerMessage = exp.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Обновляет данные существующего поставщика по его идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор поставщика, которого необходимо обновить.</param>
        /// <param name="name">Новое наименование поставщика.</param>
        /// <param name="contact">Новая контактная информация.</param>
        /// <returns>Обновлённый объект поставщика с актуальными данными.</returns>
        /// <response code="200">Данные поставщика успешно обновлены.</response>
        /// <response code="404">Поставщик с указанным ID не найден.</response>
        /// <response code="500">Внутренняя ошибка сервера при обновлении данных.</response>ч
        [Route("/PUTSupplier")]
        [HttpPut]
        public ActionResult PutCategory([FromForm] int id, [FromForm] string name, [FromForm] string contact)
        {
            try
            {
                var supplier = databaseManager.Suppliers.Find(id);

                if (supplier == null)
                    return NotFound($"Поставщик с ID {id} не найден");

                supplier.Name = name;
                supplier.Contact_Info = contact;
                databaseManager.SaveChanges();

                return Ok(new
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    Contact = supplier.Contact_Info
                });
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Удаляет поставщика из системы по его идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор поставщика, подлежащего удалению.</param>
        /// <returns>Строковое сообщение, подтверждающее успешное удаление.</returns>
        /// <response code="200">Поставщик успешно удалён из базы данных.</response>
        /// <response code="404">Поставщик с указанным ID не найден.</response>
        /// <response code="409">Удаление невозможно: поставщик связан с активными поставками (ограничение внешнего ключа).</response>
        /// <response code="500">Внутренняя ошибка сервера при удалении данных.</response>
        [Route("/DELETESupplier")]
        [HttpDelete]
        public ActionResult DeleteSupplier([FromForm] int id)
        {
            try
            {
                var supplier = databaseManager.Suppliers.Find(id);

                if (supplier == null)
                    return NotFound($"Поставщик с ID {id} не найден");

                databaseManager.Remove(supplier);
                databaseManager.SaveChanges();

                return Ok($"Поставщик {supplier.Name} удалена");
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }
    }
}
