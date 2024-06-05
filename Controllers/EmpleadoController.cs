using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_tickets.Models;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace Sistema_de_tickets.Controllers
{
    public class EmpleadoController : Controller
    {
        //Necesario hacer estos contextos antes de empezar a programar 
        private readonly sistemadeticketsDBContext _sistemadeticketsDBContext;

        public EmpleadoController(sistemadeticketsDBContext sistemadeticketsDbContext)
        {
            _sistemadeticketsDBContext = sistemadeticketsDbContext;
        }

        public IActionResult HomeCliente()
        {
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            var tickets = (from t in _sistemadeticketsDBContext.tickets
                           join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                           join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
                           where u.usuario == usuarioSesion.usuario
                           select new
                           {
                               t.id_ticket,
                               t.fecha,
                               u.usuario,
                               t.nombre_ticket,
                               e.nombre_estado
                           }).Take(3).ToList(); //Máximo de filas (tickets) a mostrar en el Home de Cliente

            ViewBag.Tickets = tickets;
            return View();
        }

        public IActionResult HomeEmpleado()
        {
            return View();
        }

        public IActionResult Ajustes()
        {
            return View();
        }

    }
}
