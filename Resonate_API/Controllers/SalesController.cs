using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resonate_API.Classes;
using Resonate_API.Models;
using Resonate_API.Models.SaleAuxiliaryClasses;

namespace Resonate_API.Controllers
{
    [Route("/sale")]
    [EndpointGroupName("v4")]
    public class SalesController : Controller
    {
        private DBManager databaseManager;

        public SalesController()
        {
            databaseManager = new DBManager();
        }

        /// <summary>
        /// Возвращает список всех продаж с детализацией по товарам и информации о кассире.
        /// Поле <c>Total_Amount</c> вычисляется автоматически на основе состава чека.
        /// </summary>
        /// <param name="token">JWT-токен авторизации для проверки прав доступа.</param>
        /// <returns>Массив объектов продаж с вычисляемой итоговой суммой.</returns>
        /// <response code="200">Список продаж успешно получен.</response>
        /// <response code="403">Доступ запрещён: недействительный токен или пользователь не найден.</response>
        /// <response code="500">Внутренняя ошибка сервера при выполнении запроса.</response>
        [Route("/GETSales")]
        [HttpGet]
        public ActionResult GetSales([FromQuery] string token)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .FirstOrDefault(x => x.Id == curUserId);

                if (currentUser == null)
                    return StatusCode(403, "Доступ запрещён");

                var salesWithItems = databaseManager.Sales
                    .Include(s => s.Sale_Items)  // 🔹 Обязательно для вычисления Total_Amount
                    .Include(s => s.Employee)
                    .GroupJoin(
                        databaseManager.Sale_Items,
                        sale => sale.Id,
                        item => item.Sale_id,
                        (sale, items) => new
                        {
                            Id = sale.Id,
                            Code = sale.Code,
                            Employee_id = sale.Employee_id,
                            Employee_Name = sale.Employee.Full_Name,
                            Employee_Position = sale.Employee.Position,
                            Sale_Date = sale.Sale_Date,
                            Total_Amount = sale.Total_Amount,  // ✅ Вычисляется автоматически
                            Items = items.Select(i => new
                            {
                                Id = i.Id,
                                Product_id = i.Product_id,
                                Name = i.Product.Name,
                                Quantity = i.Quantity,
                                Price_At_Sale = i.Price_At_Sale
                            }).ToList(),
                            ItemsCount = items.Count()
                        })
                    .ToList();

                return Ok(salesWithItems);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Возвращает детальную информацию о конкретной продаже по её идентификатору.
        /// Поле <c>Total_Amount</c> вычисляется автоматически на основе состава чека.
        /// </summary>
        /// <param name="token">JWT-токен авторизации для проверки прав доступа.</param>
        /// <param name="id">Уникальный идентификатор продажи в базе данных.</param>
        /// <returns>Объект продажи с полным составом чека и вычисляемой итоговой суммой.</returns>
        /// <response code="200">Продажа найдена и возвращена.</response>
        /// <response code="403">Доступ запрещён: недействительный токен или пользователь не найден.</response>
        /// <response code="404">Продажа с указанным ID не существует.</response>
        /// <response code="500">Внутренняя ошибка сервера при выполнении запроса.</response>
        [Route("/GETSaleById")]
        [HttpGet]
        public ActionResult GetSaleById([FromQuery] string token, int id)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .FirstOrDefault(x => x.Id == curUserId);

                if (currentUser == null)
                    return StatusCode(403, "Доступ запрещён");

                var saleWithItems = databaseManager.Sales
                    .Include(s => s.Sale_Items)  // 🔹 Обязательно для вычисления Total_Amount
                    .Include(s => s.Employee)
                    .GroupJoin(
                        databaseManager.Sale_Items,
                        sale => sale.Id,
                        item => item.Sale_id,
                        (sale, items) => new
                        {
                            Id = sale.Id,
                            Code = sale.Code,
                            Employee_id = sale.Employee_id,
                            Employee_Name = sale.Employee.Full_Name,
                            Employee_Position = sale.Employee.Position,
                            Sale_Date = sale.Sale_Date,
                            Total_Amount = sale.Total_Amount,  // ✅ Вычисляется автоматически
                            Items = items.Select(i => new
                            {
                                Id = i.Id,
                                Product_id = i.Product_id,
                                Name = i.Product.Name,
                                Quantity = i.Quantity,
                                Price_At_Sale = i.Price_At_Sale
                            }).ToList(),
                            ItemsCount = items.Count()
                        })
                    .FirstOrDefault(s => s.Id == id);

