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
        public IActionResult TicketTrabajado(int id)
        {
            var ticket = (from t in _sistemadeticketsDBContext.tickets
                          join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                          join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
                          join p in _sistemadeticketsDBContext.prioridad on t.id_prioridad equals p.id_prioridad
                          where t.id_ticket == id
                          select new
                          {
                              t.id_ticket,
                              t.fecha,
                              Usuario = u.usuario,
                              t.nombre_ticket,
                              t.descripcion,
                              Estado = e.nombre_estado,
                              AsignadoA = u.nombre,
                              correo_usuario = u.correo,
                              nombre = u.nombre,
                              telefono_usuario = t.telefono_usuario,
                              id_estado = t.id_estado, // Agrega el id_estado a la consulta
                              id_prioridad = t.id_prioridad // Agrega el id_prioridad a la consulta
                          }).FirstOrDefault();


            if (ticket == null)
            {
                return NotFound();
            }

            ViewData["Ticket"] = ticket;

            return View("TrabajarTicketAdmin");
        }




        private bool TicketExists(int id)
        {
            return _sistemadeticketsDBContext.tickets.Any(t => t.id_ticket == id);
        }



        public IActionResult CrearUsuariosAdmin()
        {
            return View();
        }

    }
}
