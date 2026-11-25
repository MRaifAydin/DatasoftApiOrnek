using DatasoftApiSample.Models.Common;

namespace DatasoftApiSample.Models.Customers;

/// <summary>
/// Cari kart; hem kaydederken gönderilir hem okurken döner. Swagger: CustomerSupplierCardDto
/// </summary>
public sealed class CustomerCardDto
{
    /// <summary>Yeni kayıtta 0; güncellemede kartın id'si.</summary>
    public long Id { get; set; }

    public CustomerGeneralInformationDto GeneralInformation { get; set; } = new();

    /// <summary>En az bir adres ve tam olarak bir varsayılan adres zorunludur.</summary>
    public List<AddressDto> Addresses { get; set; } = [];

    public List<ContactDto> Contacts { get; set; } = [];

    /// <summary>Zorunlu.</summary>
    public PaymentPlanDto? DefaultCollectionPlan { get; set; }

    public PaymentPlanDto? DefaultPaymentPlan { get; set; }
}

/// <summary>Cari kartın genel bilgileri. Swagger: CSGeneralInformationDto</summary>
public sealed class CustomerGeneralInformationDto
{
    /// <summary>Benzersiz olmalıdır.</summary>
    public string? Code { get; set; }

    public CustomerSupplierType Type { get; set; }
    public RecordStatus Status { get; set; }
    public LegalStructure LegalStructure { get; set; }
    public CustomerOrigin Origin { get; set; }
    public PublicPrivate PublicPrivate { get; set; }
    public PaymentCollectionMethod PaymentCollectionMethod { get; set; }

    /// <summary>Ünvan; tüzel kişide doldurulur.</summary>
    public string? Title { get; set; }

    /// <summary>Ad; gerçek kişide doldurulur.</summary>
    public string? Name { get; set; }

    /// <summary>Soyad; gerçek kişide doldurulur.</summary>
    public string? Surname { get; set; }

    /// <summary>VKN; tüzel kişide doldurulur. Kontrol hanesi sunucuda doğrulanır.</summary>
    public string? TaxNumber { get; set; }

    /// <summary>TCKN; gerçek kişide doldurulur.</summary>
    public string? IdentityNumber { get; set; }

    /// <summary>
    /// Vergi dairesi araması sonucundaki nesnenin tamamı konmalıdır;
    /// yalnızca id gönderilirse kart kaydedilir ama vergi dairesi boş kalır.
    /// </summary>
    public TaxOfficeDto? TaxOffice { get; set; }
}

/// <summary>Varsayılan tahsilat / ödeme planı. Swagger: CSDefaultCollectionPlanDto</summary>
public sealed class PaymentPlanDto
{
    public BillingType? BillingType { get; set; }
}

/// <summary>
/// Carinin özet hali. Arama uçları (Search, SearchDetail) bu biçimde döndürür; fatura
/// kaydedilirken de "customer" alanına bu nesne aynen konur. Swagger: UpsertBSDCustomerDto
/// </summary>
public sealed class CustomerSummaryDto : ServerObjectDto
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string? Title { get; set; }

    /// <summary>Carinin kimlik numarası: tüzel kişide VKN, gerçek kişide TCKN.</summary>
    public string? TcknVkn { get; set; }
}

/// <summary>CustomerSupplier/SearchDetail filtresi. Swagger: CustomerSupplierSearchDetailRequestDto</summary>
public sealed class CustomerDetailSearchFilter
{
    public string? Code { get; set; }
}
