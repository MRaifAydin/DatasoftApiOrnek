using System.Text.Json;
using DatasoftApiSample.Models.Common;
using DatasoftApiSample.Models.Customers;
using DatasoftApiSample.Models.Stocks;

namespace DatasoftApiSample.Models.Invoices;

// API'de fatura, irsaliye, sipariş gibi tüm alış-satış belgeleri ortak bir modelle tutulur.
// Uç adlarında ve alanlarda şu kısaltmalar geçer:
//   BSDB = belgenin cinsi   (Fatura = "S4100", İrsaliye = "S3100"...)
//   BSDT = belgenin alt türü ("Fatura (Genel Tip)", İade, İhracat...)
//   BSDI = belgenin kendisi  (kaydedilen fatura)

/// <summary>Kaydedilecek fatura; BSDI/UpsertBsdi gövdesi. Swagger: BsdUpsertDto</summary>
public class InvoiceDto
{
    /// <summary>Yeni belgede 0; güncellemede belgenin id'si.</summary>
    public long Id { get; set; }

    /// <summary>Belge türü ayarları. Şablondan geldiği gibi geri gönderilir.</summary>
    public InvoiceSettingsDto? Settings { get; set; }

    /// <summary>
    /// Arayüzdeki satır tablosunun sütun ayarları. Zorunlu alandır ama içeriği bu örneği
    /// ilgilendirmez; şablondan geldiği gibi geri gönderilir.
    /// </summary>
    public JsonElement StockGridColumns { get; set; }

    public InvoiceGeneralInfoDto? GeneralInfo { get; set; }

    /// <summary>Belgenin e-belge uygulayıcısı (e-Fatura / e-Arşiv).</summary>
    public OperatorTypeDto? OperatorType { get; set; }

    /// <summary>Belgede kullanılabilen para birimleri; sunucu yuvarlama basamaklarını buradan okur.</summary>
    public List<CurrencyDto>? CurrencyInformations { get; set; }

    public CustomerSummaryDto? Customer { get; set; }
    public AddressDto? CustomerAddress { get; set; }
    public ContactDto? CustomerContact { get; set; }

    /// <summary>Fatura satırları.</summary>
    public List<InvoiceLineDto> Stocks { get; set; } = [];

    public InvoiceTotalDto? Total { get; set; }

    /// <summary>Belge altındaki vergi tablosu: her vergi oranı için bir satır.</summary>
    public List<InvoiceTaxTotalDto> TotalTaxResultList { get; set; } = [];

    public List<string>? AdditionalDescriptions { get; set; }
}

/// <summary>
/// BSDI/GetNewBsdi yanıtı: varsayılanları doldurulmuş boş fatura ve satırları hesaplamak için
/// gereken vergi tabloları. Tablolar yalnızca hesaplamada kullanılır, kaydederken gönderilmez.
/// </summary>
public sealed class InvoiceTemplateDto : InvoiceDto
{
    /// <summary>Bu belge türünde kullanılabilen vergi kalemleri.</summary>
    public List<TaxItemDto> StockGridTaxItems { get; set; } = [];

    /// <summary>Vergi oranları ve belge tarihinde geçerli koşulları.</summary>
    public List<TaxDetailWithConditionsDto> TaxDetailsWithConditions { get; set; } = [];
}

/// <summary>
/// Belge türü ayarları. Sunucu bu bloğu belgeyle birlikte saklar; eksik gönderilen ayar
/// (ör. miktar ondalık basamağı, ödeme tipi) sıfırlanmış olarak kaydedilir. Bu yüzden
/// şablondaki blok değiştirilmeden geri gönderilir. Swagger: TenantBSDISettingsDto
/// </summary>
public sealed class InvoiceSettingsDto : ServerObjectDto
{
    public long BsdtId { get; set; }
    public bool BsdtIsSystem { get; set; }
    public string? BsdtCode { get; set; }
    public long BsdbId { get; set; }
    public bool BsdbIsSystem { get; set; }

    /// <summary>Belge türünün adı; ör. "Fatura (Genel Tip)".</summary>
    public string? Name { get; set; }
}

/// <summary>Faturanın başlık bilgileri. Swagger: BSDGeneralInfoDto</summary>
public sealed class InvoiceGeneralInfoDto : ServerObjectDto
{
    public SerialRefDto? Serial { get; set; }

    /// <summary>Sunucudan alınan sıradaki numara; ilk dört hanesi belge yılıdır.</summary>
    public string? SerialNumber { get; set; }

    public ScenarioDto? Scenario { get; set; }
    public ETypeDto? EType { get; set; }

    /// <summary>false: satış (giden) belgesi, true: alış (gelen) belgesi.</summary>
    public bool IsIncoming { get; set; }

