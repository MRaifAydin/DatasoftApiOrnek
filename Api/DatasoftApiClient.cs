using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DatasoftApiSample.Configuration;
using DatasoftApiSample.Models.Auth;

namespace DatasoftApiSample.Api;

/// <summary>
/// Datasoft API ile konuşan HTTP katmanı.
///
/// İki ayrı servis vardır:
///   <list type="bullet">
///     <item>Sales API: oturum (token) işlemleri.</item>
///     <item>Pre-Accounting API: ön muhasebe işlemleri (cari, stok, fatura...).</item>
///   </list>
/// Önce Sales API'den token alınır; bu sınıf token'ı saklar ve Pre-Accounting çağrılarına
/// <c>Authorization: Bearer</c> başlığıyla kendisi ekler.
/// </summary>
public sealed class DatasoftApiClient : IDisposable
{
    private const string ApiKeyHeader = "X-Api-Key";

    /// <summary>
    /// İstekler camelCase yazılır. Yanıtlar büyük/küçük harf duyarsız okunur, çünkü Sales API
    /// alan adlarını PascalCase ("IsSuccess"), Pre-Accounting API camelCase ("isSuccess") döndürür.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(100) };
    private readonly ConnectionSettings _settings;
    private readonly string _salesApiUrl;
    private readonly string _preAccountingApiUrl;

    public DatasoftApiClient(ConnectionSettings settings)
    {
        _settings = settings;
        _salesApiUrl = settings.SalesApiUrl.TrimEnd('/');
        _preAccountingApiUrl = settings.PreAccountingApiUrl.TrimEnd('/');
    }

    /// <summary>Açık oturumun bilgileri; giriş yapılmadıysa null.</summary>
    public CredentialsDto? Session { get; private set; }

    // ---------------------------------------------------------------------
    //  Oturum (Sales API)
    // ---------------------------------------------------------------------

    /// <summary>
    /// Kullanıcı adı ve şifreyle oturum açar.
    /// <c>POST {SalesApi}/api/salesanduser/token/authpass</c>
    /// </summary>
    /// <remarks>
    /// Bu uç iki kullanıcı türü için farklı davranır:
    /// <list type="bullet">
    ///   <item>
    ///     <b>Entegrasyon (API) kullanıcısı:</b> Bir entegrasyon tanımına bağlıdır ve o tanımın
    ///     anahtarını <c>X-Api-Key</c> başlığında göndermek zorundadır. Kullanıcı, çalışacağı
    ///     şirkete davet edilmiş olmalıdır. Anahtar başka bir kullanıcıyla gönderilirse
    ///     <c>USER_NOT_FOUND</c> döner.
    ///   </item>
    ///   <item>
    ///     <b>Standart (arayüz) kullanıcısı:</b> Anahtarsız girişte sunucu, giriş ekranındaki robot
    ///     doğrulamasının yanıtını ister; yoksa <c>INVALID_CAPTCHA</c> döner. Bu yanıt yalnızca
    ///     tarayıcıda üretilebildiği için standart kullanıcıyla buradan giriş yapılamaz.
    ///   </item>
    /// </list>
    /// </remarks>
    public async Task<CredentialsDto> LoginAsync()
    {
        var request = new LoginRequest
        {
            Username = _settings.Username,
            Password = _settings.Password,
            CompanyId = _settings.CompanyId
        };

        using HttpRequestMessage message = CreateRequest(HttpMethod.Post, _salesApiUrl + "/api/salesanduser/token/authpass", request);

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            message.Headers.Add(ApiKeyHeader, _settings.ApiKey);

        CredentialsDto credentials;
        try
        {
            credentials = await SendAsync<CredentialsDto>(message);
        }
        catch (DatasoftApiException exception) when (exception.HasErrorCode("INVALID_CAPTCHA"))
        {
            throw new DatasoftApiException(
                "Giriş reddedildi: sunucu robot doğrulaması (captcha) istiyor. " +
                "Anahtarsız girişlerde bu doğrulama zorunludur; bir API kullanıcısı ve ona ait " +
                "X-Api-Key ile giriş yapın (appsettings.json -> Connection.ApiKey).");
        }

        Session = ValidateCredentials(credentials);
        return Session;
    }

    /// <summary>
    /// Access token'ı, şifre göndermeden refresh token ile yeniler.
    /// <c>POST {SalesApi}/api/salesanduser/Token/authtoken</c>
    /// </summary>
    public async Task RefreshSessionAsync()
    {
        if (Session?.RefreshToken is null)
        {
            await LoginAsync();
            return;
        }

        var request = new RefreshTokenRequest
        {
            RefreshToken = Session.RefreshToken,
            UserId = Session.UserId
        };

        try
        {
            using HttpRequestMessage message = CreateRequest(HttpMethod.Post, _salesApiUrl + "/api/salesanduser/Token/authtoken", request);
            Session = ValidateCredentials(await SendAsync<CredentialsDto>(message));
        }
        catch (DatasoftApiException)
        {
            // Refresh token da geçersizse baştan giriş yapılır.
            await LoginAsync();
        }
    }

    /// <summary>
    /// Oturumu kapatır; refresh token sunucuda geçersiz olur.
    /// <c>POST {SalesApi}/api/salesanduser/Token/LogOut</c>
    /// </summary>
    public async Task LogoutAsync()
    {
        if (Session is null)
            return;

        var request = new LogoutRequest
        {
            RefreshToken = Session.RefreshToken,
            UserId = Session.UserId
        };

        using HttpRequestMessage message = CreateRequest(HttpMethod.Post, _salesApiUrl + "/api/salesanduser/Token/LogOut", request);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session.AccessToken);

