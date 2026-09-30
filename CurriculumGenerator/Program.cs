using System.Text;
using CurriculumGenerator.Data;
using CurriculumGenerator.Entities;
using CurriculumGenerator.Services;
using CurriculumGenerator.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// 1. Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
    };
});

builder.Services.AddAuthorization();

// 4. Controllers + CORS
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontEnd", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Any localhost port, so the Vite dev server works whichever port it picks.
            policy.SetIsOriginAllowed(o =>
                      o.StartsWith("http://localhost") || o.StartsWith("http://127.0.0.1"))
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                          ?? Array.Empty<string>();
            if (origins.Length > 0)
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
            else
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// 5. Upload limits (PDFs)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

// 6. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Curriculum Generator API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer. Ex: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// 7. DI
builder.Services.AddHttpClient<GroqTranslationService>();
builder.Services.AddHttpClient<IAiSuggestionService, AiSuggestionService>();
builder.Services.AddScoped<CurriculumService>();
builder.Services.AddScoped<IPdfExtractionService, PdfExtractionService>();
builder.Services.AddScoped<IUsageGuardService, UsageGuardService>();
builder.Services.AddScoped<ResumeService>();
builder.Services.AddScoped<IValidator<Curriculum>, CurriculumValidator>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// 8. Pipeline
// Render terminates TLS and proxies to us over plain HTTP; trusting the
// forwarded headers keeps the real scheme visible (redirects, auth).
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontEnd");
// No HTTPS redirect in Development: the Vite proxy talks plain HTTP and a 307
// to the https port breaks CORS preflight requests.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Cheap liveness probe for Render's health check and keep-alive pings.
app.MapGet("/health", () => Results.Ok("ok"));

// Serve the built SPA (FrontEnd/dist) so `dotnet run` alone delivers the full app.
var frontendDist = Path.Combine(builder.Environment.ContentRootPath, "..", "FrontEnd", "dist");
if (Directory.Exists(frontendDist))
{
    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.GetFullPath(frontendDist));
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });
}

// 9. Apply pending migrations before serving: the hosted database (Supabase/Neon)
// starts empty, and a cold one may refuse the first connection for a moment.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migrateLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch (NpgsqlException ex) when (attempt < 10)
        {
            migrateLogger.LogWarning(ex, "Banco ainda não acessível (tentativa {Attempt}/10); nova tentativa em 3s...", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

app.Run();