    public DateTime Date { get; set; }

    /// <summary>Belgenin para birimi.</summary>
    public CurrencyDto? Currency { get; set; }

    /// <summary>Şirketin para birimi.</summary>
    public CurrencyDto? DefaultCurrency { get; set; }

    /// <summary>Belge para biriminin şirket para birimine kuru.</summary>
    public decimal CurrencyValue { get; set; } = 1;
}

/// <summary>Fatura üzerindeki seri bilgisi.</summary>
public sealed class SerialRefDto
{
    public long Id { get; set; }
    public string? Code { get; set; }
}

/// <summary>e-Belge senaryosu ("Temel Fatura", "Ticari Fatura", "e-Arşiv Fatura"...).</summary>
public sealed class ScenarioDto
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }

    /// <summary>Senaryonun desteklediği e-belge tipleri; yalnızca senaryo listesinde dolu gelir.</summary>
    public List<ETypeDto>? ETypes { get; set; }
}

/// <summary>e-Belge tipi ("Satış", "İade", "Tevkifat"...).</summary>
public sealed class ETypeDto
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public long ScenarioId { get; set; }
}

/// <summary>e-Belge uygulayıcısı. Swagger: BsdOperatorInfoDto</summary>
public sealed class OperatorTypeDto : ServerObjectDto
{
    /// <summary>1: e-Fatura, 2: e-Arşiv.</summary>
    public long? OperatorTypeId { get; set; }

    public bool HasGovernmentConnection { get; set; }
    public string? Name { get; set; }
}

/// <summary>Fatura satırı. Swagger: BSDStockGridDto</summary>
public sealed class InvoiceLineDto
{
    public int RowOrder { get; set; }
    public StockType? StockType { get; set; }
    public InvoiceLineStockDto? Stock { get; set; }
    public string? Description { get; set; }

    // --- Miktar ---
    public decimal Quantity { get; set; }
    public string? StockUnitName { get; set; }

    /// <summary>Miktarın birimlere dağılımı; tek birimli satırda tek eleman olur.</summary>
    public List<InvoiceLineUnitDto> StockUnits { get; set; } = [];

    // --- Fiyat ---
    // "TL" ile biten alanlar tutarın şirket para birimindeki karşılığıdır (tutar x kur).

    /// <summary>Birim fiyat.</summary>
    public decimal Price { get; set; }
    public decimal PriceTL { get; set; }

    public decimal ConvertedPrice { get; set; }
    public decimal ConvertedPriceTL { get; set; }

    /// <summary>Birim fiyatın vergiyi içerip içermediği.</summary>
    public TaxInclusion TaxIncludingExcluding { get; set; }

    public long CurrencyId { get; set; }
    public decimal CurrencyExchangeRate { get; set; } = 1;

    // --- Satır tutarları ---

    /// <summary>Miktar x birim fiyat.</summary>
    public decimal TotalPrice { get; set; }
    public decimal TotalPriceTL { get; set; }

    public decimal DiscountPrice { get; set; }
    public decimal DiscountPriceTL { get; set; }

    /// <summary>İskonto sonrası tutar (vergi matrahı).</summary>
    public decimal GrossPrice { get; set; }
    public decimal GrossPriceTL { get; set; }

    public decimal Addition { get; set; }
    public decimal AdditionTL { get; set; }
    public decimal Deduction { get; set; }
    public decimal DeductionTL { get; set; }
    public decimal Shipping { get; set; }
    public decimal ShippingTL { get; set; }

    // Vergiler dahil satır tutarı; API aynı değeri üç ayrı alanda bekler.
    public decimal TaxIncludedPrice { get; set; }
    public decimal TaxIncludedPriceTL { get; set; }
    public decimal AddedTaxIncludedPrice { get; set; }
    public decimal AddedTaxIncludedPriceTL { get; set; }
    public decimal TaxesIncludedPrice { get; set; }
    public decimal TaxesIncludedPriceTL { get; set; }

    public List<InvoiceLineTaxDto> Taxes { get; set; } = [];
}

/// <summary>Satırdaki stokun özeti.</summary>
public sealed class InvoiceLineStockDto
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public StockType? Type { get; set; }
    public long? BarcodeId { get; set; }
    public StockSubType StockSubType { get; set; }
}

/// <summary>Satır miktarının bir birimdeki kısmı.</summary>
public sealed class InvoiceLineUnitDto
{
    public int RowOrder { get; set; }
    public decimal Quantity { get; set; }
    public InvoiceLineUnitRefDto? StockUnit { get; set; }
}

public sealed class InvoiceLineUnitRefDto
{
    /// <summary>Ölçü biriminin id'si.</summary>
    public long Id { get; set; }

