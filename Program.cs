using Microsoft.EntityFrameworkCore;
using Sistema_de_tickets.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Inyección del contexto para la base de datos
builder.Services.AddDbContext<sistemadeticketsDBContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("ticketsDbConnection")
    )
);

// Manejador de memoria
builder.Services.AddSession(options =>
{
    // Los segundos en que queremos que permanezca el estado 
    options.IdleTimeout = TimeSpan.FromSeconds(3600);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Añade IWebHostEnvironment a los servicios
builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Indicamos que haremos uso de estos métodos con la siguiente función
app.UseSession();

// Ya están configuradas las variables de sesión, ahora hay que crearlas en el controlador...

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inicio}/{action=Index}/{id?}");

app.Run();
