# ECommerceCsvImport

Модуль импорта товаров в интернет-магазин из CSV-файла с хранением в памяти.  
Производственная практика ПП.02.01, **вариант 1** (поля `brand`, `warranty_months`, `model_number`).  
Предметная область — магазин электроники (ноутбуки, периферия, комплектующие, сетевое оборудование).  

**Выполнила:** Олехнович Мария

---

## Технологии
- C#, ASP.NET Core MVC (.NET 8)
- xUnit для unit-тестов
- Собственный CSS в тёмной теме (фиолетово-синяя палитра)

---

## Как запустить
1. Открыть `ECommerceCsvImport.sln` в Visual Studio.
2. Запустить проект `ECommerceCsvImport` (F5).
3. В адресной строке перейти на `/Products/Import`.
4. Ввести путь: `/csv/sample_products.csv` и нажать «Импортировать товары».

**Ожидаемый результат:** всего строк 11, импортировано 11, ошибок 0.

---

## Формат CSV
| Поле | Проверка |
| --- | --- |
| `sku` | не пустое, без повторов |
| `name` | не пустое |
| `price` | число больше 0 |
| `currency` | если пусто — `RUB` |
| `category_name` | если пусто — «Без категории» |
| `stock` | целое число, не меньше 0 |
| `is_active` | `true` / `false` / `1` / `0` |
| `brand` | не пустое |
| `warranty_months` | целое число от 0 до 120 |
| `model_number` | не пустое |

---

## Структура проекта
- `ECommerceCsvImport/` — основной MVC проект (Controllers, Models, Services, Views, wwwroot)
- `ECommerceCsvImport.Tests/` — unit-тесты (31 тест) и папка `testdata`
- `ECommerceCsvImport.sln` — файл решения в корне репозитория

```
ECommerceCsvImport/
├── Controllers/ProductsController.cs      — страницы списка товаров и импорта
├── Models/                                — Product, ImportResult, ImportError, модели страниц
├── Services/
│   ├── CsvLineParser.cs                   — разбор строки CSV с кавычками
│   ├── IProductImportService.cs           — интерфейс сервиса импорта
│   ├── IProductRepository.cs              — интерфейс репозитория
│   ├── InMemoryProductRepository.cs       — хранение товаров в List (singleton)
│   └── ProductImportService.cs            — чтение CSV и проверка строк
├── Views/Products/                        — Index (список товаров), Import
├── Views/Shared/                          — _Layout, _ViewImports, _ViewStart
├── wwwroot/csv/                           — примеры CSV для проверки
└── Program.cs                             — регистрация DI и маршрутов

ECommerceCsvImport.Tests/
├── CsvLineParserTests.cs                  — тесты парсера
├── ProductImportServiceTests.cs           — тесты сервиса импорта
├── RepositoryTests.cs                     — тесты репозитория
└── testdata/                              — 7 CSV-файлов для тестовых сценариев
```

---

## Что сделано дополнительно

- Тёмный UI в фиолетово-синей палитре 
- Валидация всех полей варианта 1 (brand, warranty_months, model_number)
- Подсчёт общей стоимости остатков на странице списка товаров
- Обработка ошибок с указанием номера строки
- Singleton-репозиторий — данные сохраняются между запросами
- Полное покрытие unit-тестами (31 тест)

## Тестовые сценарии

| № | Файл | Ожидаемый результат | Результат |
|---|------|---------------------|-----------|
| 1 | sample_products.csv | 11 строк, 11 импортировано | ок |
| 2 | invalid_price.csv | ошибка цены в строке 2 | ок |
| 3 | empty_file.csv | «Данные для импорта отсутствуют» | ок |
| 4 | duplicate_sku.csv | 1 импортирован, 1 дубликат | ок |
| 5 | bad_active.csv | ошибка поля is_active | ок |
| 6 | missing_brand.csv | ошибка «brand обязательно» | ок |
| 7 | warranty_out_of_range.csv | ошибка гарантии (>120) | ок |
