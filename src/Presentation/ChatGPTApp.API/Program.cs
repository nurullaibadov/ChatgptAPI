using ChatGPTApp.API.Extensions;
using ChatGPTApp.API.Middlewares;
using ChatGPTApp.Infrastructure.Extensions;
using ChatGPTApp.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
//  Services
// ============================================================
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ChatGPTApp.API.Filters.ValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Infrastructure (DB, Repos, Services)
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Auth
builder.Services.AddJwtAuthentication(builder.Configuration);

// Swagger with JWT support
builder.Services.AddSwaggerWithJwt();

// CORS
builder.Services.AddCorsPolicies(builder.Configuration);

// ============================================================
//  Pipeline
// ============================================================
var app = builder.Build();

// Auto-migrate and seed on startup
await DbSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatGPT App API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "Production");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
