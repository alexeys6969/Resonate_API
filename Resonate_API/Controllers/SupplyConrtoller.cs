using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resonate_API.Classes;
using Resonate_API.Models;
using Resonate_API.Models.SaleAuxiliaryClasses;
using Resonate_API.Models.SupplyAuxiliaryClasses;

namespace Resonate_API.Controllers
{
    [Route("/supply")]
    [EndpointGroupName("v6")]
    public class SupplyConrtoller : Controller
    {
        private DBManager databaseManager;
        public SupplyConrtoller()
        {
            databaseManager = new DBManager();
        }
        [Route("/GETSupplies")]
        [HttpGet]
        public ActionResult GetSales()
        {
            try
            {
                var salesWithItems = databaseManager.Supplies
                    .GroupJoin(
                    databaseManager.Supply_Items,
                    supply => supply.Id,
                    item => item.Supply_id,
                    (supply, items) => new
                    {
                        Id = supply.Id,
                        Supplier_id = supply.Supplier_id,
                        Supplier_Name = supply.Suppliers.Name,
                        Supply_Date = supply.Supply_Date,
                        Total_Amount = supply.Total_Amount,
                        Items = items.Select(i => new
                        {
                            Id = i.Id,
                            Product_id = i.Product_id,
                            Name = i.Product.Name,
                            Quantity = i.Quantity,
                            Purchase_Price = i.Purchase_Price
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
        [Route("/GETSupplyById")]
        [HttpGet]
        public ActionResult GetSaleById(int id)
        {
            try
            {
                var supplyWithItems = databaseManager.Supplies
                    .GroupJoin(
                    databaseManager.Supply_Items,
                    supply => supply.Id,
                    item => item.Supply_id,
                    (supply, items) => new
                    {
                        Id = supply.Id,
                        Supplier_id = supply.Supplier_id,
                        Supplier_Name = supply.Suppliers.Name,
                        Supply_Date = supply.Supply_Date,
                        Total_Amount = supply.Total_Amount,
                        Items = items.Select(i => new
                        {
                            Id = i.Id,
                            Product_id = i.Product_id,
                            Name = i.Product.Name,
                            Quantity = i.Quantity,
                            Purchase_Price = i.Purchase_Price
                        }).ToList(),
                        ItemsCount = items.Count()
                    })
                    .Where(s => s.Id == id).First();

                return Ok(supplyWithItems);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }
        [Route("/POSTSupply")]
        [HttpPost]
        public ActionResult PostSale([FromBody] CreateSupplyRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        error = "Request body is null",
                        message = "Тело запроса не может быть пустым. Отправьте JSON с supplier_id и items"
                    });
                }
                if (request.supplier_id <= 0)
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
                        message = "Добавьте хотя бы один товар в поставку"
                    });
                }
                var supplier = databaseManager.Suppliers.Find(request.supplier_id);
                if (supplier == null)
                {
                    return BadRequest(new
                    {
                        error = "Supplier not found",
                        message = $"Поставщик с ID {request.supplier_id} не найден"
                    });
                }

                using (var transaction = databaseManager.Database.BeginTransaction())
                {
                    try
                    {
                        var supply = new Supplies
                        {
                            Supplier_id = request.supplier_id,
                            Supply_Date = DateTime.Now,
                            Total_Amount = 0
                        };

                        databaseManager.Supplies.Add(supply);
                        databaseManager.SaveChanges();

                        decimal totalAmount = 0;
                        var supplyItems = new List<Supply_Items>();
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
                            product.Stock_Quantity += item.quantity;
                            var supplyItem = new Supply_Items
                            {
                                Supply_id = supply.Id,
                                Product_id = item.product_id,
                                Quantity = item.quantity,
                                Purchase_Price = product.Price
                            };

                            supplyItems.Add(supplyItem);
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

                        databaseManager.Supply_Items.AddRange(supplyItems);
                        supply.Total_Amount = totalAmount;
                        databaseManager.SaveChanges();
                        transaction.Commit();
                        return Ok(new
                        {
                            supply = new
                            {
                                supply.Id,
                                supplier = new { supply.Suppliers.Id, supply.Suppliers.Name },
                                supply.Supply_Date,
                                
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
        [Route("/PUTSupply")]
        [HttpPut]
        public ActionResult PutSupply(int id, [FromBody] UpdateSupplyFullRequest request)
        {
            try
            {
                using (var transaction = databaseManager.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Находим поставку
                        var supply = databaseManager.Supplies.Find(id);
                        if (supply == null)
                        {
                            return NotFound(new { message = $"Поставка с ID {id} не найдена" });
                        }

                        // 2. Загружаем все товары этой поставки
                        var existingItems = databaseManager.Supply_Items
                            .Where(i => i.Supply_id == id)
                            .ToList();

                        // 3. Обновляем основную информацию о поставке
                        if (request.Supply != null)
                        {
                            if (request.Supply.Supplier_id.HasValue)
                            {
                                var suppliers = databaseManager.Suppliers.Find(request.Supply.Supplier_id.Value);
                                if (suppliers == null)
                                {
                                    transaction.Rollback();
                                    return BadRequest(new { message = $"Поставщик с ID {request.Supply.Supplier_id.Value} не найден" });
                                }
                                supply.Supplier_id = request.Supply.Supplier_id.Value;
                            }

                            if (request.Supply.Supply_Date.HasValue)
                                supply.Supply_Date = request.Supply.Supply_Date.Value;
                        }

                        // 4. Словарь для быстрого доступа к существующим товарам
                        var itemsDict = existingItems.ToDictionary(i => i.Id);

                        // 5. Множество для отслеживания обработанных ID
                        var processedIds = new HashSet<int>();

                        // 6. Список для новых товаров
                        var itemsToAdd = new List<Supply_Items>();

                        // 7. Переменная для пересчета общей суммы
                        decimal totalAmount = 0;

                        // 8. Сначала добавляем в сумму все существующие товары (они останутся неизменными)
                        foreach (var item in existingItems)
                        {
                            totalAmount += item.Quantity * item.Purchase_Price;
                        }

                        // 9. Обрабатываем каждый товар из запроса
                        if (request.Items != null && request.Items.Any())
                        {
                            foreach (var itemRequest in request.Items)
                            {
                                var product = databaseManager.Products.Find(itemRequest.Product_id);
                                if (product == null)
                                {
                                    transaction.Rollback();
                                    return BadRequest(new { message = $"Товар с ID {itemRequest.Product_id} не найден" });
                                }

                                // Определяем действие (если не указано - определяем по Id)
                                string action = itemRequest.Action?.ToLower();
                                if (string.IsNullOrEmpty(action))
                                {
                                    action = itemRequest.Id > 0 ? "update" : "add";
                                }

                                switch (action)
                                {
                                    case "delete":
                                        // УДАЛЕНИЕ товара из поставки
                                        if (itemsDict.TryGetValue(itemRequest.Id, out var itemToDelete))
                                        {
                                            // Уменьшаем количество товара на складе (так как поставка удаляется)
                                            product.Stock_Quantity -= itemToDelete.Quantity;

                                            // Вычитаем из общей суммы
                                            totalAmount -= itemToDelete.Quantity * itemToDelete.Purchase_Price;

                                            // Удаляем из базы
                                            databaseManager.Supply_Items.Remove(itemToDelete);
                                            processedIds.Add(itemRequest.Id);
                                        }
                                        break;

                                    case "update":
                                        // ИЗМЕНЕНИЕ существующего товара в поставке
                                        if (itemsDict.TryGetValue(itemRequest.Id, out var itemToUpdate))
                                        {
                                            // Вычитаем старую сумму из общего итога
                                            totalAmount -= itemToUpdate.Quantity * itemToUpdate.Purchase_Price;

                                            // Возвращаем старое количество на склад
                                            product.Stock_Quantity -= itemToUpdate.Quantity;

                                            // Проверяем, что количество положительное
                                            if (itemRequest.Quantity <= 0)
                                            {
                                                transaction.Rollback();
                                                return BadRequest(new
                                                {
                                                    message = $"Количество товара должно быть положительным",
                                                    product_id = itemRequest.Product_id
                                                });
                                            }

                                            // Добавляем новое количество на склад
                                            product.Stock_Quantity += itemRequest.Quantity;

                                            // Обновляем поля
                                            itemToUpdate.Quantity = itemRequest.Quantity;
                                            if (itemRequest.Purchase_Price.HasValue)
                                            {
                                                itemToUpdate.Purchase_Price = itemRequest.Purchase_Price.Value;
                                            }

                                            // Добавляем новую сумму в общий итог
                                            totalAmount += itemToUpdate.Quantity * itemToUpdate.Purchase_Price;
                                            processedIds.Add(itemRequest.Id);
                                        }
                                        break;

                                    case "add":
                                        // ДОБАВЛЕНИЕ нового товара в поставку
                                        if (itemRequest.Quantity <= 0)
                                        {
                                            transaction.Rollback();
                                            return BadRequest(new
                                            {
                                                message = $"Количество товара должно быть положительным",
                                                product_id = itemRequest.Product_id
                                            });
                                        }

                                        // Добавляем товар на склад
                                        product.Stock_Quantity += itemRequest.Quantity;

                                        // Создаем новый элемент
                                        var newItem = new Supply_Items
                                        {
                                            Supply_id = supply.Id,
                                            Product_id = itemRequest.Product_id,
                                            Quantity = itemRequest.Quantity,
                                            Purchase_Price = itemRequest.Purchase_Price ?? product.Price
                                        };

                                        itemsToAdd.Add(newItem);
                                        totalAmount += newItem.Quantity * newItem.Purchase_Price;
                                        break;

                                    default:
                                        transaction.Rollback();
                                        return BadRequest(new
                                        {
                                            message = $"Неизвестное действие: {itemRequest.Action}",
                                            allowedActions = new[] { "add", "update", "delete" }
                                        });
                                }
                            }
                        }

                        // 10. Добавляем новые товары в базу
                        if (itemsToAdd.Any())
                        {
                            databaseManager.Supply_Items.AddRange(itemsToAdd);
                        }

                        // 11. Обновляем общую сумму поставки
                        supply.Total_Amount = totalAmount;

                        // 12. Сохраняем изменения
                        databaseManager.SaveChanges();
                        transaction.Commit();

                        // 13. Формируем ответ с актуальными данными
                        var updatedItems = databaseManager.Supply_Items
                            .Where(i => i.Supply_id == supply.Id)
                            .Select(i => new
                            {
                                i.Id,
                                i.Product_id,
                                ProductName = databaseManager.Products
                                    .Where(p => p.Id == i.Product_id)
                                    .Select(p => p.Name)
                                    .FirstOrDefault(),
                                i.Quantity,
                                i.Purchase_Price,
                                Subtotal = i.Quantity * i.Purchase_Price
                            })
                            .ToList();

                        var supplier = databaseManager.Suppliers.Find(supply.Supplier_id);

                        return Ok(new
                        {
                            message = "Поставка успешно обновлена",
                            supply = new
                            {
                                supply.Id,
                                Supplier = supplier != null ? new
                                {
                                    supplier.Id,
                                    supplier.Name
                                } : null,
                                supply.Supply_Date,
                                supply.Total_Amount,
                                Items = updatedItems,
                                ItemsCount = updatedItems.Count
                            }
                        });
                    }
                    catch (Exception)
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
                    error = "Ошибка при обновлении поставки",
                    message = exp.Message,
                    innerMessage = exp.InnerException?.Message
                });
            }
        }
        [Route("/DELETESupply")]
        [HttpDelete]
        public ActionResult DeleteSupply(int id)
        {
            try
            {
                using (var transaction = databaseManager.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Находим поставку
                        var supply = databaseManager.Supplies.Find(id);
                        if (supply == null)
                        {
                            return NotFound(new { message = $"Поставка с ID {id} не найдена" });
                        }

                        // 2. Находим все товары этой поставки
                        var supplyItems = databaseManager.Supply_Items
                            .Where(si => si.Supply_id == id)
                            .ToList();

                        // 3. Убираем товары со склада (так как поставка удаляется)
                        foreach (var item in supplyItems)
                        {
                            var product = databaseManager.Products.Find(item.Product_id);
                            if (product != null)
                            {
                                product.Stock_Quantity -= item.Quantity;
                            }
                        }

                        // 4. Удаляем все связанные товары
                        databaseManager.Supply_Items.RemoveRange(supplyItems);

                        // 5. Удаляем саму поставку
                        databaseManager.Supplies.Remove(supply);

                        // 6. Сохраняем изменения
                        databaseManager.SaveChanges();
                        transaction.Commit();

                        return Ok(new
                        {
                            message = "Поставка успешно удалена",
                            deletedSupplyId = id,
                            removedItems = supplyItems.Count,
                            note = "Товары убраны со склада"
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
                    error = "Ошибка при удалении поставки",
                    message = exp.Message,
                    innerMessage = exp.InnerException?.Message
                });
            }
        }
    }
}