        await SendAsync<bool>(message);
        Session = null;
    }

    private static CredentialsDto ValidateCredentials(CredentialsDto credentials)
    {
        if (credentials.TwoFactorRequired)
            throw new DatasoftApiException("Bu kullanıcı için iki adımlı doğrulama gerekiyor. Entegrasyon için API kullanıcısı kullanın.");

        if (string.IsNullOrEmpty(credentials.AccessToken))
        {
            string hint = credentials.RequiredBranchChoice ? " Şube seçimi gerekiyor."
                        : credentials.RequiredPeriodChoice ? " Dönem seçimi gerekiyor."
                        : "";
            throw new DatasoftApiException("Giriş yanıtında token yok." + hint);
        }

        return credentials;
    }

    // ---------------------------------------------------------------------
    //  Pre-Accounting API çağrıları
    // ---------------------------------------------------------------------

    /// <summary>Pre-Accounting API'ye POST atar ve yanıt zarfındaki veriyi döndürür.</summary>
    /// <param name="path">Uç yolu; ör. "/api/Stock/Upsert".</param>
    /// <param name="body">
    /// İstek gövdesi. Çoğu uç bir nesne bekler; bazıları yalnızca bir id (ör. <c>15</c>) veya
    /// kod (ör. <c>"MUS-001"</c>) bekler.
    /// </param>
    public Task<TData> PostAsync<TData>(string path, object body)
        => SendAuthorizedAsync<TData>(HttpMethod.Post, path, body);

    /// <summary>Pre-Accounting API'ye GET atar ve yanıt zarfındaki veriyi döndürür.</summary>
    public Task<TData> GetAsync<TData>(string path)
        => SendAuthorizedAsync<TData>(HttpMethod.Get, path, body: null);

    /// <summary><see cref="PostAsync{TData}"/> gibidir; kayıt bulunamazsa hata yerine null döndürür.</summary>
    public async Task<TData?> PostOrDefaultAsync<TData>(string path, object body) where TData : class
    {
        try { return await PostAsync<TData>(path, body); }
        catch (DatasoftApiException exception) when (exception.IsNotFound) { return null; }
    }

    /// <summary><see cref="GetAsync{TData}"/> gibidir; kayıt bulunamazsa hata yerine null döndürür.</summary>
    public async Task<TData?> GetOrDefaultAsync<TData>(string path) where TData : class
    {
        try { return await GetAsync<TData>(path); }
        catch (DatasoftApiException exception) when (exception.IsNotFound) { return null; }
    }

    private async Task<TData> SendAuthorizedAsync<TData>(HttpMethod method, string path, object? body)
    {
        if (Session is null)
            throw new InvalidOperationException("Önce LoginAsync çağrılmalı.");

        using HttpRequestMessage message = CreateAuthorizedRequest(method, path, body);
        using HttpResponseMessage response = await _http.SendAsync(message);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return await ReadResponseAsync<TData>(response, path);

        // 401: token geçersiz ya da süresi dolmuş. Bir kez yenilenir ve istek tekrarlanır.
        await RefreshSessionAsync();

        using HttpRequestMessage retryMessage = CreateAuthorizedRequest(method, path, body);
        using HttpResponseMessage retryResponse = await _http.SendAsync(retryMessage);
        return await ReadResponseAsync<TData>(retryResponse, path);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string path, object? body)
    {
        HttpRequestMessage message = CreateRequest(method, _preAccountingApiUrl + path, body);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Session!.AccessToken);
        return message;
    }

    // ---------------------------------------------------------------------
    //  Ortak
    // ---------------------------------------------------------------------

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, object? body)
    {
        var message = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            // Gövde çalışma zamanındaki tipine göre yazılır; böylece nesneler de, tek başına
            // gönderilen id/kod değerleri de geçerli JSON'a dönüşür.
            string json = JsonSerializer.Serialize(body, body.GetType(), JsonOptions);
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return message;
    }

    private async Task<TData> SendAsync<TData>(HttpRequestMessage message)
    {
        using HttpResponseMessage response = await _http.SendAsync(message);
        return await ReadResponseAsync<TData>(response, message.RequestUri!.AbsolutePath);
    }

    /// <summary>Yanıt zarfını açar: başarılıysa veriyi döndürür, değilse hata fırlatır.</summary>
    private static async Task<TData> ReadResponseAsync<TData>(HttpResponseMessage response, string endpoint)
    {
        string content = await response.Content.ReadAsStringAsync();

        if (!LooksLikeEnvelope(content))
        {
            // Zarf yoksa istek iş kurallarına hiç ulaşmamıştır: yanlış adres, ağ geçidi hatası ya da
            // zorunlu bir alanın hiç gönderilmemesi (model doğrulaması) gibi.
            string preview = content.Length > 300 ? content[..300] + "..." : content;
            throw new DatasoftApiException($"{endpoint} beklenmeyen bir yanıt döndürdü (HTTP {(int)response.StatusCode}): {preview}");
        }

        // Veri önce ham JSON olarak alınır: başarısız yanıtta "data" null gelir ve doğrudan
        // hedef tipe (ör. long) çevrilmeye çalışılırsa asıl hata mesajı kaybolur.
        ApiResponse<JsonElement> envelope = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(content, JsonOptions)!;

        if (!envelope.IsSuccess)
            throw new DatasoftApiException(endpoint, envelope.Errors ?? []);

        return envelope.Data.Deserialize<TData>(JsonOptions)!;
    }

    private static bool LooksLikeEnvelope(string content)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(content);

            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.EnumerateObject()
                       .Any(property => property.Name.Equals("isSuccess", StringComparison.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public void Dispose() => _http.Dispose();
}
