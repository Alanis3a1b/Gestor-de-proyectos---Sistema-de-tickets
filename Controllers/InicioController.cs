using Microsoft.AspNetCore.Mvc;

namespace Sistema_de_tickets.Controllers
{
    public class InicioController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
