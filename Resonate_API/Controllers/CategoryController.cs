using Microsoft.AspNetCore.Mvc;
using Resonate_API.Classes;
using Resonate_API.Models;

namespace Resonate_API.Controllers
{
    [Route("/category")]
    [EndpointGroupName("v2")]
    public class CategoryController : Controller
    {
        private DBManager databaseManager;
        public CategoryController()
        {
            databaseManager = new DBManager();
        }
        /// <summary>
        /// Возвращает полный список всех категорий товаров.
        /// </summary>
        /// <returns>Массив объектов категорий, содержащих идентификатор, название и описание.</returns>
        /// <response code="200">Список категорий успешно получен.</response>
        /// <response code="500">Внутренняя ошибка сервера при выполнении запроса.</response>
        [Route("/GETCategories")]
        [HttpGet]
        public ActionResult GetCategories()
        {
            try
            {
                var categories = databaseManager.Categories
                    .Select(c => new
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description
                    })
                    .ToList();

                return Ok(categories);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Возвращает данные конкретной категории по её уникальному идентификатору.
        /// </summary>
        /// <param name="id">Числовой идентификатор категории в базе данных.</param>
        /// <returns>Объект категории с полями Id, Name, Description.</returns>
        /// <response code="200">Категория найдена и возвращена.</response>
        /// <response code="404">Категория с указанным ID не существует.</response>
        /// <response code="500">Внутренняя ошибка сервера при выполнении запроса.</response>
        [Route("/GETCategoryById")]
        [HttpGet]
        public ActionResult GetCategoryById(int id)
        {
            try
            {
                var category = databaseManager.Categories
                    .Where(c => c.Id == id).First();

                if (category == null)
                    return NotFound($"Категория с ID {id} не найдена");

                return Ok(category);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Создаёт новую категорию товаров в системе.
        /// </summary>
        /// <param name="name">Название категории. Обязательное поле.</param>
        /// <param name="description">Краткое описание категории. Может быть пустым.</param>
        /// <returns>Созданный объект категории с присвоенным системой ID.</returns>
        /// <response code="201">Категория успешно создана. В заголовке Location указан URI нового ресурса.</response>
        /// <response code="500">Внутренняя ошибка сервера при сохранении данных.</response>
        [Route("/POSTCategory")]
        [HttpPost]
        public ActionResult PostCategory([FromForm] string name, [FromForm] string description)
        {
            try
            {
                var category = new Categories
                {
                    Name = name,
                    Description = description

                };
                databaseManager.Add(category);
                databaseManager.SaveChanges();

                return CreatedAtAction(nameof(GetCategoryById),
                new { id = category.Id },
                new { Id = category.Id, Name = category.Name, Description = category.Description });
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Обновляет существующую категорию по её идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор категории, которую необходимо обновить.</param>
        /// <param name="name">Новое название категории.</param>
        /// <param name="description">Новое описание категории.</param>
        /// <returns>Обновлённый объект категории с актуальными данными.</returns>
        /// <response code="200">Данные категории успешно обновлены.</response>
        /// <response code="404">Категория с указанным ID не найдена.</response>
        /// <response code="500">Внутренняя ошибка сервера при обновлении данных.</response>
        [Route("/PUTCategory")]
        [HttpPut]
        public ActionResult PutCategory([FromForm] int id, [FromForm] string name, [FromForm] string description)
        {
            try
            {
                var category = databaseManager.Categories.Find(id);

                if (category == null)
                    return NotFound($"Категория с ID {id} не найдена");

                category.Name = name;
                category.Description = description;
                databaseManager.SaveChanges();

                return Ok(new
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description
                });
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Удаляет категорию товаров по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор категории, подлежащей удалению.</param>
        /// <returns>Строковое сообщение, подтверждающее успешное удаление.</returns>
        /// <response code="200">Категория успешно удалена из базы данных.</response>
        /// <response code="404">Категория с указанным ID не найдена.</response>
        /// <response code="409">Удаление невозможно: категория связана с другими записями (ограничения внешнего ключа).</response>
        /// <response code="500">Внутренняя ошибка сервера при удалении данных.</response>
        [Route("/DELETECategory")]
        [HttpDelete]
        public ActionResult DeleteCategory([FromForm] int id)
        {
            try
            {
                var category = databaseManager.Categories.Find(id);

                if (category == null)
                    return NotFound($"Категория с ID {id} не найдена");

                databaseManager.Remove(category);
                databaseManager.SaveChanges();

                return Ok($"Категория {category.Name} удалена");
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }
    }
}
