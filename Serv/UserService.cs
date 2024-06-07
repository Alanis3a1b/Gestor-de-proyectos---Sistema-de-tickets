using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sistema_de_tickets.Models;
using System.Text.Json;

namespace Sistema_de_tickets.Serv
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly sistemadeticketsDBContext _context;

        public UserService(IHttpContextAccessor httpContextAccessor, sistemadeticketsDBContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public async Task<usuarios> GetCurrentUserAsync()
        {
            var userJson = _httpContextAccessor.HttpContext.Session.GetString("user");
            if (string.IsNullOrEmpty(userJson))
            {
                return null;
            }

            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(userJson);
            return await _context.usuarios.FindAsync(usuarioSesion.id_usuario);
        }
    }
}