# Datasoft API Örnek Uygulaması

Datasoft API'sinin nasıl kullanılacağını gösteren bir .NET konsol uygulaması. Sırasıyla:

1. Giriş yapar (token alır)
2. Cari kart oluşturur
3. Stok kartı oluşturur
4. Bu cari ve stokla satış faturası **kaydeder** (e-belge olarak göndermez)
5. Oturumu kapatır

Ek NuGet paketi yoktur; yalnızca .NET 10 SDK gerekir.

## Çalıştırma

Visual Studio'da `DatasoftApiSample.slnx` dosyasını açın ya da komut satırından çalıştırın:

```powershell
dotnet run
```

```powershell
dotnet run -- --dry-run
```

```powershell
dotnet run -- --settings other.json
```

| Parametre | Ne yapar |
|---|---|
| *(yok)* | Tüm adımları çalıştırır, faturayı kaydeder |
| `--dry-run` | Faturayı kaydetmez; gönderilecek isteği `invoice-preview.json` dosyasına yazar. Cari ve stok yoksa yine oluşturulur |
| `--settings <dosya>` | `appsettings.json` yerine başka bir ayar dosyası kullanır |

Uygulama tekrar çalıştırılabilir: cari ve stok koda göre aranır, varsa yeniden açılmaz. Fatura her çalıştırmada yeni bir numarayla kaydedilir. Yeni kart oluşturulduğunu görmek için `appsettings.json` içindeki `Customer.Code`, `Customer.TaxNumber` ve `Stock.Code` değerlerini değiştirin.

## Ayarlar

Ortama ve kullanıcıya özel her şey `appsettings.json` dosyasındadır. Bu dosya gizli bilgi içerdiği için depoda yoktur; ilk kullanımda örnek dosyayı kopyalayıp bağlantı bilgilerini doldurun:

```powershell
Copy-Item appsettings.example.json appsettings.json
```

Şifre ve anahtarı dosyada tutmak istemezseniz ortam değişkeni verebilirsiniz (dosyadaki değerin yerine geçer): `DATASOFT_USERNAME`, `DATASOFT_PASSWORD`, `DATASOFT_API_KEY`, `DATASOFT_COMPANY_ID`.

> Örnek dosyadaki adresler entegrasyon **test** ortamına aittir.

## Proje yapısı

```
Program.cs                 Ana akış: adımları sırayla çağırır, sonuçları ekrana yazar
Configuration/             appsettings.json karşılığı sınıflar
Api/
  DatasoftApiClient.cs     Giriş, token yenileme, çıkış; Bearer token'lı çağrılar
  ApiResponse.cs           Yanıt zarfı
  PagedRequest.cs          Liste/arama uçlarının istek kalıbı
  DatasoftApiException.cs  API hatası
Models/                    İstek ve yanıt DTO'ları
  Enums.cs                 API'nin sayı olarak beklediği seçenekler
  Auth/ Common/ Customers/ Stocks/ Invoices/
Services/
  LookupService.cs         Ülke, il, ilçe, vergi dairesi, birim, para birimi aramaları
  CustomerService.cs       Cari kart işlemleri
  StockService.cs          Stok kartı işlemleri
  InvoiceService.cs        Fatura hazırlama ve kaydetme (API çağrıları)
  InvoiceCalculator.cs     Fatura satır ve toplam hesapları (API çağrısı yapmaz)
```

Her DTO'nun açıklamasında Swagger'daki karşılığının adı yazar; alan adları API'deki adlarla aynıdır.

## API'nin genel mantığı

**İki servis var.** Token Sales API'den alınır, ön muhasebe işlemleri Pre-Accounting API'de yapılır. Token ikinci serviste `Authorization: Bearer {token}` başlığıyla gönderilir.

**Her yanıt aynı zarfla gelir:**

```json
{ "isSuccess": true, "data": "...", "errors": null }
```

