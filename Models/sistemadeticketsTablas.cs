using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
namespace Sistema_de_tickets.Models
{
    public class sistemadeticketsTablas
    {

    }

    public class rol
    {
        [Key]
        public int id_rol { get; set; }
        public string nombre_rol { get; set; }
    }

    public class usuarios
    {
        [Key]
        public int id_usuario { get; set; }
        public string nombre { get; set; }
        public string correo { get; set; }
        public int id_rol { get; set; }
        public string usuario { get; set; }
        public string contrasenya { get; set; }
    }

    public class prioridad
    {
        [Key]
        public int id_prioridad { get; set; }
        public string nombre_prioridad { get; set; }

    }

    public class estados
    {
        [Key]
        public int id_estado { get; set; }
        public string nombre_estado { get; set; }

    }

    public class categorias
    {
        [Key]
        public int id_categoria { get; set; }
        public string nombre_categoria { get; set; }
    }

    public class tickets
    {
        [Key]
        public int id_ticket { get; set; }
        public string nombre_ticket { get; set; }
        public string descripcion { get; set; }
        public string url_archivo { get; set; }
        //Esto me creará las fechas de forma automática
        public DateTime fecha { get; set; } = DateTime.Now;
        public string respuesta { get; set; }
        public int id_prioridad { get; set; }
        public int id_estado { get; set; }
        public int id_usuario { get; set; }
        public int id_categoria { get; set; }

    }

    public class notificaciones
    {
        [Key]
        public int id_notificacion { get; set; }
        public int id_ticket { get; set; }
        public DateTime fecha_envio { get; set; } = DateTime.Now;

    }
}