                if (saleWithItems == null)
                    return NotFound($"Продажа с ID {id} не найдена");

                return Ok(saleWithItems);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        /// <summary>
        /// Создаёт новую продажу с указанием кассира и списка товаров.
        /// Автоматически генерирует код продажи, проверяет остатки на складе и списывает товары.
        /// Поле <c>Total_Amount</c> вычисляется автоматически на основе добавленных товаров.
        /// </summary>
        /// <param name="token">JWT-токен авторизации для проверки прав доступа.</param>
        /// <param name="request">Объект запроса, содержащий:
        /// <list type="bullet">
        /// <item><description>employee_id — ID кассира, оформляющего продажу</description></item>
        /// <item><description>items — массив товаров: product_id, quantity, price_at_sale (опционально)</description></item>
        /// </list>
        /// </param>
        /// <returns>Созданная продажа с детализацией по товарам и вычисляемой итоговой суммой.</returns>
        /// <response code="200">Продажа успешно создана и сохранена в базе.</response>
        /// <response code="400">Ошибка валидации: пустой запрос, неверный employee_id, отсутствие товаров, недостаточный остаток на складе.</response>
        /// <response code="403">Доступ запрещён: недействительный токен или пользователь не найден.</response>
        /// <response code="404">Сотрудник или товар с указанным ID не найден.</response>
        /// <response code="500">Внутренняя ошибка сервера или откат транзакции.</response>
        [Route("/POSTSale")]
        [HttpPost]
        public ActionResult PostSale([FromQuery] string token, [FromBody] CreateSaleRequest request)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .FirstOrDefault(x => x.Id == curUserId);

                if (currentUser == null)
                    return StatusCode(403, "Доступ запрещён");

                if (request == null)
                {
                    return BadRequest(new
                    {
                        error = "Request body is null",
                        message = "Тело запроса не может быть пустым. Отправьте JSON с employee_id и items"
                    });
                }

                if (request.employee_id <= 0)
                {
                    return BadRequest(new
                    {
                        error = "Invalid employee_id",
                        message = "Укажите корректный ID сотрудника"
                    });
                }

                if (request.items == null || !request.items.Any())
                {
                    return BadRequest(new
                    {
                        error = "Items list is empty",
                        message = "Добавьте хотя бы один товар в продажу"
                    });
                }

                var employee = databaseManager.Employees.Find(request.employee_id);
                if (employee == null)
                {
                    return BadRequest(new
                    {
                        error = "Employee not found",
                        message = $"Сотрудник с ID {request.employee_id} не найден"
                    });
                }

