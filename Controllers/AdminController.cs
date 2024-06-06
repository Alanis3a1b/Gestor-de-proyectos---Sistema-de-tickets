using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_tickets.Models;
using Sistema_de_tickets.Views.Services;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;

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
            var tickets = (from t in _sistemadeticketsDBContext.tickets
                           join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                           join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado           
                           select new
                           {
                               t.id_ticket,
                               t.fecha,
                               t.nombre_usuario,
                               t.nombre_ticket,
                               e.nombre_estado
                           }).ToList();
            ViewBag.Tickets = tickets;
            return View();
        }

        public IActionResult TodosLosTickets(string estado = "Todos")
        {
            var todoslosTickets = from t in _sistemadeticketsDBContext.tickets
                                  join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                                  join ua in _sistemadeticketsDBContext.usuarios on t.id_usuario_asignado equals ua.id_usuario into uag
                                  from ua in uag.DefaultIfEmpty()
                                  join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
                                  select new
                                  {
                                      t.id_ticket,
                                      t.fecha,
                                      Usuario = u.usuario,
                                      t.nombre_ticket,
                                      Estado = e.nombre_estado,
                                      AsignadoA = ua != null ? ua.nombre : "Sin asignar"
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

            // Filtra usuarios con id_rol mayor a 1 para excluir clientes
            var usuarios = _sistemadeticketsDBContext.usuarios
                            .Where(u => u.id_rol > 1)
                            .ToList();

            ViewBag.Usuarios = usuarios;

            ViewData["Ticket"] = ticket;
            return View("TrabajarTicketAdmin");
        }

        [HttpPost]
        public IActionResult GuardarCambios(int id_ticket, string respuesta, int id_estado, int id_prioridad, int id_usuario_asignado)
        {
            var ticketActual = _sistemadeticketsDBContext.tickets.FirstOrDefault(t => t.id_ticket == id_ticket);

            if (ticketActual == null)
            {
                return NotFound();
            }

            //Guardar los datos del ticket
            ticketActual.respuesta = respuesta;
            ticketActual.id_estado = id_estado;
            ticketActual.id_prioridad = id_prioridad;
            ticketActual.id_usuario_asignado = id_usuario_asignado;

            _sistemadeticketsDBContext.SaveChanges();

            //Extraer los datos del usuario del ticket
            var ticket = (from t in _sistemadeticketsDBContext.tickets
                          join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                          join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
                          select new
                          {
                              correo = u.correo,
                              estado = e.nombre_estado,

                          }).FirstOrDefault();

            //Enviar correo de actualización
            correo enviarCorreo = new correo(_configuration);
            enviarCorreo.enviar(ticket.correo,
                                "Actualización de su ticket: " + ticketActual.id_ticket,
                                "Su ticket de nombre: ''" + ticketActual.nombre_ticket + "'' ha sido actualizado" + "\n" + "\n"
                                + " Respuesta de su ticket: " + respuesta);

            return RedirectToAction("TicketEditado");
        }


        private bool TicketExists(int id)
        {
            return _sistemadeticketsDBContext.tickets.Any(t => t.id_ticket == id);
        }

        public IActionResult CrearUsuariosAdmin()
        {
            //Lista de los roles
            var listaDeRoles = (from m in _sistemadeticketsDBContext.rol
                                select m).ToList();
            ViewData["listadoDeRoles"] = new SelectList(listaDeRoles, "id_rol", "nombre_rol");

            //Lista de usuarios join con rol
            var listaDeUsuarios = (from m in _sistemadeticketsDBContext.usuarios
                                   join r in _sistemadeticketsDBContext.rol on m.id_rol equals r.id_rol
                                   select new
                                   {
                                       m.id_usuario,
                                       m.nombre,
                                       m.correo,
                                       rol = r.nombre_rol,
                                       m.nombre_empresa,
                                       m.usuario,
                                       m.contrasenya
                                   }).ToList();
            ViewBag.usuarios = listaDeUsuarios;

            return View();
        }

        //Crear usuarios
        public IActionResult CrearUsuariossAdmin(usuarios usuarioNuevo)
        {
            correo enviarCorreo = new correo(_configuration);
            _sistemadeticketsDBContext.Add(usuarioNuevo);
            _sistemadeticketsDBContext.SaveChanges();
            enviarCorreo.enviar(usuarioNuevo.correo,
                                "Cuenta para acceder a HELPHUB",
                                "Se le ha asignado una nueva cuenta cuyo nombre de cuenta es: " + usuarioNuevo.usuario + "\n"
                                + " Y su contraseña es:  " + usuarioNuevo.contrasenya + "\n"
                                + " La contraseña la puede cambiar ingresando a su cuenta e ingresando luego a su perfil.");

            return RedirectToAction("CrearUsuariosAdmin");
        }


        //Eliminar usuario
        public IActionResult EliminarUsuarioAdmin(int id)
        {
            var usuario = _sistemadeticketsDBContext.usuarios.Find(id);
            if (usuario == null)
            {
                return NotFound();
            }

            _sistemadeticketsDBContext.usuarios.Remove(usuario);
            _sistemadeticketsDBContext.SaveChanges();

            return RedirectToAction("CrearUsuariosAdmin");

        }

        public IActionResult TicketEditado()
        {
            return View();
        }


    }
}
