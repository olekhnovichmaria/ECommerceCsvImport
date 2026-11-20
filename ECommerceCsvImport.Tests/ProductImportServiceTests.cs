using ECommerceCsvImport.Models;
using ECommerceCsvImport.Services;
using Xunit;

namespace ECommerceCsvImport.Tests;

public class ProductImportServiceTests
{
    private const string Header =
        "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number";

    private static (ProductImportService service, InMemoryProductRepository repository) CreateService()
    {
        var repository = new InMemoryProductRepository();
        return (new ProductImportService(repository), repository);
    }

    // Тест 1: Успешный импорт одной корректной строки
    [Fact]
    public void ImportFromLines_OneValidRow_ImportsProduct()
    {
        var (service, repository) = CreateService();
        var lines = new[]
        {
            Header,
            "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук для работы и учебы,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15"
        };

        var result = service.ImportFromLines(lines);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);

        var product = repository.GetBySku("1001");
        Assert.NotNull(product);
        Assert.Equal("Ноутбук Lenovo IdeaPad 3", product.Name);
        Assert.Equal(45990m, product.Price);
        Assert.Equal("Lenovo", product.Brand);
        Assert.Equal(24, product.WarrantyMonths);
    }

    // Тест 2: Успешный импорт нескольких строк
    [Fact]
    public void ImportFromLines_MultipleValidRows_ImportsAll()
    {
        var (service, repository) = CreateService();
        var lines = new[]
        {
            Header,
            "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15",
            "1002,Мышь Logitech M185,Беспроводная мышь,1290,RUB,Периферия,10,/images/mouse.jpg,true,Logitech,12,M185"
        };

        var result = service.ImportFromLines(lines);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(2, repository.Count);
    }

    // Тест 3: Файл не найден
    [Fact]
    public void ImportFromCsv_FileNotFound_ReturnsError()
    {
        var (service, _) = CreateService();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");

        var result = service.ImportFromCsv(path);

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Файл не найден", result.Errors[0].Message);
    }

    // Тест 4: Пустой файл
    [Fact]
    public void ImportFromCsv_EmptyFile_ReturnsError()
    {
        var (service, _) = CreateService();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        File.WriteAllText(path, string.Empty);

        try
        {
            var result = service.ImportFromCsv(path);

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Тест 5: Только заголовок без данных
    [Fact]
    public void ImportFromLines_HeaderOnly_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[] { Header });

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
    }

    // Тест 6: Неверный заголовок CSV
    [Fact]
    public void ImportFromLines_BadHeader_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[]
        {
            "sku,name,description,price,currency,category_name,stock,image_url,is_active"
        });

        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Неверные заголовки CSV", result.Errors[0].Message);
    }

    // Тест 7: Неверное количество полей в строке данных
    [Fact]
    public void ImportFromLines_WrongFieldCount_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[] { Header, "1001,Ноутбук Lenovo IdeaPad 3" });

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Неверное количество полей в строке", result.Errors[0].Message);
    }

    // Тест 8: Пустой SKU
    [Fact]
    public void ImportFromLines_EmptySku_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[]
        {
            Header,
            ",Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad3-15"
        });

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Поле sku не может быть пустым", result.Errors[0].Message);
    }

    // Тест 9: Пустое название товара
    [Fact]
    public void ImportFromLines_EmptyName_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[]
        {
            Header,
            "1001,,Бюджетный ноутбук,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad3-15"
        });

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Поле name не может быть пустым", result.Errors[0].Message);
    }

    // Тест 10: Неверная цена — отрицательное значение
    [Fact]
    public void ImportFromLines_NegativePrice_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[]
        {
            Header,
            "1003,Клавиатура A4Tech Bloody V3M,Механическая игровая клавиатура,-500,RUB,Периферия,7,/images/keyboard.jpg,true,A4Tech,24,V3M"
        });

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Поле price должно быть числом больше 0", result.Errors[0].Message);
    }

    // Тест 11: Неверная цена — нечисловое значение
    [Fact]
    public void ImportFromLines_NonNumericPrice_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[]
        {
            Header,
            "1003,Клавиатура A4Tech Bloody V3M,Механическая игровая клавиатура,abc,RUB,Периферия,7,/images/keyboard.jpg,true,A4Tech,24,V3M"
        });

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Поле price должно быть числом больше 0", result.Errors[0].Message);
    }

    // Тест 12: Отрицательный остаток
    [Fact]
    public void ImportFromLines_NegativeStock_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[]
        {
            Header,
            "1004,Монитор Dell S2722QC,Игровой монитор 27 дюймов,29990,RUB,Компьютеры,-5,/images/monitor.jpg,true,Dell,24,S2722QC"
        });

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Остаток на складе должен быть целым числом не меньше 0", result.Errors[0].Message);
    }

    // Тест 13: Неверное значение is_active
    [Fact]
    public void ImportFromLines_InvalidIsActive_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[]
        {
            Header,
            "1007,Webcam Logitech C920,Веб-камера Full HD,4990,RUB,Периферия,6,/images/webcam.jpg,yes,Logitech,24,C920"
        });

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Поле is_active должно иметь значение true, false, 1 или 0", result.Errors[0].Message);
    }

    // Тест 14: Отсутствует бренд
    [Fact]
    public void ImportFromLines_MissingBrand_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[]
        {
            Header,
            "1005,Видеокарта NVIDIA GeForce RTX 3060,,24990,RUB,Компьютеры,4,/images/gpu.jpg,true,,36,RTX3060"
        });

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Поле brand обязательно для варианта «Электроника»", result.Errors[0].Message);
    }

    // Тест 15: Гарантия меньше нуля
    [Fact]
    public void ImportFromLines_WarrantyBelowZero_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[]
        {
            Header,
            "1006,Накопитель SSD Samsung 870 EVO 1TB,Внутренний твердотельный накопитель,5990,RUB,Компьютеры,12,/images/ssd.jpg,true,Samsung,-1,870EVO-1T"
        });

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Гарантия должна быть целым числом от 0 до 60 месяцев", result.Errors[0].Message);
    }

    // Тест 16: Гарантия больше 60
    [Fact]
    public void ImportFromLines_WarrantyAbove60_ReturnsError()
    {
        var (service, _) = CreateService();

        var result = service.ImportFromLines(new[]
        {
            Header,
            "1006,Накопитель SSD Samsung 870 EVO 1TB,Внутренний твердотельный накопитель,5990,RUB,Компьютеры,12,/images/ssd.jpg,true,Samsung,61,870EVO-1T"
        });

        Assert.Equal(0, result.ImportedCount);
        Assert.Contains("Гарантия должна быть целым числом от 0 до 60 месяцев", result.Errors[0].Message);
    }

    // Тест 17: Дубликат SKU внутри одного файла
    [Fact]
    public void ImportFromLines_DuplicateSkuInFile_KeepsFirst()
    {
        var (service, repository) = CreateService();
        var lines = new[]
        {
            Header,
            "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук для работы и учебы,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15",
            "1001,Ноутбук ASUS VivoBook,Другой ноутбук,52990,RUB,Компьютеры,7,/images/asus.jpg,true,ASUS,24,VivoBook"
        };

        var result = service.ImportFromLines(lines);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);

        var product = repository.GetBySku("1001");
        Assert.NotNull(product);
        Assert.Equal("Ноутбук Lenovo IdeaPad 3", product.Name);
    }

    // Тест 18: Дубликат SKU с уже существующим товаром в репозитории
    [Fact]
    public void ImportFromLines_DuplicateSkuWithExisting_ReturnsError()
    {
        var (service, repository) = CreateService();
        repository.Add(new Product { Sku = "1001", Name = "Ноутбук Lenovo", Price = 45990m, Brand = "Lenovo" });

        var result = service.ImportFromLines(new[]
        {
            Header,
            "1001,Ноутбук ASUS VivoBook,Другой ноутбук,52990,RUB,Компьютеры,7,/images/asus.jpg,true,ASUS,24,VivoBook"
        });

        Assert.Equal(1, repository.Count);
        Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);
    }

    // Тест 19: Несколько ошибок в одной строке
    [Fact]
    public void ImportFromLines_MultipleErrorsInOneRow_ReportsAll()
    {
        var (service, repository) = CreateService();

        // 12 полей: sku и name пустые, price=-10, stock=-5, is_active=yes, brand пустой, warranty=61
        var result = service.ImportFromLines(new[]
        {
            Header,
            ",,Некорректная строка,-10,RUB,Компьютеры,-5,/images/x.jpg,yes,,61,"
        });

        Assert.Equal(0, result.ImportedCount);
        Assert.True(result.Errors.Count >= 4);
        Assert.Equal(0, repository.Count);
    }

    // Тест 20: Пустые необязательные поля разрешены
    [Fact]
    public void ImportFromLines_EmptyOptionalFields_Imported()
    {
        var (service, repository) = CreateService();

        var result = service.ImportFromLines(new[]
        {
            Header,
            "1011,Блок питания Corsair CV550,,4990,RUB,Компьютеры,9,,true,Corsair,60,CV550"
        });

        Assert.Equal(1, result.ImportedCount);
        Assert.Empty(result.Errors);

        var product = repository.GetBySku("1011");
        Assert.NotNull(product);
        Assert.Equal("", product.Description);
        Assert.Equal("", product.ImageUrl);
    }

    // Тест 21: Пустая валюта заменяется на RUB
    [Fact]
    public void ImportFromLines_EmptyCurrency_DefaultsToRUB()
    {
        var (service, repository) = CreateService();

        service.ImportFromLines(new[]
        {
            Header,
            "1011,Блок питания Corsair CV550,,4990,,Компьютеры,9,,true,Corsair,60,CV550"
        });

        var product = repository.GetBySku("1011");
        Assert.NotNull(product);
        Assert.Equal("RUB", product.Currency);
    }

    // Тест 22: Пустая категория заменяется на «Без категории»
    [Fact]
    public void ImportFromLines_EmptyCategory_DefaultsToNoCategory()
    {
        var (service, repository) = CreateService();

        service.ImportFromLines(new[]
        {
            Header,
            "1011,Блок питания Corsair CV550,,4990,RUB,,9,,true,Corsair,60,CV550"
        });

        var product = repository.GetBySku("1011");
        Assert.NotNull(product);
        Assert.Equal("Без категории", product.CategoryName);
    }

    // Тест 23: Кириллица сохраняется корректно
    [Fact]
    public void ImportFromLines_CyrillicPreserved()
    {
        var (service, repository) = CreateService();

        service.ImportFromLines(new[]
        {
            Header,
            "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук для работы и учебы,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15"
        });

        var product = repository.GetBySku("1001");
        Assert.NotNull(product);
        Assert.Equal("Ноутбук Lenovo IdeaPad 3", product.Name);
        Assert.Equal("Бюджетный ноутбук для работы и учебы", product.Description);
        Assert.Equal("Компьютеры", product.CategoryName);
    }
}