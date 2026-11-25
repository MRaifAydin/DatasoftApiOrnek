using DatasoftApiSample.Models.Common;

namespace DatasoftApiSample.Models.Stocks;

/// <summary>
/// Stok kartı; hem kaydederken gönderilir hem okurken döner. Swagger: StockDto
/// </summary>
public sealed class StockCardDto
{
    /// <summary>Yeni kayıtta 0; güncellemede kartın id'si.</summary>
    public long Id { get; set; }

    public StockGeneralInfoDto GeneralInfo { get; set; } = new();

    /// <summary>
    /// Stokun aktif/pasif durumunu asıl belirleyen bölümdür. Gönderilmezse kart pasif açılır
    /// ve fatura satırında seçilemez.
    /// </summary>
    public StockAdditionalInfoDto? StockAdditionalInfo { get; set; }

    /// <summary>En az bir birim ve tam olarak bir ana birim zorunludur.</summary>
    public List<StockUnitDto> Units { get; set; } = [];

    /// <summary>En az bir vergi satırı zorunludur.</summary>
    public List<StockTaxDto> TaxInfos { get; set; } = [];

    /// <summary>Fiyat satırları. Adı "barcodes" olsa da barkod zorunlu değildir.</summary>
    public List<StockPriceDto> Barcodes { get; set; } = [];
}

/// <summary>Stok kartının genel bilgileri. Swagger: StockGeneralInfoDto</summary>
public sealed class StockGeneralInfoDto
{
    /// <summary>Benzersiz olmalıdır.</summary>
    public string? Code { get; set; }

    public string? Name { get; set; }
    public RecordStatus Status { get; set; }

    /// <summary>Belgede kullanılan stokun tipi sonradan değiştirilemez.</summary>
    public StockType? Type { get; set; }

    public StockInventoryType? StockInventoryType { get; set; }
}

/// <summary>Stok kartının ek bilgileri. Swagger: StockAdditionalInfoDto</summary>
public sealed class StockAdditionalInfoDto
{
    public RecordStatus Status { get; set; }
    public StockSubType StockSubType { get; set; }
}

/// <summary>Stokun kullanabildiği bir ölçü birimi. Swagger: StockUnitInfoDto</summary>
public sealed class StockUnitDto
{
    // Sunucu "...Id" alanının yanında nesnenin kendisini de ister; ikisi birlikte gönderilir.
    public long MeasurementId { get; set; }
    public MeasurementDto? Measurement { get; set; }

    public bool IsPrimary { get; set; }
}

/// <summary>Stokun bir vergisi. Swagger: StockRelationTaxDto</summary>
public sealed class StockTaxDto
{
    public TaxDefinitionSource Definition { get; set; }

    /// <summary>Verginin geçerli olduğu yön (alış, satış veya ikisi).</summary>
    public TradeDirection BuySellType { get; set; }

    public long? TaxItemId { get; set; }

    /// <summary>Zorunlu; vergi adı bu nesneden okunur.</summary>
    public TaxItemDto? TaxItem { get; set; }

    public long? TaxItemDetailId { get; set; }
    public TaxItemDetailDto? TaxItemDetail { get; set; }
    public TaxType TaxType { get; set; }
    public decimal? Ratio { get; set; }
}

/// <summary>Stokun bir fiyat satırı. Swagger: StockBarcodeDto</summary>
public sealed class StockPriceDto
{
    public long MeasurementId { get; set; }
    public MeasurementDto? Measurement { get; set; }

    public decimal? Price { get; set; }

    public long? CurrencyId { get; set; }
    public CurrencyDto? Currency { get; set; }

    /// <summary>Fiyatın yönü (alış fiyatı / satış fiyatı).</summary>
    public TradeDirection PriceType { get; set; }

    public long? StockPriceKindId { get; set; }
    public StockPriceKindDto? StockPriceKind { get; set; }

    public TaxInclusion? TaxIncludedType { get; set; }
    public StockPriceMethod? StockPriceMethod { get; set; }
}

/// <summary>Fiyat adı ("Genel Alış", "Genel Satış"...). Swagger: StockPriceKindDto</summary>
public sealed class StockPriceKindDto : ServerObjectDto
{
    public long Id { get; set; }
    public string? PriceName { get; set; }
    public TradeDirection? PriceType { get; set; }
}

/// <summary>
/// Stokun belge satırında kullanılmak üzere dönen hali: birimleri, fiyatları ve vergileriyle.
/// Stock/DetailedSearch yanıtı.
/// </summary>
public sealed class StockForDocumentDto
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public StockType? Type { get; set; }
    public long? BarcodeId { get; set; }
    public StockSubType StockSubType { get; set; }

    public List<StockForDocumentUnitDto> Units { get; set; } = [];
    public List<StockForDocumentPriceDto> Prices { get; set; } = [];
    public List<StockForDocumentTaxDto> Taxes { get; set; } = [];
}

public sealed class StockForDocumentUnitDto
{
    /// <summary>Ölçü biriminin id'si.</summary>
    public long Id { get; set; }

    public string? Name { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class StockForDocumentPriceDto
{
    public decimal? Price { get; set; }
    public TradeDirection PriceType { get; set; }
    public TaxInclusion? TaxIncludedType { get; set; }
}

public sealed class StockForDocumentTaxDto
{
    public long TaxItemId { get; set; }
    public long TaxItemDetailId { get; set; }
    public TradeDirection BuySellType { get; set; }
}

/// <summary>Stock/DetailedSearch filtresi. Swagger: DetailedSearchGetDto</summary>
public sealed class StockForDocumentSearchFilter
{
    public string? SearchKeyword { get; set; }

    /// <summary>Hangi yönün fiyat ve vergileri dönsün.</summary>
    public TradeDirection BuySell { get; set; }

    public long LanguageId { get; set; } = Languages.Turkish;

    /// <summary>Stokun kullanılacağı belge işlem türü.</summary>
    public long BsdtId { get; set; }

    public bool BsdtIsSystem { get; set; }
}
