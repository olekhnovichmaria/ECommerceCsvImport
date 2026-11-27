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
        // 1. Преобразуем ввод пользователя в абсолютный путь
        var resolvedPath = ResolveFilePath(filePath);

        // 2. Если путь пустой — показываем ошибку
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

        // 3. ВАЖНО: передаём в сервис именно resolvedPath, а не filePath!
        var result = _importService.ImportFromCsv(resolvedPath);

        return View("Import", new ImportViewModel
        {
            FilePath = filePath ?? string.Empty,
            Result = result
        });
    }

    private string? ResolveFilePath(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var trimmed = input.Trim();

        if (Path.IsPathRooted(trimmed) && trimmed.Length > 1 && trimmed[1] == ':')
            return trimmed;

        trimmed = trimmed.TrimStart('/', '\\');

        if (string.IsNullOrWhiteSpace(trimmed)) return null;


        return Path.Combine(_environment.WebRootPath, trimmed);
    }
}