using System.Globalization;
using ECommerceCsvImport.Models;

namespace ECommerceCsvImport.Services;

public class ProductImportService : IProductImportService
{
    private readonly IProductRepository _repository;

    // Обязательные колонки CSV для варианта 1 «Электроника»
    private static readonly string[] RequiredColumns =
    {
        "sku", "name", "description", "price", "currency",
        "category_name", "stock", "image_url", "is_active",
        "brand", "warranty_months", "model_number"
    };

    public ProductImportService(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public ImportResult ImportFromCsv(string filePath)
    {
        // 1. Пустой путь или несуществующий файл
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new ImportResult
            {
                Errors = { new ImportError { RowNumber = 0, Message = "Файл не найден" } }
            };
        }

        // 2. Читаем строки файла
        string[] lines;
        try
        {
            lines = File.ReadAllLines(filePath);
        }
        catch (IOException)
        {
            return new ImportResult
            {
                Errors = { new ImportError { RowNumber = 0, Message = "Не удалось прочитать файл" } }
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new ImportResult
            {
                Errors = { new ImportError { RowNumber = 0, Message = "Нет доступа к файлу" } }
            };
        }

        // 3. Передаём строки в основную логику
        return ImportFromLines(lines);
    }

    public ImportResult ImportFromLines(IEnumerable<string> lines)
    {
        var result = new ImportResult();
        var lineList = lines as string[] ?? lines.ToArray();

        // Если строк нет или все пустые
        if (lineList.Length == 0 || lineList.All(string.IsNullOrWhiteSpace))
        {
            result.Errors.Add(new ImportError { RowNumber = 0, Message = "Данные для импорта отсутствуют" });
            return result;
        }

        // 1. Ищем первую непустую строку — это заголовок
        int headerIndex = -1;
        for (int i = 0; i < lineList.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lineList[i]))
            {
                headerIndex = i;
                break;
            }
        }

        // 2. Разбираем заголовок и строим карту колонок
        var headerFields = CsvLineParser.Parse(lineList[headerIndex]);
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerFields.Count; i++)
        {
            var name = headerFields[i].Trim();
            if (!columnMap.ContainsKey(name))
                columnMap[name] = i;
        }

        // 3. Проверяем наличие всех обязательных колонок
        foreach (var required in RequiredColumns)
        {
            if (!columnMap.ContainsKey(required))
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = headerIndex + 1,
                    Message = "Неверные заголовки CSV"
                });
                return result;
            }
        }

        int expectedFieldCount = headerFields.Count;

        // 4. Множество SKU, успешно импортированных в этом файле (для дубликатов)
        var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 5. Перебираем строки данных
        for (int i = headerIndex + 1; i < lineList.Length; i++)
        {
            var line = lineList[i];

            // Пустые строки пропускаем без ошибок
            if (string.IsNullOrWhiteSpace(line))
                continue;

            result.TotalRows++;
            int rowNumber = i + 1; // физический номер строки

            var values = CsvLineParser.Parse(line);

            // Проверка количества полей
            if (values.Count != expectedFieldCount)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = rowNumber,
                    Message = "Неверное количество полей в строке"
                });
                continue;
            }

            // Локальная функция получения значения по имени колонки
            string GetValue(string columnName)
            {
                if (columnMap.TryGetValue(columnName, out int index) && index < values.Count)
                    return values[index];
                return string.Empty;
            }

            // 6. Валидация полей (порядок: sku, name, price, stock, is_active, brand, warranty)
            var rowErrors = new List<ImportError>();

            string sku = GetValue("sku").Trim();
            if (string.IsNullOrWhiteSpace(sku))
                rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле sku не может быть пустым" });

            string name = GetValue("name").Trim();
            if (string.IsNullOrWhiteSpace(name))
                rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле name не может быть пустым" });

            decimal price = 0;
            bool priceValid = TryParseDecimal(GetValue("price"), out price) && price > 0;
            if (!priceValid)
                rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле price должно быть числом больше 0" });

            int stock = 0;
            bool stockValid = TryParseInt(GetValue("stock"), out stock) && stock >= 0;
            if (!stockValid)
                rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Остаток на складе должен быть целым числом не меньше 0" });

            bool isActive = false;
            bool activeValid = TryParseBool(GetValue("is_active"), out isActive);
            if (!activeValid)
                rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле is_active должно иметь значение true, false, 1 или 0" });

            string brand = GetValue("brand").Trim();
            if (string.IsNullOrWhiteSpace(brand))
                rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле brand обязательно для варианта «Электроника»" });

            int warrantyMonths = 0;
            bool warrantyValid = TryParseInt(GetValue("warranty_months"), out warrantyMonths)
                                 && warrantyMonths >= 0 && warrantyMonths <= 60;
            if (!warrantyValid)
                rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Гарантия должна быть целым числом от 0 до 60 месяцев" });

            // Если есть ошибки — товар не добавляем
            if (rowErrors.Count > 0)
            {
                result.Errors.AddRange(rowErrors);
                continue;
            }

            // 7. Проверка дубликата SKU (в файле или в репозитории)
            bool isDuplicate = _repository.GetBySku(sku) != null || importedSkus.Contains(sku);
            if (isDuplicate)
            {
                result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = "Артикул товара уже существует" });
                continue;
            }

            // 8. Создаём товар и добавляем в репозиторий
            var product = new Product
            {
                Sku = sku,
                Name = name,
                Description = GetValue("description").Trim(),
                Price = price,
                Currency = NormalizeCurrency(GetValue("currency")),
                CategoryName = NormalizeCategory(GetValue("category_name")),
                StockQuantity = stock,
                ImageUrl = GetValue("image_url").Trim(),
                IsActive = isActive,
                Brand = brand,
                WarrantyMonths = warrantyMonths,
                ModelNumber = GetValue("model_number").Trim()
            };

            _repository.Add(product);
            importedSkus.Add(sku);
            result.ImportedCount++;
        }

        // 9. Финальная проверка: если данных не было вообще
        if (result.TotalRows == 0 && result.Errors.Count == 0)
        {
            result.Errors.Add(new ImportError { RowNumber = 0, Message = "Данные для импорта отсутствуют" });
        }

        return result;
    }

    // Вспомогательные методы парсинга
    private static bool TryParseDecimal(string value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

    private static bool TryParseInt(string value, out int result)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static bool TryParseBool(string value, out bool result)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "true":
            case "1":
                result = true;
                return true;
            case "false":
            case "0":
                result = false;
                return true;
            default:
                result = false;
                return false;
        }
    }

    private static string NormalizeCurrency(string value)
        => string.IsNullOrWhiteSpace(value) ? "RUB" : value.Trim().ToUpperInvariant();

    private static string NormalizeCategory(string value)
        => string.IsNullOrWhiteSpace(value) ? "Без категории" : value.Trim();
}