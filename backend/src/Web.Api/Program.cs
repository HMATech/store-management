using Infrastructure;
using Web.Api;
using Web.Api.Infrastructure;
using Web.Api.Endpoints;
using System.Reflection;
using Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>(MigrationExtensions.ApplyOnStartupKey))
{
    app.ApplyMigrations();
}

await app.CreateInitialUserAsync();

app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints(Assembly.GetExecutingAssembly());

app.Run();