Doğrulama hatalarında HTTP kodu çoğunlukla yine `200` olur; başarı `isSuccess` alanından anlaşılır. Hata durumunda `errors` listesindeki `errorCode`, `errorMessage` ve `propertyNames` hangi alanın neden reddedildiğini söyler. Sales API alan adlarını PascalCase, Pre-Accounting API camelCase döndürür.

**Liste ve arama uçlarının istek kalıbı ortaktır:**

```json
{ "data": { "...filtre..." }, "pagingOptions": { "enablePaging": true, "pageNumber": 1, "pageSize": 50 } }
```

**Ekle ve güncelle aynı uçtur (Upsert).** `id: 0` yeni kayıt, `id > 0` güncelleme demektir. Yanıtın `data` alanı kaydın id'sidir.

**Sunucu, istekte gelen nesneyi olduğu gibi saklar.** Bir arama sonucunu (ör. para birimi, vergi kalemi, cari) başka bir isteğin içine koyarken eksik alanla gönderirseniz o alanlar boş kaydedilir. Bu yüzden DTO'larda tanımlı olmayan alanlar `ServerObjectDto` üzerinden korunur ve aynen geri gönderilir.

## Kullanılan uçlar

### Oturum (Sales API)

| Uç | Amaç |
|---|---|
| `POST /api/salesanduser/token/authpass` | Giriş. Başlık: `X-Api-Key` |
| `POST /api/salesanduser/Token/authtoken` | Token yenileme |
| `POST /api/salesanduser/Token/LogOut` | Çıkış |

### Cari (Pre-Accounting API)

| Uç | Amaç |
|---|---|
| `POST /api/CustomerSupplier/Search` | Anahtar kelimeyle arama (benzer kayıtları da getirir) |
| `POST /api/CustomerSupplier/GetByCode` | Koda göre kart. Gövde yalnızca `"KOD"` |
| `POST /api/CustomerSupplier/GetById` | Id'ye göre kart. Gövde yalnızca `15` |
| `POST /api/CustomerSupplier/SearchDetail` | Carinin fatura üzerinde taşınan özeti |
| `POST /api/CustomerSupplier/Upsert` | Ekle / güncelle |
| `POST /api/Region/SearchCountry` · `SearchCity` · `SearchCounty` | Ülke, il, ilçe |
| `POST /api/TaxOffice/Search` | Vergi dairesi |

### Stok

| Uç | Amaç |
|---|---|
| `GET /api/Stock/GetByCode/{kod}` | Koda göre kart |
| `POST /api/Stock/Upsert` | Ekle / güncelle |
| `POST /api/Stock/DetailedSearch` | Stokun belge için birim, fiyat ve vergi bilgileri |
| `POST /api/GeneralSetting/GetCompanySelectedUnits` | Ölçü birimleri |
| `POST /api/GeneralSetting/GetCompanySelectedCurrencies` | Para birimleri |
| `POST /api/TaxLegislation/GetAllTaxItems` | Vergi kalemleri (KDV, ÖTV…) |
| `POST /api/TaxLegislation/SearchTaxItemDetail` | Vergi oranları (%1, %10, %20…) |
| `POST /api/StockPriceKind/Search` | Fiyat adları (Genel Alış, Genel Satış) |

### Fatura

Adlarda geçen kısaltmalar: **BSDB** belgenin cinsi (Fatura = `S4100`), **BSDT** alt türü (Genel Tip, İade, İhracat…), **BSDI** belgenin kendisi.

| Uç | Amaç |
|---|---|
| `POST /api/BSDT/GetAllByBSDBCode` | Belge cinsinin alt türleri |
| `POST /api/BSDI/GetNewBsdi` | Boş fatura şablonu |
| `POST /api/BSDI/GetBsdOperatorType` | Cari e-Fatura mükellefi mi, e-Arşiv mi? |
| `POST /api/BSDI/GetAllScenarioByBsdtCodeAndOperatorTypeId` | Uygulayıcıya göre senaryolar |
| `POST /api/BSDSerial/GetSerialsByBsdb` | Kullanılabilir seriler |
| `POST /api/BSDSerial/GetSerialNumberNext` | Sıradaki seri numarası |
| `POST /api/BSDI/UpsertBsdi` | Faturayı kaydet |
| `POST /api/BSDI/GetBsdi` | Kaydedilen faturayı oku |

