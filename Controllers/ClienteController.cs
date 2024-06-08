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


//Controlador para la parte de los clientes
namespace Sistema_de_tickets.Controllers
{
    public class ClienteController : Controller
    {
        //Necesario hacer estos contextos antes de empezar a programar 
        private readonly sistemadeticketsDBContext _sistemadeticketsDBContext;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IUserService _userService;
        //Descargar archivo
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ClienteController(sistemadeticketsDBContext sistemadeticketsDbContext, IConfiguration configuration, IWebHostEnvironment hostingEnvironment,
            IWebHostEnvironment webHostEnvironment, IUserService userService)
        {
            _sistemadeticketsDBContext = sistemadeticketsDbContext;
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
            _webHostEnvironment = webHostEnvironment;
            _userService = userService;
        }

        public IActionResult CrearTicket()
        {
            // Aquí listaremos el listado de los clientes
            var listaDeUsuarios = (from m in _sistemadeticketsDBContext.usuarios
                                   select m).ToList();
            ViewData["listadoDeUsuarios"] = new SelectList(listaDeUsuarios, "id_usuario", "correo");

            // Listado de categorías
            var listaDeCategorias = (from e in _sistemadeticketsDBContext.categorias
                                     select e).ToList();
            ViewData["listadoDeCategorias"] = new SelectList(listaDeCategorias, "id_categoria", "nombre_categoria");

            // Listado de categorías
            var listaDePrioridad = (from e in _sistemadeticketsDBContext.prioridad
                                    select e).ToList();
            ViewData["listadoDePrioridad"] = new SelectList(listaDePrioridad, "id_prioridad", "nombre_prioridad");

            // Obtener los datos del usuario de la sesión
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            ViewBag.NombreUsuario = datosUsuario.nombre;

            return View();
        }

        public IActionResult HistorialDeTickets()
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));

            var historialTickets = (from t in _sistemadeticketsDBContext.tickets
                                    join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                                    join ua in _sistemadeticketsDBContext.usuarios on t.id_usuario_asignado equals ua.id_usuario into uag
                                    from ua in uag.DefaultIfEmpty()
                                    join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
                                    where u.usuario == datosUsuario.usuario
                                    select new
                                    {
                                        t.id_ticket,
                                        t.fecha,
                                        Usuario = u.usuario,
                                        t.nombre_ticket,
                                        Estado = e.nombre_estado,
                                        AsignadoA = ua != null ? ua.nombre : "Sin asignar"
                                    }).ToList();

            ViewData["HistorialTickets"] = historialTickets;

            return View();
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

        public IActionResult TicketTrabajado(int id)
        {
            var ticket = (from t in _sistemadeticketsDBContext.tickets
                          join u in _sistemadeticketsDBContext.usuarios on t.id_usuario equals u.id_usuario
                          join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
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
                              respuesta = t.respuesta,
                              url_archivo = t.url_archivo
                          }).FirstOrDefault();

            if (ticket == null)
            {
                return NotFound();
            }

            ViewData["Ticket"] = ticket;

            return View();
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile()
        {
            var files = HttpContext.Request.Form.Files;
            if (files == null || files.Count == 0)
                return BadRequest("No file uploaded.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePaths = new List<string>();
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    filePaths.Add(fileName);
                }
            }

            return Ok(filePaths);
        }

        //Función para guardar crear los tickets
        //(usuarios nuevoUsuario) ---> ("nombre tabla a meter datos" "variable")
        public IActionResult CrearTickets(tickets nuevoTicket, IFormFile archivoAdjunto)
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));

            // Validaciones
            if (string.IsNullOrWhiteSpace(nuevoTicket.telefono_usuario) || nuevoTicket.telefono_usuario.Length != 8)
            {
                TempData["Error"] = "El número de teléfono debe tener 8 dígitos.";
                return RedirectToAction("CrearTicket");
            }

            if (string.IsNullOrWhiteSpace(nuevoTicket.nombre_ticket) || nuevoTicket.nombre_ticket.Length <= 7)
            {
                TempData["Error"] = "El título del ticket es muy corto.";
                return RedirectToAction("CrearTicket");
            }

            if (string.IsNullOrWhiteSpace(nuevoTicket.descripcion) || nuevoTicket.descripcion.Length <= 10)
            {
                TempData["Error"] = "La descripción del ticket es muy corta";
                return RedirectToAction("CrearTicket");
            }

            if (nuevoTicket.id_categoria == 0)
            {
                TempData["Error"] = "Debe seleccionar una categoría.";
                return RedirectToAction("CrearTicket");
            }

            if (nuevoTicket.id_prioridad == 0)
            {
                TempData["Error"] = "Debe seleccionar una prioridad.";
                return RedirectToAction("CrearTicket");
            }

            var ticketInfo = (from c in _sistemadeticketsDBContext.categorias
                              join e in _sistemadeticketsDBContext.estados on nuevoTicket.id_estado equals e.id_estado
                              join p in _sistemadeticketsDBContext.prioridad on nuevoTicket.id_prioridad equals p.id_prioridad
                              where nuevoTicket.id_categoria == c.id_categoria
                              select new
                              {
                                  categoria = c.nombre_categoria,
                                  estado = e.nombre_estado,
                                  prioridad = p.nombre_prioridad
                              }).FirstOrDefault();

            if (archivoAdjunto != null && archivoAdjunto.Length > 0)
            {
                var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivoAdjunto.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    archivoAdjunto.CopyTo(fileStream);
                }

                nuevoTicket.url_archivo = "/uploads/" + fileName;
            }

            nuevoTicket.nombre_usuario = datosUsuario.nombre;
            nuevoTicket.id_usuario = datosUsuario.id_usuario;
            nuevoTicket.id_usuario_asignado = null;

            _sistemadeticketsDBContext.Add(nuevoTicket);
            _sistemadeticketsDBContext.SaveChanges();

            //Enviar correo de confirmación al usuario que lo crea
            correo enviarCorreo = new correo(_configuration);
            enviarCorreo.enviar(datosUsuario.correo,
                                "Su ticket ha sido creado correctamente!",
                                "Se ha creado el ticket: " + nuevoTicket.id_ticket + "\n"
                                + "Con el nombre de: " + nuevoTicket.nombre_ticket + "\n"
                                + "La categoría de su ticket es de: " + ticketInfo.categoria + "\n"
                                + "Estado del ticket: " + ticketInfo.estado + "\n"
                                + "Y la prioridad de su ticket es de: " + ticketInfo.prioridad);

            //Enviar correo de ticket creado a los admin
            var admins = (from u in _sistemadeticketsDBContext.usuarios
                          join e in _sistemadeticketsDBContext.rol on u.id_rol equals e.id_rol
                          where u.id_rol == 2
                          select new
                          {
                              correo = u.correo,
                          });

            foreach (var admin in admins)
            {
                enviarCorreo.enviar(admin.correo,
                                    "SE HA CREADO UN NUEVO TICKET",
                                    "Se ha creado el ticket: " + nuevoTicket.id_ticket + "\n"
                                    + "Con el nombre de: " + nuevoTicket.nombre_ticket + "\n"
                                    + "La categoría de su ticket es de: " + ticketInfo.categoria + "\n"
                                    + "Estado del ticket: " + ticketInfo.estado + "\n"
                                    + "Y la prioridad de su ticket es de: " + ticketInfo.prioridad);
            }

            return RedirectToAction("Success");
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


        /*public IActionResult Settings()
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
        }*/

        public IActionResult AjustesCliente()
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



    }
}
