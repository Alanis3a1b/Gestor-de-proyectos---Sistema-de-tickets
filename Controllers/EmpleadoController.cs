using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_tickets.Models;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json;

namespace Sistema_de_tickets.Controllers
{
    public class EmpleadoController : Controller
    {
        //Necesario hacer estos contextos antes de empezar a programar 
        private readonly sistemadeticketsDBContext _sistemadeticketsDBContext;
        //Descargar archivo
        private readonly IWebHostEnvironment _webHostEnvironment;
        public EmpleadoController(sistemadeticketsDBContext sistemadeticketsDbContext, IWebHostEnvironment webHostEnvironment)
        {
            _sistemadeticketsDBContext = sistemadeticketsDbContext;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult HomeEmpleado()
        {
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            var tickets = (from t in _sistemadeticketsDBContext.tickets
                           join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                           join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
                           where t.id_usuario_asignado == usuarioSesion.id_usuario // Filtrar por tickets asignados al empleado
                           where t.id_estado == 1 || t.id_estado == 2 || t.id_estado == 4
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



        public IActionResult Ajustes()
        {
            return View();
        }

        public IActionResult TicketsEmpleado(string estado = "Todos")
        {
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            var ticketsEmpleado = from t in _sistemadeticketsDBContext.tickets
                                  join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                                  join ua in _sistemadeticketsDBContext.usuarios on t.id_usuario_asignado equals ua.id_usuario
                                  join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
                                  where t.id_usuario_asignado == usuarioSesion.id_usuario // Filtrar por tickets asignados al empleado que inicio sesión
                                  select new
                                  {
                                      t.id_ticket,
                                      t.fecha,
                                      Usuario = u.usuario,
                                      t.nombre_ticket,
                                      Estado = e.nombre_estado,
                                      AsignadoA = ua.nombre
                                  };

            if (estado != "Todos")
            {
                ticketsEmpleado = ticketsEmpleado.Where(t => t.Estado == estado);
            }

            var estadosList = _sistemadeticketsDBContext.estados.ToList();
            var selectOptions = estadosList
                .Select(e => $"<option value=\"{e.nombre_estado}\" {(estado == e.nombre_estado ? "selected" : "")}>{e.nombre_estado}</option>")
                .ToList();

            selectOptions.Insert(0, $"<option value=\"Todos\" {(estado == "Todos" ? "selected" : "")}>Todos</option>");
            ViewData["SelectOptions"] = string.Join("\n", selectOptions);

            ViewData["TicketsEmpleado"] = ticketsEmpleado.ToList();
            ViewData["SelectedEstado"] = estado;

            return View();
        }

        [HttpPost]
        public IActionResult GuardarCambios(int id_ticket, string respuesta, int id_estado)
        {
            var ticket = _sistemadeticketsDBContext.tickets.Find(id_ticket);
            if (ticket == null)
            {
                return NotFound();
            }

            ticket.respuesta = respuesta;
            ticket.id_estado = id_estado;
  
            _sistemadeticketsDBContext.SaveChanges();

            return RedirectToAction("TicketEditado");
        }

        public IActionResult TicketTrabajado(int id)
        {
            var ticket = _sistemadeticketsDBContext.tickets
                .FirstOrDefault(t => t.id_ticket == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var prioridad = _sistemadeticketsDBContext.prioridad
                .FirstOrDefault(p => p.id_prioridad == ticket.id_prioridad)?.nombre_prioridad;

            var estado = _sistemadeticketsDBContext.estados
                .FirstOrDefault(e => e.id_estado == ticket.id_estado)?.nombre_estado;

            ViewData["Prioridad"] = prioridad;
            ViewData["Estado"] = estado;

            return View("TrabajarTicketEmpleado", ticket);
        }



        public IActionResult TicketEditado()
        {
            return View();
        }


        //Descargar el archivo subido por el cliente
        public IActionResult DescargarArchivo(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return NotFound();
            }

            var path = Path.Combine(_webHostEnvironment.WebRootPath, filePath.TrimStart('/'));

            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                stream.CopyTo(memory);
            }
            memory.Position = 0;

            return File(memory, "application/octet-stream", Path.GetFileName(path));
        }

        [HttpPost]
        public IActionResult CambiarContrasena(string currentPassword, string newPassword, string confirmNewPassword)
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));

            // Buscar el usuario por su contraseña actual
            var usuario = _sistemadeticketsDBContext.usuarios.FirstOrDefault(u => u.contrasenya == currentPassword);

            // Validar que el usuario existe y que la nueva contraseña y confirmación coincidan
            if (usuario == null)
            {
                ModelState.AddModelError("currentPassword", "La contraseña actual es incorrecta.");
                return View("Settings");
            }

            if (newPassword != confirmNewPassword)
            {
                ModelState.AddModelError("confirmNewPassword", "Las contraseñas no coinciden.");
                return View("Settings");
            }

            // Actualizar la contraseña en la base de datos
            usuario.contrasenya = newPassword;
            _sistemadeticketsDBContext.Update(usuario);
            _sistemadeticketsDBContext.SaveChanges();

            // Establecer un mensaje de éxito
            TempData["Message"] = "Contraseña reestablecida correctamente.";

            // Redirigir a la página de Settings
            return RedirectToAction("Settings");
        }

        public IActionResult Settings()
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            ViewBag.NombreUsuario = datosUsuario.nombre;
            ViewBag.CorreoUsuario = datosUsuario.correo;

            return View();
        }

    }
}