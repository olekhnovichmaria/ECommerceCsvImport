using ECommerceCsvImport.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавляем MVC-контроллеры
builder.Services.AddControllersWithViews();

// Регистрируем наши сервисы как Singleton
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddSingleton<IProductImportService, ProductImportService>();

var app = builder.Build();

// Настройка middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Маршрут по умолчанию — сразу на импорт товаров
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Import}/{id?}");

app.Run();