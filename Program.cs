using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Hl7VaccinationSender.Models;
using Hl7VaccinationSender.Services;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var options = configuration.GetSection("Api").Get<ApiOptions>()
    ?? throw new InvalidOperationException("Falta la configuración 'Api'.");

var inputFolder = configuration["Input:Folder"] ?? "./messages";
var csvPath = configuration["Output:ReportCsv"] ?? "./output/report.csv";
var jsonPath = configuration["Output:ReportJson"] ?? "./output/report.json";

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
var api = new ApiClient(http, options);

var results = new List<TransactionResult>();

Console.WriteLine("Verificando conectividad con echo...");

HttpResponseMessage? echo = null;

for (var attempt = 1; attempt <= 3; attempt++)
{
    try
    {
        echo = await api.EchoAsync(CancellationToken.None);

        if (echo.IsSuccessStatusCode)
            break;

        Console.WriteLine($"Intento {attempt}: echo devolvió HTTP {(int)echo.StatusCode}.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Intento {attempt}: error de red: {ex.Message}");
    }

    if (attempt < 3)
        await Task.Delay(TimeSpan.FromSeconds(10));
}

if (echo is null || !echo.IsSuccessStatusCode)
{
    var code = echo is null ? "sin respuesta" : $"HTTP {(int)echo.StatusCode}";
    Console.WriteLine($"echo no está disponible ({code}).");
    return 2;
}

Console.WriteLine("echo OK. Enviando mensajes HL7...");

var files = Directory.GetFiles(inputFolder, "*.hl7")
    .OrderBy(f => f, StringComparer.Ordinal)
    .ToList();

if (files.Count == 0)
{
    Console.WriteLine($"No se encontraron archivos .hl7 en: {Path.GetFullPath(inputFolder)}");
    return 1;
}

foreach (var file in files)
{
    var fileName = Path.GetFileName(file);

    try
    {
        var hl7 = Hl7FileReader.Read(file);
        var message = Hl7MessageParser.Parse(hl7);

        Console.WriteLine($"Enviando {fileName} | MSH-10: {message.MessageControlId}");

        var response = await api.SendFileAsync(fileName, hl7, CancellationToken.None);
        var body = await response.Content.ReadAsStringAsync(CancellationToken.None);

        results.Add(AckClassifier.Classify(fileName, message, response, body));
    }
    catch (Exception ex)
    {
        results.Add(new TransactionResult
        {
            FileName = fileName,
            Status = TransactionStatus.Unknown,
            Error = ex.Message,
            Reason = "No se pudo enviar o procesar el mensaje.",
            SentAtUtc = DateTimeOffset.UtcNow
        });
    }
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(csvPath))!);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(jsonPath))!);

WriteCsv(csvPath, results);

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(results, jsonOptions));

PrintSummary(results, csvPath, jsonPath);
return 0;

static void WriteCsv(string path, List<TransactionResult> results)
{
    var sb = new StringBuilder();

    sb.AppendLine("Archivo,MSH-10,Tipo,Estado,Codigo ACK,Correlacion MSA-2,HTTP,Motivo,Error,Fecha envio UTC");

    foreach (var r in results)
    {
        sb.AppendLine(string.Join(",",
            Escape(r.FileName),
            Escape(r.MessageControlId),
            Escape(r.MessageType),
            r.Status,
            Escape(r.AckCode),
            Escape(r.CorrelationId),
            r.HttpStatusCode?.ToString(CultureInfo.InvariantCulture) ?? "",
            Escape(r.Reason),
            Escape(r.Error),
            r.SentAtUtc.ToString("O")));
    }

    File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
}

static string Escape(string? value)
{
    value ??= "";
    return $"\"{value.Replace("\"", "\"\"")}\"";
}

static void PrintSummary(List<TransactionResult> results, string csvPath, string jsonPath)
{
    Console.WriteLine();
    Console.WriteLine("========== RESUMEN ==========");

    foreach (var status in Enum.GetValues<TransactionStatus>())
    {
        var items = results.Where(r => r.Status == status).ToList();

        Console.WriteLine();
        Console.WriteLine($"{status}: {items.Count}");

        foreach (var r in items)
        {
            Console.WriteLine($"- {r.FileName} | MSH-10: {r.MessageControlId} | ACK: {r.AckCode}");
            Console.WriteLine($"  Motivo: {r.Reason}");

            if (!string.IsNullOrWhiteSpace(r.Error))
                Console.WriteLine($"  Error técnico: {Truncate(r.Error)}");
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Reporte CSV: {Path.GetFullPath(csvPath)}");
    Console.WriteLine($"Reporte JSON: {Path.GetFullPath(jsonPath)}");
}

static string Truncate(string value)
    => value.Length <= 500 ? value : value[..500];