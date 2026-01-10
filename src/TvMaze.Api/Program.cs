using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using TvMaze.Api.Application.Behaviors;
using TvMaze.Api.Data;
using TvMaze.Api.Middleware;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add MediatR with validation pipeline behavior
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Add services to the container.
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

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Configure SQLite Database
builder.Services.AddDbContext<TvMazeContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=tvmaze.db"));

// Register ITvMazeContext
builder.Services.AddScoped<ITvMazeContext>(provider => provider.GetRequiredService<TvMazeContext>());

// Configure HttpClient with retry policy for TVMaze API using .NET 8 resilience
builder.Services.AddHttpClient("TvMazeApi")
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
    });

// Add CORS
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

// Add exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
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

// Serve static files for the demo website
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TvMazeContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();
