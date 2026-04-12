using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resonate_API.Classes;
using Resonate_API.Models;

namespace Resonate_API.Controllers
{
    [Route("/product")]
    [EndpointGroupName("v3")]
    public class ProductController : Controller
    {
        private DBManager databaseManager;
        public ProductController()
        {
            databaseManager = new DBManager();
        }

        /// <summary>
        /// Возвращает полный каталог товаров с их основными характеристиками и привязанной категорией.
        /// </summary>
        /// <returns>Массив объектов товаров, отсортированный по ID.</returns>
        /// <response code="200">Список товаров успешно получен.</response>
        /// <response code="500">Внутренняя ошибка сервера при выполнении запроса.</response>
        [Route("/GETProducts")]
        [HttpGet]
        public ActionResult GetProducts()
        {
            try
            {
                var products = databaseManager.Products
                    .OrderBy(p => p.Id)
                    .Select(c => new
                    {
                        Id = c.Id,
                        Article = c.Article,
                        Name = c.Name,
                        Description = c.Description,
                        Category = c.Category,
                        Price = c.Price,
                        Stock_Quantity = c.Stock_Quantity
                    })
                    .ToList();

                return Ok(products);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Возвращает детальную информацию о конкретном товаре по его уникальному идентификатору.
        /// </summary>
        /// <param name="id">Числовой идентификатор товара в базе данных.</param>
        /// <returns>Объект товара с полными данными, включая вложенный объект категории.</returns>
        /// <response code="200">Товар найден и возвращён.</response>
        /// <response code="404">Товар с указанным ID не существует.</response>
        /// <response code="500">Внутренняя ошибка сервера при выполнении запроса.</response>
        [Route("/GETProductById")]
        [HttpGet]
        public ActionResult GetProductById(int id)
        {
            try
            {
                var product = databaseManager.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);

                if (product == null)
                    return NotFound($"Товар {id} не найден");

                return Ok(product);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Создаёт новую карточку товара в каталоге.
        /// </summary>
        /// <param name="Article">Уникальный артикул товара (SKU).</param>
        /// <param name="Name">Название товара. Обязательное поле.</param>
        /// <param name="Description">Описание товара. Может быть пустым.</param>
        /// <param name="Category_Id">Идентификатор категории, к которой относится товар.</param>
        /// <param name="Price">Цена товара в рублях (десятичное число).</param>
        /// <param name="Stock_Quantity">Текущий остаток товара на складе.</param>
        /// <returns>Созданный объект товара с присвоенным системой ID.</returns>
        /// <response code="201">Товар успешно создан. URI нового ресурса указан в заголовке Location.</response>
        /// <response code="400">Ошибка валидации данных (например, дублирующийся артикул).</response>
        /// <response code="500">Внутренняя ошибка сервера при сохранении данных.</response>
        [Route("/POSTProduct")]
        [HttpPost]
        public ActionResult PostProduct(string token, [FromForm] string Article, [FromForm] string Name, [FromForm] string Description, [FromForm] int Category_Id, [FromForm] decimal Price, [FromForm] int Stock_Quantity)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .Where(x => x.Id == curUserId).First();
                if (currentUser.Position != "Администратор")
                    return StatusCode(403, "Доступ запрещён");
                var products = new Products
                {
                    Article = Article,
                    Name = Name,
                    Description = Description,
                    Category_Id = Category_Id,
                    Price = Price,
                    Stock_Quantity = Stock_Quantity
                };
                databaseManager.Add(products);
                databaseManager.SaveChanges();

                return CreatedAtAction(nameof(GetProductById),
                    new { id = products.Id },
                    new
                    {
                        Id = products.Id,
                        Article = products.Article,
                        Name = products.Name,
                        Description = products.Description,
                        Category_Id = products.Category_Id,
                        Price = products.Price,
                        Stock_Quantity = products.Stock_Quantity
                    });
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Обновляет данные существующего товара по его идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор товара, который необходимо обновить.</param>
        /// <param name="Article">Новый артикул товара.</param>
        /// <param name="Name">Новое название товара.</param>
        /// <param name="Description">Новое описание товара.</param>
        /// <param name="Category_Id">Новый идентификатор категории.</param>
        /// <param name="Price">Новая цена товара.</param>
        /// <param name="Stock_Quantity">Новый остаток на складе.</param>
        /// <returns>Обновлённый объект товара с актуальными данными.</returns>
        /// <response code="200">Данные товара успешно обновлены.</response>
        /// <response code="404">Товар с указанным ID не найден.</response>
        /// <response code="500">Внутренняя ошибка сервера при обновлении данных.</response>
        [Route("/PUTProduct")]
        [HttpPut]
        public ActionResult PutProduct(string token, [FromForm] int id, [FromForm] string Article, [FromForm] string Name, [FromForm] string Description, [FromForm] int Category_Id, [FromForm] decimal Price, [FromForm] int Stock_Quantity)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .Where(x => x.Id == curUserId).First();
                if (currentUser.Position != "Администратор")
                    return StatusCode(403, "Доступ запрещён");
                var product = databaseManager.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);

                if (product == null)
                    return NotFound($"Товар с ID {id} не найден");

                product.Article = Article;
                product.Name = Name;
                product.Description = Description;
                product.Category_Id = Category_Id;
                product.Price = Price;
                product.Stock_Quantity = Stock_Quantity;
                databaseManager.SaveChanges();

                return Ok(new
                {
                    product.Article,
                    product.Name,
                    product.Description,
                    product.Category_Id,
                    product.Price,
                    product.Stock_Quantity
            });
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Удаляет товар из каталога по его идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор товара, подлежащего удалению.</param>
        /// <returns>Строковое сообщение, подтверждающее успешное удаление.</returns>
        /// <response code="200">Товар успешно удалён из базы данных.</response>
        /// <response code="404">Товар с указанным ID не найден.</response>
        /// <response code="409">Удаление невозможно: товар связан с активными продажами или поставками (ограничение внешнего ключа).</response>
        /// <response code="500">Внутренняя ошибка сервера при удалении данных.</response>
        [Route("/DELETEProducts")]
        [HttpDelete]
        public ActionResult DeleteProducts(string token, [FromForm] int id)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .Where(x => x.Id == curUserId).First();
                if (currentUser.Position != "Администратор")
                    return StatusCode(403, "Доступ запрещён");
                var product = databaseManager.Products.Find(id);

                if (product == null)
                    return NotFound($"Товар с ID {id} не найден");

                databaseManager.Remove(product);
                databaseManager.SaveChanges();

                return Ok($"Товар {product.Article} удален");
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }
    }
}