    public bool IsPrimary { get; set; }
}

/// <summary>Satıra uygulanan bir vergi.</summary>
public sealed class InvoiceLineTaxDto
{
    /// <summary>Vergi kalemi (KDV).</summary>
    public TaxItemDto? Tax { get; set; }

    /// <summary>Vergi oranı (%20).</summary>
    public InvoiceLineTaxDetailDto? TaxDetail { get; set; }

    /// <summary>Oranın geçerli olduğu koşul.</summary>
    public TaxConditionDto? TaxCondition { get; set; }

    public decimal TaxPrice { get; set; }
    public decimal TaxPriceTL { get; set; }
    public bool IsExceptional { get; set; }
}

/// <summary>Satırdaki vergi oranı tanımı.</summary>
public class InvoiceLineTaxDetailDto
{
    public long Id { get; set; }
    public string? GroupName { get; set; }
    public string? Code { get; set; }
    public string? Operation { get; set; }
    public TaxDefinitionSource Definition { get; set; }
    public long TaxItemId { get; set; }
    public int TaxItemApplyOrder { get; set; }
}

/// <summary>Şablondaki vergi oranı: tanım ve belge tarihinde geçerli koşulları.</summary>
public sealed class TaxDetailWithConditionsDto : InvoiceLineTaxDetailDto
{
    public List<TaxConditionDto> TaxConditions { get; set; } = [];
}

/// <summary>Belge alt toplamları. Swagger: BSDTotalDto</summary>
public sealed class InvoiceTotalDto
{
    /// <summary>Mal/hizmet toplamı (satırların GrossPrice toplamı).</summary>
    public decimal GrossTotal { get; set; }
    public decimal GrossTotalTL { get; set; }

    /// <summary>Satırların TotalPrice toplamı.</summary>
    public decimal Total { get; set; }
    public decimal TotalTL { get; set; }

    public decimal DiscountTotal { get; set; }
    public decimal DiscountTotalTL { get; set; }
    public decimal Discount { get; set; }
    public decimal DiscountTL { get; set; }
    public decimal Addition { get; set; }
    public decimal AdditionTL { get; set; }
    public decimal Deduction { get; set; }
    public decimal DeductionTL { get; set; }
    public decimal Shipping { get; set; }
    public decimal ShippingTL { get; set; }

    /// <summary>Toplamı artıran vergiler (KDV).</summary>
    public decimal TaxAddition { get; set; }
    public decimal TaxAdditionTL { get; set; }

    /// <summary>Toplamı azaltan vergiler (tevkifat, stopaj).</summary>
    public decimal TaxDeduction { get; set; }
    public decimal TaxDeductionTL { get; set; }

    public decimal TaxExceptional { get; set; }
    public decimal TaxExceptionalTL { get; set; }
    public decimal NetTaxTotal { get; set; }
    public decimal NetTaxTotalTL { get; set; }

    /// <summary>Ödenecek tutar = GrossTotal + TaxAddition + Addition + Shipping - TaxDeduction - Deduction - TaxExceptional.</summary>
    public decimal GeneralTotal { get; set; }
    public decimal GeneralTotalTL { get; set; }
}

/// <summary>Vergi tablosunun bir satırı. Swagger: BSDTotalTaxResultDto</summary>
public sealed class InvoiceTaxTotalDto
{
    public long TaxItemId { get; set; }
    public string? TaxItemName { get; set; }
    public string? Code { get; set; }
    public bool UpperLimitControl { get; set; }
    public long TaxItemDetailId { get; set; }

    public TaxResultType ValueType { get; set; }

    /// <summary>Oran; ör. 20.</summary>
    public decimal Value { get; set; }

    public TotalEffect TotalAmountOnEffect { get; set; }

    /// <summary>Verginin hesaplandığı matrah.</summary>
    public decimal MatrahAmount { get; set; }
    public decimal MatrahAmountTL { get; set; }

    /// <summary>Hesaplanan vergi.</summary>
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountTL { get; set; }

    public decimal TaxExceptionalAmount { get; set; }
    public decimal TaxExceptionalAmountTL { get; set; }
    public decimal NetTaxAmount { get; set; }
    public decimal NetTaxAmountTL { get; set; }
}

/// <summary>Kaydedilen faturanın geri okunan özeti (BSDI/GetBsdi yanıtından gereken alanlar).</summary>
public sealed class SavedInvoiceDto
{
    public long Id { get; set; }
    public InvoiceGeneralInfoDto? GeneralInfo { get; set; }
    public SavedInvoiceTotalDto? Total { get; set; }
}

public sealed class SavedInvoiceTotalDto
{
    public decimal GeneralTotal { get; set; }
}
