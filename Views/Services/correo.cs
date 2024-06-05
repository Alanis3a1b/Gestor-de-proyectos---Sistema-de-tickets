using Microsoft.Data.SqlClient;

namespace Sistema_de_tickets.Views.Services
{
    public class correo
    {
        private IConfiguration _configuration;
        public correo(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //Esta es la clase encargada de enviar los correos y en la siguiente función el método recibirá el correo destinatario,
        //el asunto y el cuerpo del correo
        public void enviar(string destinatario, string asunto, string cuerpo)
        {
            try 
            { 
                string connectionString = _configuration.GetSection("ConnectionStrings").GetSection("ticketsDbConnection").Value;
                
                string sqlQuery = "exec msdb.dbo.sp_send_dbmail " +
                   "                 @profile_name = 'SQLMail_CATOLICA', " +
                   "                 @recipients = @par_destinatarios, " +
                   "                 @body = @par_asunto, " +
                   "                 @subject = @par_mensaje ";

                //COn esto ya configurado, es de usar la clase y el metodo en el controlador o metodo en donde querramos implementarlo
                using (SqlConnection connection = new SqlConnection(connectionString)) 
                {
                    using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                    {
                        command.Parameters.AddWithValue("@par_destinatarios", destinatario);
                        command.Parameters.AddWithValue("@par_asunto", asunto);
                        command.Parameters.AddWithValue("@par_mensaje", cuerpo);

                        connection.Open();  
                        command.ExecuteNonQuery();  
                    }
                }

            }
            catch (Exception ex) 
            { 
                Console.WriteLine("Error: ");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
