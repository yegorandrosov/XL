using Microsoft.EntityFrameworkCore;
using XL.API.Data;
using Carter;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(x =>
    x.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining(typeof(Program)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "XL API", Version = "v1" });
    c.CustomSchemaIds(s => s.FullName.Replace("+", "."));
});

builder.Services.AddCarter(configurator: c =>
{
    c.WithModules(DiscoverCarterModules());
});

var app = builder.Build();

InitializeDatabase();

app.UseSwagger();
app.UseSwaggerUI();

app.MapCarter();

app.MapGet("/", () => "Hello World!");

app.Run();

static Type[] DiscoverCarterModules()
{
    var catalog = new DependencyContextAssemblyCatalog();
    var types = catalog.GetAssemblies().SelectMany(x => x.GetTypes());
    var modules = types
                .Where(t =>
                    !t.IsAbstract &&
                    typeof(ICarterModule).IsAssignableFrom(t)
                    && (t.IsPublic || t.IsNestedPublic)
                ).ToArray();

    return modules;
}

void InitializeDatabase()
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}