using Microsoft.EntityFrameworkCore;
namespace Sistema_de_tickets.Models
{
    public class sistemadeticketsDBContext : DbContext
    {
        public sistemadeticketsDBContext(DbContextOptions options) : base(options)
        {

        }

        //Aquí poner los contextos de las tablas a utilizar:
        //Se pueden borrar las que no se ocuparan pero por si acaso las dejo todas.
        public DbSet<rol> rol { get; set; }

        //Referencia del modelado de la tabla para la autenticación de los usuarios
        public DbSet<usuarios> usuarios { get; set; }
        
        public DbSet<prioridad> prioridad { get; set; }
        public DbSet<estados> estados { get; set; }
        public DbSet<categorias> categorias { get; set; }
        public DbSet<tickets> tickets { get; set; }
        public DbSet<notificaciones> notificaciones { get; set; }


    }
}
