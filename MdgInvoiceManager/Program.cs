/// Katmanlarımızın Namespace'leri
using MdgInvoiceManager.Business.Abstract;
using MdgInvoiceManager.Business.Concreate;
using MdgInvoiceManager.Business.Concrete;
using MdgInvoiceManager.DataAccess.Data;
using MdgInvoiceManager.DataAccess.Repositories.Abstract;
using MdgInvoiceManager.DataAccess.Repositories.Concrete;
using MassTransit; // <-- YENİ: Kuyruk kütüphanesi
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.Web.CodeGeneration.Design;
using StackExchange.Redis;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();

// 1. Veritabanı Bağlantısı (DbContextFactory Kullanımı)
builder.Services.AddDbContext<MdgInvoiceDbContext>(options =>
  options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
          maxRetryCount: 5,
          maxRetryDelay: TimeSpan.FromSeconds(10),
          errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(60); // 60 saniye sorgu süresi
    }));

// Redis Cache Servis Kaydı
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "MdgInvoiceManager_";
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
  ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

// YENİ: MassTransit ve RabbitMQ Servis Kaydı
// -------------------------------------------------------------
builder.Services.AddMassTransit(x =>
{
    // Kuyruktan gelen mesajı işleyecek tüketici sınıfımızı kaydediyoruz
    x.AddConsumer<InvoiceCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // Docker üzerinde çalışan RabbitMQ'ya bağlanır (varsayılan port 5672)
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Fatura mesajlarının toplanacağı ve işleneceği kuyruk ucu
        cfg.ReceiveEndpoint("invoice-created-queue", e =>
        {
            e.ConfigureConsumer<InvoiceCreatedConsumer>(context);
        });
    });
});

// 2. Identity Servisi
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<MdgInvoiceDbContext>()
.AddDefaultTokenProviders();

// 3. JWT Kimlik Doğrulama Servisi
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "mdgadmin",
        ValidAudience = "mdgkullanici",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("mdg1234567891234mdg1234567891234"))
    };
});

// 4. Katmanlı Mimari Servis Kayıtları
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceService, InvoiceManager>();
builder.Services.AddScoped<IAuthService, AuthManager>();

// 5. Controller Desteği ve JSON Döngü Engelleme
builder.Services.AddControllersWithViews()
 .AddJsonOptions(options =>
 {
     options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
 });

// 6. Swagger Yapılandırması
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MdgInvoiceManager", Version = "v1" });

    // Şema çakışmalarını önleyen ayar
    options.CustomSchemaIds(type => type.FullName);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token'ınızı girin. Örnek: 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    });
});

var app = builder.Build();

// Otomatik Migration ve Rol Oluşturma
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 1. Sıfır SQL container'ında veritabanı ve Identity tablolarını açar
        var dbContext = services.GetRequiredService<MdgInvoiceDbContext>();
        dbContext.Database.Migrate();

        // 2. Rolleri ekler
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = new[] { "User", "Admin" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı başlatılırken veya roller eklenirken bir hata oluştu.");
    }
}

// 7. Middleware Yapılandırması
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 8. Yönlendirmeler
app.MapControllers();

app.Run();