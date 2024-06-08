using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_tickets.Models;
using Sistema_de_tickets.Serv;
using Sistema_de_tickets.Views.Services;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sistema_de_tickets.Controllers
{
    public class EmpleadoController : Controller
    {
        private readonly sistemadeticketsDBContext _sistemadeticketsDBContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IUserService _userService;

        public EmpleadoController(sistemadeticketsDBContext sistemadeticketsDbContext, IWebHostEnvironment webHostEnvironment, IUserService userService)
        {
            _sistemadeticketsDBContext = sistemadeticketsDbContext;
            _webHostEnvironment = webHostEnvironment;
            _userService = userService;
        }

        public IActionResult HomeEmpleado()
        {
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            var tickets = (from t in _sistemadeticketsDBContext.tickets
                           join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                           join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
                           where t.id_usuario_asignado == usuarioSesion.id_usuario
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

            var ticketsPorMes = new int[12];

            // Agrupar los tickets por mes y contar la cantidad de tickets en cada grupo
            var resultado = _sistemadeticketsDBContext.tickets
                               .GroupBy(t => t.fecha.Month)
                               .Select(g => new { Mes = g.Key, Cantidad = g.Count() })
                               .ToList();

            // Llenar el array ticketsPorMes con los resultados
            foreach (var item in resultado)
            {
                ticketsPorMes[item.Mes - 1] = item.Cantidad; // Restar 1 para que el índice del array comience en 0
            }

            // Pasar los datos a la vista
            ViewBag.fecha = ticketsPorMes;

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
                                  where t.id_usuario_asignado == usuarioSesion.id_usuario
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
            var ticket = _sistemadeticketsDBContext.tickets.FirstOrDefault(t => t.id_ticket == id);

            if (ticket == null)
            {
                return NotFound();
            }

            var prioridad = _sistemadeticketsDBContext.prioridad.FirstOrDefault(p => p.id_prioridad == ticket.id_prioridad)?.nombre_prioridad;
            var estado = _sistemadeticketsDBContext.estados.FirstOrDefault(e => e.id_estado == ticket.id_estado)?.nombre_estado;

            ViewData["Prioridad"] = prioridad;
            ViewData["Estado"] = estado;

            return View("TrabajarTicketEmpleado", ticket);
        }

        public IActionResult TicketEditado()
        {
            return View();
        }

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


            // Establecer encabezados de control de caché para evitar el almacenamiento en caché
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "-1";

            return File(memory, "application/octet-stream", Path.GetFileName(path));
        }

        [HttpPost]
        public async Task<IActionResult> CambiarFotoPerfil(IFormFile photoUpload)
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            var usuarioF = await _userService.GetCurrentUserAsync();

            if (photoUpload != null && photoUpload.Length > 0)
            {
                if (!string.IsNullOrEmpty(usuarioF.direccion))
                {
                    string existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath, usuarioF.direccion);
                    if (System.IO.File.Exists(existingFilePath))
                    {
                        System.IO.File.Delete(existingFilePath);
                    }
                }

                string newFilePath = await UploadPhoto(photoUpload);
                if (!string.IsNullOrEmpty(newFilePath))
                {
                    usuarioF.Foto = System.IO.File.ReadAllBytes(Path.Combine(_webHostEnvironment.WebRootPath, newFilePath));
                    usuarioF.direccion = newFilePath;
                }

                _sistemadeticketsDBContext.Update(usuarioF);
                await _sistemadeticketsDBContext.SaveChangesAsync();

                // Actualizar el objeto de usuario en la sesión
                var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
                if (newFilePath != null)
                {
                    usuarioSesion.direccion = newFilePath; // Actualizar la ruta en el objeto de sesión
                }
                HttpContext.Session.SetString("user", JsonSerializer.Serialize(usuarioSesion));

                return Json(new { success = true, newImageUrl = "/" + newFilePath.Replace("\\", "/") });
            }

            return Json(new { success = false, message = "No se proporcionó ninguna imagen para actualizar." });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarContrasena(string currentPassword, string newPassword, string confirmNewPassword)
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));

            var usuario = _sistemadeticketsDBContext.usuarios.FirstOrDefault(u => u.contrasenya == currentPassword);

            if (usuario == null)
            {
                ModelState.AddModelError("currentPassword", "La contraseña actual es incorrecta.");
                return View("Settings");
            }

            if (!string.IsNullOrEmpty(newPassword) && newPassword != confirmNewPassword)
            {
                ModelState.AddModelError("confirmNewPassword", "Las contraseñas no coinciden.");
                return View("Settings");
            }

            if (!string.IsNullOrEmpty(newPassword))
            {
                usuario.contrasenya = newPassword;
                _sistemadeticketsDBContext.Update(usuario);
                await _sistemadeticketsDBContext.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Contraseña actualizada correctamente." });
        }





        private async Task<string> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "ProfileImg");

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string filePath = Path.Combine(uploadPath, fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception)
            {
                return null;
            }

            return Path.Combine("ProfileImg", fileName);
        }

        public IActionResult Settings()
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            ViewBag.NombreUsuario = datosUsuario.nombre;
            ViewBag.CorreoUsuario = datosUsuario.correo;

            if (datosUsuario.Foto != null)
            {
                string base64Image = Convert.ToBase64String(datosUsuario.Foto);
                ViewBag.FotoUsuario = $"data:image/png;base64,{base64Image}";
            }
            else if (!string.IsNullOrEmpty(datosUsuario.direccion))
            {
                ViewBag.FotoUsuario = "/" + datosUsuario.direccion.Replace("\\", "/");
            }
            else
            {
                ViewBag.FotoUsuario = null;
            }

            return View();
        }
    }
}
