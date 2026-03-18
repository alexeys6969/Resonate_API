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
