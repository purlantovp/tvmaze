using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using TvMaze.Api.Application.Behaviors;
using TvMaze.Api.Data;
using TvMaze.Api.Middleware;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "TVMaze API",
        Version = "v1",
        Description = "API for accessing TVMaze show and cast information",
        Contact = new()
        {
            Name = "TVMaze API",
            Url = new Uri("https://www.tvmaze.com/api")
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddDbContext<TvMazeContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=tvmaze.db"));

builder.Services.AddScoped<ITvMazeContext>(provider => provider.GetRequiredService<TvMazeContext>());

builder.Services.AddHttpClient("TvMazeApi", client =>
{
    var baseAddress = builder.Configuration.GetValue<string>("TvMazeSettings:BaseUrl");
    client.BaseAddress = new Uri(baseAddress);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TVMaze API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TvMazeContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();
