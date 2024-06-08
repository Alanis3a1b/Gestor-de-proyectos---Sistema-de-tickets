using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_tickets.Models;
using Sistema_de_tickets.Views.Services;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;
using Sistema_de_tickets.Serv;

namespace Sistema_de_tickets.Controllers
{
    public class AdminController : Controller
    {
        private readonly sistemadeticketsDBContext _sistemadeticketsDBContext;
        private readonly IConfiguration _configuration;
        //Descargar archivo
        private readonly IWebHostEnvironment _webHostEnvironment;

        //Subir fotos:
        private readonly IUserService _userService;

        public AdminController(sistemadeticketsDBContext context, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, IUserService userService)
        {
            _sistemadeticketsDBContext = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _userService = userService;
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
                              respuesta = t.respuesta,
                              url_archivo = t.url_archivo
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

            //Extraer el correo del creador del ticket
            var ticket = (from t in _sistemadeticketsDBContext.tickets
                          join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                          join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
                          where t.id_ticket == id_ticket && t.id_usuario == u.id_usuario
                          select new
                          {
                              correo = u.correo,

                          }).FirstOrDefault();
            //Extraer el nombre del nuevo estado
            var estado = (from e in _sistemadeticketsDBContext.estados
                          where e.id_estado == id_estado
                          select new
                          {
                              estado = e.nombre_estado

                          }).FirstOrDefault();

            //Enviar correo de actualización
            correo enviarCorreo = new correo(_configuration);
            enviarCorreo.enviar(ticket.correo,
                                "Actualización de su ticket: " + ticketActual.id_ticket,
                                "Su ticket de nombre: ''" + ticketActual.nombre_ticket + "'' ha sido actualizado" + "\n" + "\n"
                                + " Estado actual de su ticket: " + estado.estado + "\n"
                                + " Respuesta de su ticket: " + respuesta);

            //Enviar correo al usuario al quien se le asigno el ticket
            var dameelcorreo = (from u in _sistemadeticketsDBContext.usuarios
                                where id_usuario_asignado == u.id_usuario
                                select new
                                {
                                    correousuario = u.correo

                                }).FirstOrDefault();
            enviarCorreo.enviar(dameelcorreo.correousuario,
                                "Se le ha asignado el ticket: #" + id_ticket,
                                "El administrador le ha asignado la resolución del ticket de nombre: " + ticketActual.nombre_ticket);



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
                                       m.direccion,
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
                                "Bienvenido a HELPHUB " + "\n"
                                + "Se le ha asignado una nueva cuenta cuyo nombre de cuenta es: " + usuarioNuevo.usuario + "\n"
                                + "Y su contraseña es:  " + usuarioNuevo.contrasenya + "\n"
                                + "La contraseña la puede cambiar ingresando a su cuenta e ingresando a la plataforma y en Ajustes puede configurar una nueva");

            return RedirectToAction("Success");
        }

        public IActionResult TicketEditado()
        {
            return View();
        }

        public IActionResult Success()
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

            // Establecer encabezados de control de caché para evitar el almacenamiento en caché
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "-1";

            return File(memory, "application/octet-stream", Path.GetFileName(path));
        }

        //Restaura contraseña
        [HttpPost]
        public async Task<IActionResult> CambiarContrasena(string currentPassword, string newPassword, string confirmNewPassword, IFormFile photoUpload)
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));

            var usuario = _sistemadeticketsDBContext.usuarios.FirstOrDefault(u => u.contrasenya == currentPassword);

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

            usuario.contrasenya = newPassword;
            _sistemadeticketsDBContext.Update(usuario);
            await _sistemadeticketsDBContext.SaveChangesAsync();

            var usuarioF = await _userService.GetCurrentUserAsync();
            string newFilePath = null;

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

                newFilePath = await UploadPhoto(photoUpload);
                if (!string.IsNullOrEmpty(newFilePath))
                {
                    usuarioF.Foto = System.IO.File.ReadAllBytes(Path.Combine(_webHostEnvironment.WebRootPath, newFilePath));
                    usuarioF.direccion = newFilePath;
                }
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