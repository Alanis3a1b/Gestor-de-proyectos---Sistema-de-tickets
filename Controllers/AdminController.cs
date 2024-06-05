using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_tickets.Models;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;

namespace Sistema_de_tickets.Controllers
{
    public class AdminController : Controller
    {
        //Necesario hacer estos contextos antes de empezar a programar 
        private readonly sistemadeticketsDBContext _sistemadeticketsDBContext;

        public AdminController(sistemadeticketsDBContext sistemadeticketsDbContext)
        {
            _sistemadeticketsDBContext = sistemadeticketsDbContext;
        }
        public IActionResult HomeAdmin()
        {
            return View();
        }

        public IActionResult TodosLosTickets()
        {
            var todoslostickets = from t in _sistemadeticketsDBContext.tickets
                                   join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                                   join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
                                   select new
                                   {
                                       t.id_ticket,
                                       t.fecha,
                                       Usuario = u.usuario,
                                       t.nombre_ticket,
                                       Estado = e.nombre_estado,
                                       AsignadoA = u.nombre
                                   };

            ViewData["TodosLosTickets"] = todoslostickets.ToList();

            return View();
        }

        //Trabajar tickets (aun no funciona...)
        public IActionResult TrabajarTicketAdmin(int id, [FromForm] tickets ticketModificar)
        {
            // seleccionar el ticket
            tickets? ticketActual = (from e in _sistemadeticketsDBContext.tickets
                                where e.id_ticket == id
                                select e).FirstOrDefault();

            ViewData["Ticket"] = ticketActual;

            if (ticketActual == null)
            { return NotFound(); }

            //Datos a modificar para trabajar los tickets
            //ticketActual.nombre_ticket = ticketModificar.nombre_ticket;
            //ticketActual.fecha = ticketModificar.fecha;
            //ticketActual.respuesta = ticketModificar.respuesta;
            //ticketActual.id_prioridad = ticketModificar.id_prioridad;
            //ticketActual.id_estado = ticketModificar.id_estado;
            //ticketActual.id_categoria = ticketModificar.id_categoria;

            _sistemadeticketsDBContext.Entry(ticketActual).State = EntityState.Modified;
            _sistemadeticketsDBContext.SaveChanges();

            //return Ok(ticketModificar);
            return View();
            //return RedirectToAction("TodosLosTickets");

        }

        public IActionResult CrearUsuariosAdmin()
        {
            return View();
        }

    }
}
