using System.Text.Json;

namespace DatasoftApiSample.Configuration;

/// <summary>
/// appsettings.json dosyasının karşılığı. Ortama ve kullanıcıya özel her değer buradan gelir.
/// </summary>
public sealed class AppSettings
{
    public ConnectionSettings Connection { get; set; } = new();
    public CustomerSettings Customer { get; set; } = new();
    public StockSettings Stock { get; set; } = new();
    public InvoiceSettings Invoice { get; set; } = new();

    /// <summary>
    /// Ayar dosyasını okur. Yol verilmezse önce uygulamanın yanındaki, sonra çalışma
    /// klasöründeki appsettings.json kullanılır.
    /// </summary>
    public static AppSettings Load(string? filePath)
    {
        string path = filePath ?? FindDefaultFile();
        if (!File.Exists(path))
            throw new FileNotFoundException($"Ayar dosyası bulunamadı: {path}");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,   // dosyada "//" açıklamalarına izin verir
            AllowTrailingCommas = true
        };

        AppSettings settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), options)
                               ?? throw new InvalidOperationException($"Ayar dosyası boş: {path}");

        settings.Connection.ApplyEnvironmentVariables();
        settings.Connection.Validate();
        return settings;
    }

    private static string FindDefaultFile()
    {
        const string fileName = "appsettings.json";
        string besideApplication = Path.Combine(AppContext.BaseDirectory, fileName);

        return File.Exists(besideApplication)
            ? besideApplication
            : Path.Combine(Environment.CurrentDirectory, fileName);
    }
}

public sealed class ConnectionSettings
{
    /// <summary>Oturum (token) işlemlerinin yapıldığı servis.</summary>
    public string SalesApiUrl { get; set; } = "";

    /// <summary>Ön muhasebe işlemlerinin (cari, stok, fatura) yapıldığı servis.</summary>
    public string PreAccountingApiUrl { get; set; } = "";

    /// <summary>API kullanıcısının bağlı olduğu entegrasyon tanımının anahtarı (X-Api-Key).</summary>
    public string? ApiKey { get; set; }

    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    /// <summary>Çalışılacak şirket; boşsa kullanıcının varsayılan şirketi kullanılır.</summary>
    public long? CompanyId { get; set; }

    /// <summary>
    /// Gizli bilgileri dosyada tutmak istemeyenler için: tanımlı ortam değişkenleri dosyadaki
    /// değerlerin yerine geçer.
    /// </summary>
    public void ApplyEnvironmentVariables()
    {
        Username = Environment.GetEnvironmentVariable("DATASOFT_USERNAME") ?? Username;
        Password = Environment.GetEnvironmentVariable("DATASOFT_PASSWORD") ?? Password;
        ApiKey = Environment.GetEnvironmentVariable("DATASOFT_API_KEY") ?? ApiKey;

        if (long.TryParse(Environment.GetEnvironmentVariable("DATASOFT_COMPANY_ID"), out long companyId))
            CompanyId = companyId;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SalesApiUrl) || string.IsNullOrWhiteSpace(PreAccountingApiUrl))
            throw new InvalidOperationException("Connection.SalesApiUrl ve Connection.PreAccountingApiUrl dolu olmalı.");

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("Kullanıcı adı ve şifre boş olamaz (appsettings.json ya da DATASOFT_USERNAME / DATASOFT_PASSWORD).");
    }
}

/// <summary>Oluşturulacak örnek cari kart.</summary>
public sealed class CustomerSettings
{
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string TaxNumber { get; set; } = "";

    // Aşağıdaki dört alan adla yazılır; id'leri API'den aranarak bulunur.
    public string TaxOffice { get; set; } = "";
    public string Country { get; set; } = "Türkiye";
    public string City { get; set; } = "";
    public string County { get; set; } = "";

    public string? Neighborhood { get; set; }
    public string? Avenue { get; set; }
    public string? Street { get; set; }
    public string BuildingNo { get; set; } = "";
    public string? PostCode { get; set; }
    public string? FullAddress { get; set; }
    public string? Email { get; set; }
}

/// <summary>Oluşturulacak örnek stok kartı.</summary>
public sealed class StockSettings
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>Ölçü biriminin adı; ör. "Adet".</summary>
    public string Unit { get; set; } = "Adet";

    /// <summary>KDV oranı (%).</summary>
    public decimal VatRate { get; set; } = 20;

    /// <summary>KDV hariç satış fiyatı.</summary>
    public decimal SalePrice { get; set; }
}

/// <summary>Kaydedilecek örnek satış faturası.</summary>
public sealed class InvoiceSettings
{
    /// <summary>Belge cinsinin kodu; Fatura için "S4100".</summary>
    public string DocumentCode { get; set; } = "S4100";

    public decimal Quantity { get; set; } = 1;

    /// <summary>KDV hariç birim fiyat; boşsa stok kartındaki satış fiyatı kullanılır.</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>Kullanılacak seri; boşsa belge türünün varsayılan serisi.</summary>
    public string? SerialCode { get; set; }

    /// <summary>Yalnızca cari e-Fatura mükellefiyse kullanılır; ör. "Temel Fatura".</summary>
    public string? Scenario { get; set; }

    public string? Description { get; set; }
}
