// Datasoft API örnek uygulaması.
//
// Sırasıyla: giriş yapar, cari kart ve stok kartı oluşturur (aynı kodla varsa mevcut kartı
// kullanır), bu ikisiyle bir satış faturası kaydeder ve oturumu kapatır.
//
// Kullanım:
//   dotnet run                          tüm adımlar
//   dotnet run -- --dry-run             faturayı kaydetmez, hazırlanan isteği dosyaya yazar
//   dotnet run -- --settings other.json farklı bir ayar dosyası kullanır

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DatasoftApiSample.Api;
using DatasoftApiSample.Configuration;
using DatasoftApiSample.Models;
using DatasoftApiSample.Models.Auth;
using DatasoftApiSample.Models.Customers;
using DatasoftApiSample.Models.Invoices;
using DatasoftApiSample.Models.Stocks;
using DatasoftApiSample.Services;

const string DryRunFileName = "invoice-preview.json";

Console.OutputEncoding = Encoding.UTF8;

if (!TryParseArguments(args, out string? settingsFile, out bool dryRun))
{
    Console.WriteLine("Kullanım: DatasoftApiSample [--settings <dosya.json>] [--dry-run]");
    return 2;
}

try
{
    AppSettings settings = AppSettings.Load(settingsFile);

    using var api = new DatasoftApiClient(settings.Connection);
    var lookups = new LookupService(api);
    var customers = new CustomerService(api, lookups);
    var stocks = new StockService(api, lookups);
    var invoices = new InvoiceService(api, customers, stocks);

    await LoginAsync(api);
    await EnsureCustomerAsync(customers, settings.Customer);
    await EnsureStockAsync(stocks, settings.Stock);
    await CreateInvoiceAsync(invoices, settings, dryRun);
    await LogoutAsync(api);

    Console.WriteLine();
    Console.WriteLine("Tüm adımlar tamamlandı.");
    return 0;
}
catch (DatasoftApiException exception)
{
    // Sunucu işlemi reddetti; mesaj hangi alanın neden reddedildiğini içerir.
    return Fail("API HATASI", exception.Message);
}
catch (HttpRequestException exception)
{
    // Sunucuya ulaşılamadı: adres, port, ağ ya da sertifika.
    return Fail("BAĞLANTI HATASI", exception.Message);
}
catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException or FileNotFoundException)
{
    // Ayar eksikliği ya da örneğin kapsamı dışında kalan bir durum.
    return Fail("HATA", exception.Message);
}

static async Task LoginAsync(DatasoftApiClient api)
{
    WriteHeader("1) Oturum açma");

    CredentialsDto session = await api.LoginAsync();

    Console.WriteLine($"   Kullanıcı : {session.UserName} (id: {session.UserId})");
    Console.WriteLine($"   Şirket    : {session.CompanyName} (id: {session.CompanyId})");
}

static async Task EnsureCustomerAsync(CustomerService customers, CustomerSettings settings)
{
    WriteHeader("2) Cari kart");

    Console.WriteLine("   Sistemdeki ilk cariler:");
    foreach (CustomerSummaryDto customer in await customers.SearchAsync(keyword: " ", maxCount: 5))
        Console.WriteLine($"   - {customer.Code,-14} {customer.Title}");

    CustomerCardDto? existing = await customers.FindByCodeAsync(settings.Code);
    if (existing is not null)
    {
        Console.WriteLine($"   \"{settings.Code}\" kodlu cari zaten var (id: {existing.Id}); yeni kart açılmadı.");
        return;
    }

    long customerId = await customers.CreateAsync(settings);
    Console.WriteLine($"   Cari kart oluşturuldu: {settings.Code} - {settings.Title} (id: {customerId})");
}

