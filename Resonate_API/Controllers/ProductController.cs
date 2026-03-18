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

        [Route("/GETProducts")]
        [HttpGet]
        public ActionResult GetProducts()
        {
            try
            {
                var products = databaseManager.Products
                    .Select(c => new
                    {
                        Id = c.Id,
                        Aricle = c.Article,
                        Name = c.Name,
                        Description = c.Description,
                        Category = c.Category,
                        Price = c.Price,
                        Stock = c.Stock_Quantity
                    })
                    .ToList();

                return Ok(products);
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

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

        [Route("/POSTProduct")]
        [HttpPost]
        public ActionResult PostProduct([FromForm] string Article, [FromForm] string Name, [FromForm] string Description, [FromForm] int Category_Id, [FromForm] decimal Price, [FromForm] int Stock_Quantity)
        {
            try
            {
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

                var savedProduct = databaseManager.Products
                    .Include(p => p.Category)
                    .FirstOrDefault(p => p.Id == products.Id);

                return CreatedAtAction(nameof(GetProductById),
                new { id = savedProduct.Id },
                new { Id = savedProduct.Id, Article = savedProduct.Article, Name = savedProduct.Name, Description = savedProduct.Description, Category = savedProduct.Category?.Name ?? "Без категории", Price = savedProduct.Price, Stock_Quantity = savedProduct.Stock_Quantity });
            }
            catch (Exception exp)
            {
                return StatusCode(500, exp.Message);
            }
        }

        [Route("/PUTProduct")]
        [HttpPut]
        public ActionResult PutProduct([FromForm] int id, [FromForm] string Article, [FromForm] string Name, [FromForm] string Description, [FromForm] int Category_Id, [FromForm] decimal Price, [FromForm] int Stock_Quantity)
        {
            try
            {
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

        [Route("/DELETEProducts")]
        [HttpDelete]
        public ActionResult DeleteProduc([FromForm] int id)
        {
            try
            {
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
