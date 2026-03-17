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
