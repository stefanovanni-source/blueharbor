using BlueHarbor.Data;
using BlueHarbor.Security;
using BlueHarbor.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Persistenza: database in-memory (temporaneo: vive finche l'app e' attiva) ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("blueharbor"));

// --- Servizi applicativi ---
builder.Services.AddScoped<TerminalClockService>();
builder.Services.AddScoped<ShipService>();
builder.Services.AddScoped<SchedulingService>();
builder.Services.AddSingleton<InMemoryUserStore>();

// --- Sicurezza: form login con cookie e due ruoli ---
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
    });
builder.Services.AddAuthorization();

// --- Presentazione: MVC + viste Razor ---
builder.Services.AddControllersWithViews();

// --- Swagger (endpoint tecnico di status) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed all'avvio: orologio (giorno 1) + banchine fisse.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DataInitializer.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Swagger UI disponibile su /swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
