using WebApplication2.extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
        error = 404,
        message = "Resource not found"
    });
});

app.Run();