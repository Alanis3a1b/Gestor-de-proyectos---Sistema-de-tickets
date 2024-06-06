using Microsoft.EntityFrameworkCore;
using Sistema_de_tickets.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Inyecci�n del contexto para la base de datos
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

// A�ade IWebHostEnvironment a los servicios
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

// Indicamos que haremos uso de estos m�todos con la siguiente funci�n
app.UseSession();

// Ya est�n configuradas las variables de sesi�n, ahora hay que crearlas en el controlador...

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Inicio}/{action=Index}/{id?}");

app.Run();
