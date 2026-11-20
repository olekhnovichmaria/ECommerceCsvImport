using System.Text;

namespace ECommerceCsvImport.Services;

public static class CsvLineParser
{
    public static IReadOnlyList<string> Parse(string line)
    {
        if (string.IsNullOrEmpty(line))
            return new List<string>();

        var fields = new List<string>();
        bool inQuotes = false;
        var currentField = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Проверяем, не является ли это экранированной кавычкой ("")
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++; // Пропускаем следующую кавычку
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        // Добавляем последнее поле
        fields.Add(currentField.ToString().Trim());

        return fields;
    }
}