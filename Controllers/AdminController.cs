using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_tickets.Models;
using Sistema_de_tickets.Views.Services;
using System.Diagnostics;
using System.Linq;

namespace Sistema_de_tickets.Controllers
{
    public class AdminController : Controller
    {
        private readonly sistemadeticketsDBContext _sistemadeticketsDBContext;
        private readonly IConfiguration _configuration;

        public AdminController(sistemadeticketsDBContext sistemadeticketsDBContext, IConfiguration configuration)
        {
            _sistemadeticketsDBContext = sistemadeticketsDBContext;
            _configuration = configuration;
        }

        public IActionResult HomeAdmin()
        {
            return View();
        }

        public IActionResult TodosLosTickets(string estado = "Todos")
        {
            var todoslosTickets = from t in _sistemadeticketsDBContext.tickets
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

            if (estado != "Todos")
            {
                todoslosTickets = todoslosTickets.Where(t => t.Estado == estado);
            }

            var estadosList = _sistemadeticketsDBContext.estados.ToList();
            var selectOptions = estadosList
                .Select(e => $"<option value=\"{e.nombre_estado}\" {(estado == e.nombre_estado ? "selected" : "")}>{e.nombre_estado}</option>")
                .ToList();

            selectOptions.Insert(0, $"<option value=\"Todos\" {(estado == "Todos" ? "selected" : "")}>Todos</option>");
            ViewData["SelectOptions"] = string.Join("\n", selectOptions);

            ViewData["TodosLosTickets"] = todoslosTickets.ToList();
            ViewData["SelectedEstado"] = estado;

            return View();
        }



        /*.
        [HttpGet]
        public IActionResult TrabajarTicketAdmin(int id)
        {
            var ticket = _sistemadeticketsDBContext.tickets
                .Include(t => t.id_estado)
                .FirstOrDefault(t => t.id_ticket == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var estados = _sistemadeticketsDBContext.estados.ToList();
            ViewData["Estados"] = estados;

            return View(ticket);
        }*/

        //Al dar clic en un ticket, abre la vista para trabajar este ticket.
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
                              id_estado = t.id_estado,
                              id_prioridad = t.id_prioridad,
                              respuesta = t.respuesta
                          }).FirstOrDefault();

            if (ticket == null)
            {
                return NotFound();
            }

            ViewData["Ticket"] = ticket;
            return View("TrabajarTicketAdmin");
        }

        [HttpPost]
        public IActionResult GuardarCambios(int id_ticket, string respuesta, int id_estado)
        {
            var ticketActual = _sistemadeticketsDBContext.tickets.FirstOrDefault(t => t.id_ticket == id_ticket);

            if (ticketActual == null)
            {
                return NotFound();
            }

            ticketActual.respuesta = respuesta;
            ticketActual.id_prioridad = id_estado;

            _sistemadeticketsDBContext.SaveChanges();

            return RedirectToAction("TicketEditado");
        }




        private bool TicketExists(int id)
        {
            return _sistemadeticketsDBContext.tickets.Any(t => t.id_ticket == id);
        }

        public IActionResult CrearUsuariosAdmin()
        {
            var listaDeRoles = (from m in _sistemadeticketsDBContext.rol
                                select m).ToList();
            ViewData["listadoDeRoles"] = new SelectList(listaDeRoles, "id_rol", "nombre_rol");

            return View();
        }

        [HttpPost]
        public IActionResult CrearUsuariosAdmin(usuarios usuarioNuevo)
        {
            correo enviarCorreo = new correo(_configuration);
            _sistemadeticketsDBContext.Add(usuarioNuevo);
            _sistemadeticketsDBContext.SaveChanges();
            enviarCorreo.enviar(usuarioNuevo.correo,
                                "Bienvenido al sistema de tickets",
                                "Su usuario ha sido creado exitosamente.");

            return RedirectToAction("HomeAdmin");
        }

        public IActionResult EditarUsuarioAdmin(int id)
        {
            var usuario = _sistemadeticketsDBContext.usuarios.Find(id);
            if (usuario == null)
            {
                return NotFound();
            }

            var listaDeRoles = (from m in _sistemadeticketsDBContext.rol
                                select m).ToList();
            ViewData["listadoDeRoles"] = new SelectList(listaDeRoles, "id_rol", "nombre_rol");

            return View(usuario);
        }

        [HttpPost]
        public IActionResult EditarUsuarioAdmin(usuarios usuarioActualizado)
        {
            if (!ModelState.IsValid)
            {
                var listaDeRoles = (from m in _sistemadeticketsDBContext.rol
                                    select m).ToList();
                ViewData["listadoDeRoles"] = new SelectList(listaDeRoles, "id_rol", "nombre_rol");

                return View(usuarioActualizado);
            }

            _sistemadeticketsDBContext.Update(usuarioActualizado);
            _sistemadeticketsDBContext.SaveChanges();

            return RedirectToAction("HomeAdmin");
        }

        public IActionResult EliminarUsuarioAdmin(int id)
        {
            var usuario = _sistemadeticketsDBContext.usuarios.Find(id);
            if (usuario == null)
            {
                return NotFound();
            }

            _sistemadeticketsDBContext.usuarios.Remove(usuario);
            _sistemadeticketsDBContext.SaveChanges();

            return RedirectToAction("HomeAdmin");
        }

        public IActionResult TicketEditado()
        {
            return View();
        }
    }
}
