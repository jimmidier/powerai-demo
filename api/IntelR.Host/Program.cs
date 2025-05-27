using IntelR.Host;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.WithProperty("Application", "IntelR.Host")
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

builder.Services.ConfigureOpenAi(configuration);
builder.Services.ConfigureMcp(configuration);
builder.Services.ConfigureKernel();
builder.Services.ConfigureIntelR(configuration);

builder.Services.Configure<ApplicationOptions>(configuration.GetSection("App"));

var corsOrigins = configuration["Cors"]?.Split(',') ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp",
        builder => builder
            .WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowWebApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
