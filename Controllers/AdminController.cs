using Microsoft.AspNetCore.Mvc;

namespace Sistema_de_tickets.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult HomeAdmin()
        {
            return View();
        }
    }
}
