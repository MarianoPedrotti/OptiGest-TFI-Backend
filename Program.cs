using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaDeVentas.Data;
using SistemaDeVentas.Entities;
using SistemaDeVentas.Helpers;
using SistemaDeVentas.Repositories;
using SistemaDeVentas.Repositories.Interfaces;
using SistemaDeVentas.Services;
using SistemaDeVentas.Services.Interfaces;
using System.Text;


var builder = WebApplication.CreateBuilder(args);


// ==========================
// Controllers
// ==========================

builder.Services.AddControllers();


// ==========================
// Base de datos
// ==========================

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration
            .GetConnectionString("DefaultConnection")
    )
);


// ==========================
// Dependencias
// ==========================

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtHelper>();

builder.Services.AddScoped<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>
>();

builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();

builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();

builder.Services.AddScoped<IVehiculoRepository, VehiculoRepository>();
builder.Services.AddScoped<IVehiculoService, VehiculoService>();

builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IVentaService, VentaService>();

builder.Services.AddScoped<IPagoVentaRepository, PagoVentaRepository>();
builder.Services.AddScoped<IPagoVentaService, PagoVentaService>();

builder.Services.AddScoped<
    ICuentaCorrienteRepository,
    CuentaCorrienteRepository
>();

builder.Services.AddScoped<
    ICuentaCorrienteService,
    CuentaCorrienteService
>();

builder.Services.AddScoped<
    IConfiguracionFinanciacionRepository,
    ConfiguracionFinanciacionRepository
>();

builder.Services.AddScoped<
    IConfiguracionFinanciacionService,
    ConfiguracionFinanciacionService
>();

builder.Services.AddScoped<IFinanciacionRepository, FinanciacionRepository>();
builder.Services.AddScoped<IOperacionFinancieraRepository, OperacionFinancieraRepository>();
builder.Services.AddScoped<IFinanciacionService, FinanciacionService>();

builder.Services.AddScoped<ICuotaRepository, CuotaRepository>();
builder.Services.AddScoped<ICuotaService, CuotaService>();

builder.Services.AddScoped<IPagoCuotaRepository, PagoCuotaRepository>();
builder.Services.AddScoped<IPagoCuotaService, PagoCuotaService>();

builder.Services.AddScoped<
    IMarcaVehiculoRepository,
    MarcaVehiculoRepository
>();

builder.Services.AddScoped<
    IMarcaVehiculoService,
    MarcaVehiculoService
>();

builder.Services.AddScoped<
    IMarcaProductoRepository,
    MarcaProductoRepository
>();

builder.Services.AddScoped<
    IMarcaProductoService,
    MarcaProductoService
>();

builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();

builder.Services.AddScoped<
    ISubCategoriaRepository,
    SubCategoriaRepository
>();

builder.Services.AddScoped<
    ISubCategoriaService,
    SubCategoriaService
>();

builder.Services.AddScoped<IModeloRepository, ModeloRepository>();
builder.Services.AddScoped<IModeloService, ModeloService>();

builder.Services.AddScoped<IDashboardService, DashboardService>();


// ==========================
// Swagger
// ==========================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Ingrese el token JWT"
        }
    );

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        }
    );
});


// ==========================
// CORS
// ==========================

var frontendUrl =
    builder.Configuration["FrontendUrl"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Vue", policy =>
    {
        policy
            .WithOrigins(frontendUrl!)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// ==========================
// JWT
// ==========================

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ClockSkew = TimeSpan.Zero,

            ValidIssuer =
                builder.Configuration["Jwt:Issuer"],

            ValidAudience =
                builder.Configuration["Jwt:Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        builder.Configuration["Jwt:Key"]!
                    )
                )
        };
});


// ==========================
// Autorizacion global
// ==========================

builder.Services.AddAuthorization();
var app = builder.Build();


// ==========================
// Swagger solo Development
// ==========================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// ==========================
// Middleware
// ==========================
app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("Vue");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers()
    .RequireAuthorization();

app.MapFallbackToFile("index.html")
    .AllowAnonymous();

app.Run();