static async Task EnsureStockAsync(StockService stocks, StockSettings settings)
{
    WriteHeader("3) Stok kartı");

    StockCardDto? existing = await stocks.FindByCodeAsync(settings.Code);
    if (existing is not null)
    {
        Console.WriteLine($"   \"{settings.Code}\" kodlu stok zaten var (id: {existing.Id}); yeni kart açılmadı.");

        if (existing.GeneralInfo.Status != RecordStatus.Active)
            Console.WriteLine("   UYARI: Stok pasif durumda; pasif stoklar fatura satırında kullanılamaz.");
        return;
    }

    long stockId = await stocks.CreateAsync(settings);
    Console.WriteLine($"   Stok kartı oluşturuldu: {settings.Code} - {settings.Name} (id: {stockId})");
}

static async Task CreateInvoiceAsync(InvoiceService invoices, AppSettings settings, bool dryRun)
{
    WriteHeader(dryRun ? "4) Satış faturası (deneme: kaydedilmeyecek)" : "4) Satış faturası");

    // Tek satırlık örnek; daha fazla satır için listeye yeni InvoiceLineInput eklenir.
    List<InvoiceLineInput> lines =
    [
        new(settings.Stock.Code, settings.Invoice.Quantity, settings.Invoice.UnitPrice)
    ];

    InvoiceDto invoice = await invoices.PrepareSalesInvoiceAsync(settings.Customer.Code, lines, settings.Invoice);
    WriteInvoiceSummary(invoice);

    if (dryRun)
    {
        string filePath = Path.Combine(Environment.CurrentDirectory, DryRunFileName);
        await File.WriteAllTextAsync(filePath, ToIndentedJson(invoice));
        Console.WriteLine($"   Fatura kaydedilmedi. Hazırlanan istek: {filePath}");
        return;
    }

    long invoiceId = await invoices.SaveAsync(invoice);
    SavedInvoiceDto saved = await invoices.GetSavedAsync(invoiceId);

    Console.WriteLine($"   Fatura kaydedildi (id: {invoiceId}). Sunucudaki genel toplam: {saved.Total?.GeneralTotal:N2}");
}

static async Task LogoutAsync(DatasoftApiClient api)
{
    WriteHeader("5) Oturumu kapatma");

    await api.LogoutAsync();
    Console.WriteLine("   Oturum kapatıldı.");
}

static void WriteInvoiceSummary(InvoiceDto invoice)
{
    InvoiceGeneralInfoDto? generalInfo = invoice.GeneralInfo;
    InvoiceTotalDto? total = invoice.Total;

    Console.WriteLine($"   Belge     : {invoice.Settings?.Name} / {invoice.OperatorType?.Name} / {generalInfo?.Scenario?.Name}");
    Console.WriteLine($"   Seri-No   : {generalInfo?.Serial?.Code} {generalInfo?.SerialNumber}");
    Console.WriteLine($"   Cari      : {invoice.Customer?.Code} - {invoice.Customer?.Title}");
    Console.WriteLine($"   Tutar     : {total?.GrossTotal:N2} + {total?.TaxAddition:N2} vergi = {total?.GeneralTotal:N2} {generalInfo?.Currency?.Code}");
}

static string ToIndentedJson(InvoiceDto invoice)
{
    var options = new JsonSerializerOptions(DatasoftApiClient.JsonOptions)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping   // Türkçe karakterler okunaklı yazılsın
    };

    return JsonSerializer.Serialize(invoice, options);
}

static bool TryParseArguments(string[] args, out string? settingsFile, out bool dryRun)
{
    settingsFile = null;
    dryRun = false;

    for (int index = 0; index < args.Length; index++)
    {
        switch (args[index])
        {
            case "--settings" when index + 1 < args.Length:
                settingsFile = args[++index];
                break;
            case "--dry-run":
                dryRun = true;
                break;
            default:
                return false;
        }
    }

    return true;
}

static void WriteHeader(string text)
{
    Console.WriteLine();
    Console.WriteLine(text);
    Console.WriteLine(new string('-', text.Length));
}

static int Fail(string title, string message)
{
    Console.WriteLine();
    Console.WriteLine($"{title}: {message}");
    return 1;
}
