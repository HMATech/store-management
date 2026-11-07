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
    .AddPresentation();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.ApplyMigrations();
}

await app.CreateInitialUserAsync();

app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints(Assembly.GetExecutingAssembly());

app.Run();
