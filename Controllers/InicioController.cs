using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_tickets.Models;
using System.Text.Json;

namespace Sistema_de_tickets.Controllers
{
    public class InicioController : Controller
    {

        //Necesario hacer estos contextos antes de empezar a programar 
        private readonly sistemadeticketsDBContext _sistemadeticketsDBContext;

        public InicioController(sistemadeticketsDBContext sistemadeticketsDbContext)
        {
            _sistemadeticketsDBContext = sistemadeticketsDbContext;
        }
        public IActionResult Index()
        {
            var listaDeRoles = (from m in _sistemadeticketsDBContext.rol
                                select m).ToList();
            ViewData["listadoDeRoles"] = new SelectList(listaDeRoles, "id_rol", "nombre_rol");

            //Aqui recuperamos los datos de la variable de session hacia el objeto "datosUsuario"
            if (HttpContext.Session.GetString("user") != null)
            {
                var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
                ViewBag.NombreUsuario = datosUsuario.usuario;
            }
            return View();
        }

        //Creo que aquí se debería poner el IF dependiendo que qué tipo de usuario se está validando
        public IActionResult ValidarUsuario(credenciales credenciales)
        {
           
            usuarios? usuario = (from user in _sistemadeticketsDBContext.usuarios
                                 where user.usuario == credenciales.usuario
                                 && user.contrasenya == credenciales.contrasenya
                                 select user).FirstOrDefault();

            //Si las credenciales no son correctas, saltara el mensaje de error
            if (usuario == null)
            {
                ViewBag.Mensaje = "Credenciales incorrectas.";
                return View("Index");
            }

            //Si no da error, saltara a estas lineas de codigo
            string datoUsuario = JsonSerializer.Serialize(usuario);
            HttpContext.Session.SetString("user", datoUsuario);

            //Comprobación de tipo de usuario al autenticar...
            if (usuario.id_rol == 1)
            {
                HttpContext.Session.SetString("user", datoUsuario);
                return RedirectToAction("HomeCliente", "Cliente");
            }
            if (usuario.id_rol == 2)
            {
                HttpContext.Session.SetString("user", datoUsuario);
                return RedirectToAction("HomeAdmin", "Admin");
            }
            else
            {
                //Para los empleados (cuando tengan su propia vista...)
                HttpContext.Session.SetString("user", datoUsuario);
                return RedirectToAction("HomeEmpleado", "Empleado");
            }

            //Me redirige al Index de la carpeta Home, asi se hace para redirigir a otras vistas de diferentes carpetas
            //return RedirectToAction("Index", "Home");
        }
    }
}
