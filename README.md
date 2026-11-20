# ECommerceCsvImport

Модуль импорта товаров в интернет-магазин из CSV-файла с хранением в памяти.
Производственная практика ПП.02.01, вариант 1 (поля brand, warranty_months, model_number).
Предметная область — магазин электроники (ноутбуки, периферия, комплектующие, сетевое оборудование).

Выполнила: Олехнович Мария

## Технологии

- C#, ASP.NET Core MVC (.NET 8)
- xUnit для unit-тестов
- Собственный CSS в тёмной теме

## Как запустить

1. Открыть ECommerceCsvImport.sln в Visual Studio.
2. Запустить проект ECommerceCsvImport (F5).
3. В адресной строке перейти на /Products/Import.
4. В поле «Путь к CSV-файлу» ввести /csv/sample_products.csv.
5. Нажать «Импортировать товары».

Ожидаемый результат: всего строк 11, импортировано 11, ошибок 0.

Или из консоли:

```
dotnet run --project ECommerceCsvImport
dotnet test
```

## Формат CSV

```
sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number
1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук для работы и учебы,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15
```

| Поле | Проверка |
|------|----------|
| sku | не пустое, без повторов |
| name | не пустое |
| description | может быть пустым |
| price | число больше 0 |
| currency | если пусто — RUB |
| category_name | если пусто — «Без категории» |
| stock | целое число, не меньше 0 |
| image_url | может быть пустым |
| is_active | true / false / 1 / 0 |
| brand | не пустое |
| warranty_months | целое число от 0 до 120 |
| model_number | не пустое |

Если артикул уже есть (в файле или в памяти), строка пропускается и выводится ошибка.
Файл может быть в UTF-8 или Windows-1251, разделитель — запятая или точка с запятой.

## Структура

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
