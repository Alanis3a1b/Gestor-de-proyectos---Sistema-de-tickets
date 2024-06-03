using Microsoft.EntityFrameworkCore;
using Sistema_de_tickets.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//Inyeccion del contexto para la base de datos
builder.Services.AddDbContext<sistemadeticketsDBContext>(opt =>
        opt.UseSqlServer(
            builder.Configuration.GetConnectionString("ticketsDbConnection")
            )
);

//Manejador de memoria
builder.Services.AddSession(options =>
{
    //Los segundos en que queremos que permanezca el estado 
    options.IdleTimeout = TimeSpan.FromSeconds(3600);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

//indicamos que haremos uso de estos metodos con la siguiente funcion
app.UseSession();
//Ya estan configuradas las variables de session, ahora hay que crearlas en el controlador...

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inicio}/{action=Index}/{id?}");

app.Run();