Fatura sıfırdan kurulmaz: `GetNewBsdi` ile şablon alınır, cari / seri / satırlar / toplamlar eklenir ve `UpsertBsdi` ile kaydedilir.

## Dikkat edilecek noktalar

Swagger tanımında açıkça yazmayan, deneyerek ve sunucu kodu incelenerek bulunan davranışlar:

- **Entegrasyon için API kullanıcısı gerekir.** Bu kullanıcı bir entegrasyon tanımına bağlıdır ve girişte o tanımın anahtarını `X-Api-Key` başlığında gönderir. Kullanıcının çalışacağı şirkete davet edilmiş olması gerekir. Anahtar başka bir kullanıcıyla gönderilirse `USER_NOT_FOUND` döner.
- **Standart (arayüz) kullanıcısıyla konsoldan girilemez.** Anahtarsız girişte sunucu giriş ekranındaki robot doğrulamasının yanıtını ister, yoksa `INVALID_CAPTCHA` döner.
- **Stok durumu `stockAdditionalInfo.status` alanından okunur.** Bu bölüm gönderilmezse kart pasif açılır ve faturada seçilemez.
- **Stok tipini doğru seçin.** `0` "Yok" demektir; ürün `1` (Envanter), hizmet `2`, masraf `3`'tür. Belgede kullanılan stokun tipi sonradan değiştirilemez.
- **İç içe nesneler zorunludur.** Stokta `measurementId`, `taxItemId`, `stockPriceKindId` yetmez; `measurement`, `taxItem`, `taxItemDetail`, `stockPriceKind` nesneleri de gönderilmelidir. Caride vergi dairesi yalnızca id ile gönderilirse kart kaydedilir ama vergi dairesi boş kalır.
- **Cari adresinde** mahalle, cadde ve sokaktan en az biri ile bina no zorunludur. Varsayılan tahsilat planı da zorunludur.
- **VKN kontrol hanesi doğrulanır.** Rastgele 10 hane kabul edilmez.
- **Faturadaki ayar bloğu şablondan aynen geri gönderilmelidir.** `settings` ve `stockGridColumns` belgeyle birlikte saklanır; eksik gönderilen ayar (ör. miktar ondalık basamağı, ödeme tipi) sıfırlanmış kaydedilir.
- **`generalInfo.ettn` şablondan gelir.** Boş gönderilirse "ETTN kullanılmıştır" hatası alınır.
- **Fatura tutarlarını sunucu hesaplamaz, yalnızca doğrular.** Satır, vergi ve genel toplamlar tutarlı gönderilmelidir; toplam alanlarının tamamı dolu olmalıdır.
- **Uygulayıcıyı cari belirler.** Cari e-Fatura mükellefiyse e-Fatura, değilse e-Arşiv; seri ve senaryo da buna göre değişir.
- **Seri numarası sunucudan alınır.** İlk dört hanesi belge yılıdır.
- **`UpsertBsdi` yalnızca kaydeder.** Gönderen uç `UpsertBsdiAndSend`'dir; bu örnekte kullanılmamıştır.
- **Enum değerlerinin adları Swagger'da yok.** `Models/Enums.cs` içindeki ad-değer eşleşmeleri sunucu kaynak kodundaki tanımlardan alınmıştır.

## Örneğin kapsamı

Basit tutmak için şunlar kapsam dışıdır: gerçek kişi (şahıs) cari kartı, döviz cinsinden fatura, iskonto ve ek masraflar, tevkifat / stopaj / ÖTV gibi KDV dışı vergiler, çok birimli stoklar, faturanın e-belge olarak gönderilmesi.

## API tanımları

Uç noktaların tam listesi ve modelleri test ortamının Swagger sayfalarında:

- `{SalesApiUrl}/swagger/index.html`
- `{PreAccountingApiUrl}/swagger/index.html`
