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

        [Route("/GETSales")]
        [HttpGet]
        public ActionResult GetSales()
        {
            try
            {
                var salesWithItems = databaseManager.Sales
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
                    Total_Amount = sale.Total_Amount,
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
        [Route("/GETSaleById")]
        [HttpGet]
        public ActionResult GetSaleById(int id)
        {
            try
            {
                var saleWithItems = databaseManager.Sales
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
                        Total_Amount = sale.Total_Amount,
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
                    .Where(s => s.Id == id).First();

                return Ok(saleWithItems);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }
        [Route("/POSTSale")]
        [HttpPost]
        public ActionResult PostSale([FromBody] CreateSaleRequest request)
        {
            try
            {
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
                            Sale_Date = DateTime.Now,
                            Total_Amount = 0
                        };

                        databaseManager.Sales.Add(sale);
                        databaseManager.SaveChanges();

                        decimal totalAmount = 0;
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
                            totalAmount += product.Price * item.quantity;

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
                        sale.Total_Amount = totalAmount;
                        databaseManager.SaveChanges();
                        transaction.Commit();
                        return Ok(new
                        {
                            sale = new
                            {
                                sale.Id,
                                sale.Code,
                                sale.Sale_Date,
                                Employee = new { employee.Id, employee.Full_Name },
                                Items = processedItems,
                                Total_Amount = totalAmount,
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

        [Route("/PUTSale")]
        [HttpPut]
        public ActionResult PutSale(int id, [FromBody] UpdateSaleFullRequest request)
        {
            try
            {
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

                        // 2. Загружаем все товары этой продажи
                        var existingItems = databaseManager.Sale_Items
                            .Where(i => i.Sale_id == id)
                            .ToList();

                        // 3. Обновляем основную информацию о продаже
                        if (request.Sale != null)
                        {
                            if (!string.IsNullOrEmpty(request.Sale.Code))
                                sale.Code = request.Sale.Code;

                            if (request.Sale.Employee_id.HasValue)
                            {
                                var Employee = databaseManager.Employees.Find(request.Sale.Employee_id.Value);
                                if (Employee == null)
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
                        var itemsDict = existingItems.ToDictionary(i => i.Id);

                        // 5. Множество для отслеживания обработанных ID
                        var processedIds = new HashSet<int>();

                        // 6. Список для новых товаров
                        var itemsToAdd = new List<Sale_Items>();

                        // 7. Переменная для пересчета общей суммы
                        decimal totalAmount = 0;

                        // 8. Обрабатываем каждый товар из запроса
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

                                            // Удаляем из базы
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

                                            totalAmount += itemToUpdate.Quantity * itemToUpdate.Price_At_Sale;
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
                                        totalAmount += newItem.Quantity * newItem.Price_At_Sale;
                                        break;

                                    default:
                                        // Если action не указан - считаем как добавление/обновление
                                        if (itemRequest.Id > 0 && itemsDict.TryGetValue(itemRequest.Id, out var existingItem))
                                        {
                                            // Это существующий товар - обновляем
                                            goto case "update";
                                        }
                                        else
                                        {
                                            // Это новый товар - добавляем
                                            goto case "add";
                                        }
                                        break;
                                }
                            }
                        }

                        // 9. Добавляем новые товары в базу
                        if (itemsToAdd.Any())
                        {
                            databaseManager.Sale_Items.AddRange(itemsToAdd);
                        }

                        // 10. Обновляем общую сумму продажи
                        sale.Total_Amount = totalAmount;

                        // 11. Сохраняем изменения
                        databaseManager.SaveChanges();
                        transaction.Commit();

                        // 12. Формируем ответ с актуальными данными
                        var updatedItems = databaseManager.Sale_Items
                            .Where(i => i.Sale_id == sale.Id)
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

                        var employee = databaseManager.Employees.Find(sale.Employee_id);

                        return Ok(new
                        {
                            message = "Продажа успешно обновлена",
                            sale = new
                            {
                                sale.Id,
                                sale.Code,
                                sale.Sale_Date,
                                sale.Total_Amount,
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
        [Route("/DELETESale")]
        [HttpDelete]
        public ActionResult DeleteSale(int id)
        {
            try
            {
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

                        // 2. Находим все товары этой продажи через JOIN (по сути WHERE)
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
