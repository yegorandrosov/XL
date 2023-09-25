using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using XL.API.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(x =>
    x.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddMediatR(typeof(Program).Assembly);
builder.Services.AddCarter();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapCarter();

app.Run();