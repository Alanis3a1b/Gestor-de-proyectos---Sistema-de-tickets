﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_tickets.Models;
using Sistema_de_tickets.Views.Services;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;


//Controlador para la parte de los clientes
namespace Sistema_de_tickets.Controllers
{
    public class ClienteController : Controller
    {
        //Necesario hacer estos contextos antes de empezar a programar 
        private readonly sistemadeticketsDBContext _sistemadeticketsDBContext;
        private readonly IConfiguration _configuration;
        public ClienteController(sistemadeticketsDBContext sistemadeticketsDbContext, IConfiguration configuration)
        {
            _sistemadeticketsDBContext = sistemadeticketsDbContext;
            _configuration = configuration;
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



        public IActionResult Settings()
        {
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
                              respuesta = t.respuesta
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
        public IActionResult CrearTickets(tickets nuevoTicket)
        {
            // Obtener los datos del usuario de la sesión
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            
            //Extraer los nombres de categoria, estado y prioridad
            var ticket = (from t in _sistemadeticketsDBContext.tickets
                          join c in _sistemadeticketsDBContext.categorias on t.id_categoria equals c.id_categoria
                          join e in _sistemadeticketsDBContext.estados on t.id_estado equals e.id_estado
                          join p in _sistemadeticketsDBContext.prioridad on t.id_prioridad equals p.id_prioridad
                          select new 
                          {
                              categoria = c.nombre_categoria,
                              estado = e.nombre_estado,
                              prioridad = p.nombre_prioridad
                         }).FirstOrDefault();


            nuevoTicket.nombre_usuario = datosUsuario.nombre;
            nuevoTicket.id_usuario = datosUsuario.id_usuario;
            nuevoTicket.id_usuario_asignado = null;
            _sistemadeticketsDBContext.Add(nuevoTicket);
            _sistemadeticketsDBContext.SaveChanges();

            //Enviar correo de confirmacion
            correo enviarCorreo = new correo(_configuration);
            enviarCorreo.enviar(datosUsuario.correo,
                                "Su ticket ha sido creado correctamente!",
                                "Se ha creado el ticket: " + nuevoTicket.id_ticket + "\n"
                                + "Con el nombre de: " + nuevoTicket.nombre_ticket + "\n"
                                + "La categoría de su ticket es de: " + ticket.categoria + "\n" 
                                + "Estado del ticket: " + ticket.estado + "\n"
                                + "Y la prioridad de su ticket es de: " + ticket.prioridad);

            return RedirectToAction("Success");
        }


        public IActionResult Success()
        {
            return View();
        }


    }
}
