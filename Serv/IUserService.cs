using Sistema_de_tickets.Models;

namespace Sistema_de_tickets.Serv
{
    public interface IUserService
    {
        Task<usuarios> GetCurrentUserAsync();
    }
}