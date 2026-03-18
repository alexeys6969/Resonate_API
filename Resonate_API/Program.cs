using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc(option => option.EnableEndpointRouting = true);
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Сотрудники",
        Description = "An API for Resonate"
    });
    option.SwaggerDoc("v2", new OpenApiInfo
    {
        Version = "v2",
        Title = "Категории",
        Description = "An API for Resonate"
    });
    option.SwaggerDoc("v3", new OpenApiInfo
    {
        Version = "v3",
        Title = "Товары",
        Description = "An API for Resonate"
    });
    option.SwaggerDoc("v4", new OpenApiInfo
    {
        Version = "v4",
        Title = "Продажи",
        Description = "An API for Resonate"
    });
    string PathFile = Path.Combine(AppContext.BaseDirectory, "Resonate_API.xml");
    option.IncludeXmlComments(PathFile);
});

var app = builder.Build();
app.UseSwagger();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Сотрудники");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "Категории");
    c.SwaggerEndpoint("/swagger/v3/swagger.json", "Товары");
    c.SwaggerEndpoint("/swagger/v4/swagger.json", "Продажи");
});
app.Run();

