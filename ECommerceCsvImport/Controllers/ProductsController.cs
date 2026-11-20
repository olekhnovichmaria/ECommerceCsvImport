using Microsoft.AspNetCore.Mvc;
using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Microsoft.Extensions.Hosting;

namespace ECommerceCsvImport.Controllers;

public class ProductsController : Controller
{
    private readonly IProductRepository _repository;
    private readonly IProductImportService _importService;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(
        IProductRepository repository,
        IProductImportService importService,
        IWebHostEnvironment environment)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    // GET: /Products/Index
    [HttpGet]
    public ActionResult Index()
    {
        var viewModel = new ProductsIndexViewModel
        {
            Products = _repository.GetAll()
        };
        return View(viewModel);
    }

    // GET: /Products/Import
    [HttpGet]
    public ActionResult Import()
    {
        return View(new ImportViewModel());
    }

    // POST: /Products/Import
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Import(string filePath)
    {
        var resolvedPath = ResolveFilePath(filePath);

        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return View("Import", new ImportViewModel
            {
                FilePath = filePath ?? string.Empty,
                Result = new ImportResult
                {
                    Errors = { new ImportError { RowNumber = 0, Message = "Укажите путь к CSV-файлу" } }
                }
            });
        }

        var result = _importService.ImportFromCsv(@"C:\Users\user\Desktop\Берестов_ПМ.02_ноябрь\ECommerceCsvImport\wwwroot\csv\sample_products.csv");

        return View("Import", new ImportViewModel
        {
            FilePath = filePath ?? string.Empty,
            Result = result
        });
    }

    // Вспомогательный метод для преобразования относительных путей в абсолютные
    private string? ResolveFilePath(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var trimmed = input.Trim();

        // Если путь абсолютный (например, C:\data\file.csv)
        if (Path.IsPathRooted(trimmed))
            return trimmed;

        // Если путь относительный (например, /csv/sample_products.csv)
        if (trimmed.StartsWith('/'))
            trimmed = trimmed.TrimStart('/');

        return Path.Combine(_environment.WebRootPath, trimmed);
    }
}