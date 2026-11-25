using DatasoftApiSample.Models.Common;

namespace DatasoftApiSample.Models.Stocks;

// Vergi iki seviyelidir:
//   vergi KALEMİ  (TaxItem)       -> KDV, ÖTV, Tevkifat...
//   vergi DETAYI  (TaxItemDetail) -> o kalemin bir oranı: %1, %10, %20...
// Her detayın, belge tarihine göre geçerli olan bir KOŞULU (TaxCondition) vardır; hesaplama
// için gereken oran ve toplama etkisi koşulda yazar.

/// <summary>Vergi kalemi. Swagger: TaxItemDto</summary>
public sealed class TaxItemDto : ServerObjectDto
{
    public long Id { get; set; }

    /// <summary>Resmi vergi kodu; KDV için "0015".</summary>
    public string? Code { get; set; }

    public string? Name { get; set; }
    public TaxDefinitionSource Definition { get; set; }

    /// <summary>Verginin hesaplanma sırası.</summary>
    public int ApplyOrder { get; set; }

    public bool UpperLimitControl { get; set; }
}

/// <summary>Vergi kaleminin bir oranı. Swagger: TaxItemDetailDto</summary>
public sealed class TaxItemDetailDto : ServerObjectDto
{
    public long Id { get; set; }

    /// <summary>Görünen ad; ör. "%20".</summary>
    public string? GroupName { get; set; }

    /// <summary>Oran; ör. 20.</summary>
    public decimal? Result { get; set; }
}

/// <summary>Vergi oranının geçerli olduğu koşul. Swagger: TaxConditionDto</summary>
public sealed class TaxConditionDto : ServerObjectDto
{
    public long Id { get; set; }
    public TaxResultType ResultType { get; set; }

    /// <summary>ResultType = Rate ise yüzde oranı.</summary>
    public decimal Result { get; set; }

    public TotalEffect TotalAmountOnEffect { get; set; }
}

/// <summary>TaxLegislation/SearchTaxItemDetail filtresi.</summary>
public sealed class TaxItemDetailSearchFilter
{
    /// <summary>Adına rağmen vergi KALEMİNİN id'sidir.</summary>
    public long TaxGroupId { get; set; }

    public TradeDirection BuySellType { get; set; }
    public bool OrderByGroupName { get; set; } = true;
}