                using (var transaction = databaseManager.Database.BeginTransaction())
                {
                    try
                    {
                        var sale = new Sales
                        {
                            Code = "SALE-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                            Employee_id = request.employee_id,
                            Sale_Date = DateTime.Now
                            // 🔹 Total_Amount не задаём — он вычислится автоматически через свойство
                        };

                        databaseManager.Sales.Add(sale);
                        databaseManager.SaveChanges();  // Сохраняем, чтобы получить sale.Id

                        var saleItems = new List<Sale_Items>();
                        var processedItems = new List<object>();

                        foreach (var item in request.items)
                        {
                            if (item.quantity <= 0)
                            {
                                transaction.Rollback();
                                return BadRequest(new
                                {
                                    error = "Invalid quantity",
                                    message = $"Количество товара должно быть больше 0. Product_id: {item.product_id}"
                                });
                            }

                            var product = databaseManager.Products.Find(item.product_id);
                            if (product == null)
                            {
                                transaction.Rollback();
                                return BadRequest(new
                                {
                                    error = "Product not found",
                                    message = $"Товар с ID {item.product_id} не найден"
                                });
                            }

                            if (product.Stock_Quantity < item.quantity)
                            {
                                transaction.Rollback();
                                return BadRequest(new
                                {
                                    error = "Insufficient stock",
                                    message = $"Недостаточно товара '{product.Name}'. Доступно: {product.Stock_Quantity}, запрошено: {item.quantity}"
                                });
                            }

                            product.Stock_Quantity -= item.quantity;

                            var saleItem = new Sale_Items
                            {
                                Sale_id = sale.Id,
                                Product_id = item.product_id,
                                Quantity = item.quantity,
                                Price_At_Sale = product.Price
                            };

                            saleItems.Add(saleItem);

                            processedItems.Add(new
                            {
                                product.Id,
                                product.Name,
                                product.Article,
                                Price = product.Price,
                                item.quantity,
                                Subtotal = product.Price * item.quantity
                            });
                        }

                        databaseManager.Sale_Items.AddRange(saleItems);
                        // 🔹 sale.Total_Amount = totalAmount;  // ❌ УДАЛЕНО: сумма вычислится автоматически
                        databaseManager.SaveChanges();  // После этого свойство Total_Amount вернёт актуальное значение
                        transaction.Commit();

                        // 🔹 При сериализации sale.Total_Amount автоматически вычислится
                        // Нужно явно загрузить коллекцию для вычисления
                        var saleWithItems = databaseManager.Sales
                            .Include(s => s.Sale_Items)
                            .Include(s => s.Employee)
                            .First(s => s.Id == sale.Id);

                        return Ok(new
                        {
                            sale = new
                            {
                                saleWithItems.Id,
                                saleWithItems.Code,
                                saleWithItems.Sale_Date,
                                saleWithItems.Total_Amount,  // ✅ Вычисляется автоматически
                                Employee = new { employee.Id, employee.Full_Name },
                                Items = processedItems,
                                Items_Count = processedItems.Count
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
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
        /// Обновляет существующую продажу: метаданные и/или состав товаров.
        /// Поддерживает три операции для каждого товара: <c>add</c>, <c>update</c>, <c>delete</c>.
        /// Автоматически корректирует остатки на складе и пересчитывает итоговую сумму.
        /// Поле <c>Total_Amount</c> вычисляется автоматически на основе актуального состава чека.
        /// </summary>
        /// <param name="token">JWT-токен авторизации для проверки прав доступа.</param>
        /// <param name="id">Идентификатор продажи, которую необходимо обновить.</param>
        /// <param name="request">Объект запроса, содержащий:
        /// <list type="bullet">
        /// <item><description>Sale — опциональные поля для обновления: Code, Employee_id, Sale_Date</description></item>
        /// <item><description>Items — массив операций над товарами:
        ///   <list type="bullet">
        ///   <item><description>Id — ID позиции в продаже (для update/delete)</description></item>
        ///   <item><description>Product_id — ID товара</description></item>
        ///   <item><description>Quantity — новое количество</description></item>
        ///   <item><description>Price_At_Sale — цена на момент продажи (опционально)</description></item>
        ///   <item><description>Action — операция: "add", "update", "delete"</description></item>
        ///   </list>
        /// </description></item>
        /// </list>
        /// </param>
        /// <returns>Обновлённая продажа с актуальным составом и вычисляемой итоговой суммой.</returns>
        /// <response code="200">Продажа успешно обновлена.</response>
        /// <response code="400">Ошибка валидации: недостаточный остаток товара, неверные данные.</response>
        /// <response code="403">Доступ запрещён: недействительный токен или пользователь не найден.</response>
        /// <response code="404">Продажа, сотрудник или товар с указанным ID не найден.</response>
        /// <response code="500">Внутренняя ошибка сервера или откат транзакции.</response>
        [Route("/PUTSale")]
        [HttpPut]
        public ActionResult PutSale([FromQuery] string token, int id, [FromBody] UpdateSaleFullRequest request)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .FirstOrDefault(x => x.Id == curUserId);

                if (currentUser == null)
                    return StatusCode(403, "Доступ запрещён");

                using (var transaction = databaseManager.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Находим продажу
                        var sale = databaseManager.Sales
                            .Include(s => s.Sale_Items)  // 🔹 Для вычисления суммы
                            .FirstOrDefault(s => s.Id == id);

                        if (sale == null)
                        {
                            return NotFound(new { message = $"Продажа с ID {id} не найдена" });
                        }

                        // 2. Загружаем все товары этой продажи
                        var existingItems = databaseManager.Sale_Items
                            .Where(i => i.Sale_id == id)
                            .ToList();

                        // 🔹 Исправлено:
                        var existingSaleItems = databaseManager.Sale_Items
                            .Where(i => i.Sale_id == id)
                            .ToList();

                        // 3. Обновляем основную информацию о продаже
                        if (request.Sale != null)
                        {
                            if (!string.IsNullOrEmpty(request.Sale.Code))
                                sale.Code = request.Sale.Code;

                            if (request.Sale.Employee_id.HasValue)
                            {
                                var employees = databaseManager.Employees.Find(request.Sale.Employee_id.Value);
                                if (employees == null)
                                {
                                    transaction.Rollback();
                                    return BadRequest(new { message = $"Сотрудник с ID {request.Sale.Employee_id.Value} не найден" });
                                }
                                sale.Employee_id = request.Sale.Employee_id.Value;
                            }

                            if (request.Sale.Sale_Date.HasValue)
                                sale.Sale_Date = request.Sale.Sale_Date.Value;
                        }

                        // 4. Словарь для быстрого доступа к существующим товарам
                        var itemsDict = existingSaleItems.ToDictionary(i => i.Id);

                        // 5. Множество для отслеживания обработанных ID
                        var processedIds = new HashSet<int>();

                        // 6. Список для новых товаров
                        var itemsToAdd = new List<Sale_Items>();

                        // 7. Обрабатываем каждый товар из запроса
                        if (request.Items != null)
                        {
                            foreach (var itemRequest in request.Items)
                            {
                                var product = databaseManager.Products.Find(itemRequest.Product_id);
                                if (product == null)
                                {
                                    transaction.Rollback();
                                    return BadRequest(new { message = $"Товар с ID {itemRequest.Product_id} не найден" });
                                }

                                switch (itemRequest.Action?.ToLower())
                                {
                                    case "delete":
                                        // УДАЛЕНИЕ товара
                                        if (itemsDict.TryGetValue(itemRequest.Id, out var itemToDelete))
                                        {
                                            // Возвращаем товар на склад
                                            product.Stock_Quantity += itemToDelete.Quantity;
                                            databaseManager.Sale_Items.Remove(itemToDelete);
                                            processedIds.Add(itemRequest.Id);
                                        }
                                        break;

                                    case "update":
                                        // ИЗМЕНЕНИЕ существующего товара
                                        if (itemsDict.TryGetValue(itemRequest.Id, out var itemToUpdate))
                                        {
                                            // Возвращаем старый товар на склад
                                            product.Stock_Quantity += itemToUpdate.Quantity;

                                            // Проверяем достаточно ли нового количества
                                            if (product.Stock_Quantity < itemRequest.Quantity)
                                            {
                                                transaction.Rollback();
                                                return BadRequest(new
                                                {
                                                    message = $"Недостаточно товара '{product.Name}'",
                                                    available = product.Stock_Quantity,
                                                    requested = itemRequest.Quantity
                                                });
                                            }

                                            // Забираем новое количество со склада
                                            product.Stock_Quantity -= itemRequest.Quantity;

                                            // Обновляем поля
                                            itemToUpdate.Quantity = itemRequest.Quantity;
                                            itemToUpdate.Price_At_Sale = itemRequest.Price_At_Sale ?? product.Price;
                                            processedIds.Add(itemRequest.Id);
                                        }
                                        break;

                                    case "add":
                                        // ДОБАВЛЕНИЕ нового товара
                                        if (product.Stock_Quantity < itemRequest.Quantity)
                                        {
                                            transaction.Rollback();
                                            return BadRequest(new
                                            {
                                                message = $"Недостаточно товара '{product.Name}' для добавления",
                                                available = product.Stock_Quantity,
                                                requested = itemRequest.Quantity
                                            });
                                        }

                                        // Забираем товар со склада
                                        product.Stock_Quantity -= itemRequest.Quantity;

                                        // Создаем новый элемент
                                        var newItem = new Sale_Items
                                        {
                                            Sale_id = sale.Id,
                                            Product_id = itemRequest.Product_id,
                                            Quantity = itemRequest.Quantity,
                                            Price_At_Sale = itemRequest.Price_At_Sale ?? product.Price
                                        };

                                        itemsToAdd.Add(newItem);
                                        break;

                                    default:
                                        // Если action не указан — считаем как добавление/обновление
                                        if (itemRequest.Id > 0 && itemsDict.TryGetValue(itemRequest.Id, out var existingItem))
                                        {
                                            goto case "update";
                                        }
                                        else
                                        {
                                            goto case "add";
                                        }
                                        break;
                                }
                            }
                        }

                        // 8. Добавляем новые товары в базу
                        if (itemsToAdd.Any())
                        {
                            databaseManager.Sale_Items.AddRange(itemsToAdd);
                        }

                        // 🔹 sale.Total_Amount = totalAmount;  // ❌ УДАЛЕНО: сумма вычислится автоматически
                        databaseManager.SaveChanges();  // После этого свойство вернёт актуальное значение
                        transaction.Commit();

                        // 🔹 Формируем ответ с актуальными данными (сумма вычислится при сериализации)
                        var updatedSale = databaseManager.Sales
                            .Include(s => s.Sale_Items)
                            .Include(s => s.Employee)
                            .First(s => s.Id == id);

                        var updatedItems = updatedSale.Sale_Items
                            .Select(i => new
                            {
                                i.Id,
                                i.Product_id,
                                ProductName = databaseManager.Products
                                    .Where(p => p.Id == i.Product_id)
                                    .Select(p => p.Name)
                                    .FirstOrDefault(),
                                i.Quantity,
                                i.Price_At_Sale,
                                Subtotal = i.Quantity * i.Price_At_Sale
                            })
                            .ToList();

                        var employee = databaseManager.Employees.Find(updatedSale.Employee_id);

                        return Ok(new
                        {
                            message = "Продажа успешно обновлена",
                            sale = new
                            {
                                updatedSale.Id,
                                updatedSale.Code,
                                updatedSale.Sale_Date,
                                updatedSale.Total_Amount,  // ✅ Вычисляется автоматически
                                Employee = employee != null ? new
                                {
                                    employee.Id,
                                    employee.Full_Name
                                } : null,
                                Items = updatedItems,
                                ItemsCount = updatedItems.Count
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception exp)
            {
                return StatusCode(500, new
                {
                    error = "Ошибка при обновлении продажи",
                    message = exp.Message,
                    innerMessage = exp.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Удаляет продажу по идентификатору. Автоматически возвращает все товары из чека на склад.
        /// </summary>
        /// <param name="token">JWT-токен авторизации для проверки прав доступа.</param>
        /// <param name="id">Идентификатор продажи, подлежащей удалению.</param>
        /// <returns>Объект с подтверждением удаления и статистикой.</returns>
        /// <response code="200">Продажа успешно удалена, товары возвращены на склад.</response>
        /// <response code="403">Доступ запрещён: недействительный токен или пользователь не найден.</response>
        /// <response code="404">Продажа с указанным ID не найдена.</response>
        /// <response code="500">Внутренняя ошибка сервера или откат транзакции.</response>
        [Route("/DELETESale")]
        [HttpDelete]
        public ActionResult DeleteSale([FromQuery] string token, int id)
        {
            try
            {
                var curUserId = JwtToken.GetUserIdFromToken(token);
                var currentUser = databaseManager.Employees
                    .FirstOrDefault(x => x.Id == curUserId);

                if (currentUser == null)
                    return StatusCode(403, "Доступ запрещён");

                using (var transaction = databaseManager.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Находим продажу
                        var sale = databaseManager.Sales.Find(id);
                        if (sale == null)
                        {
                            return NotFound(new { message = $"Продажа с ID {id} не найдена" });
                        }

                        // 2. Находим все товары этой продажи
                        var saleItems = databaseManager.Sale_Items
                            .Where(si => si.Sale_id == id)
                            .ToList();

                        // 3. Возвращаем товары на склад
                        foreach (var item in saleItems)
                        {
                            var product = databaseManager.Products.Find(item.Product_id);
                            if (product != null)
                            {
                                product.Stock_Quantity += item.Quantity;
                            }
                        }

                        // 4. Удаляем все связанные товары
                        databaseManager.Sale_Items.RemoveRange(saleItems);

                        // 5. Удаляем саму продажу
                        databaseManager.Sales.Remove(sale);

                        // 6. Сохраняем изменения
                        databaseManager.SaveChanges();
                        transaction.Commit();

                        return Ok(new
                        {
                            message = "Продажа успешно удалена",
                            deletedSaleId = id,
                            returnedItems = saleItems.Count
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception exp)
            {
                return StatusCode(500, new
                {
                    error = "Ошибка при удалении продажи",
                    message = exp.Message,
                    innerMessage = exp.InnerException?.Message
                });
            }
        }
    }
}