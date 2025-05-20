using WebApplication2.extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("ConnectionStrings.DefaultConnection string not found");
}
builder.Services.AddRepositories(connectionString);

var app = builder.Build();

// Configure standard middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Auto-register custom middleware
app.UseMiddlewaresFromDirectory("src/middleware");

app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Auto-register routes
app.MapRoutesFromDirectory("src/routes");
app.MapFallback(context =>
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new
    {
        status = 404,
        message = "Resource not found"
    });
});

app.Run();