namespace DatasoftApiSample.Models.Common;

// Tanım (lookup) aramalarının filtre nesneleri. Hepsi PagedRequest içinde "data" olarak gönderilir.

/// <summary>Ülke / il / ilçe araması. İl için CountryId, ilçe için CityId doldurulur.</summary>
public sealed class RegionSearchFilter
{
    public long LanguageId { get; set; } = Languages.Turkish;
    public long? CountryId { get; set; }
    public long? CityId { get; set; }
    public string? SearchText { get; set; }
}

/// <summary>Vergi dairesi araması.</summary>
public sealed class TaxOfficeSearchFilter
{
    public string? Name { get; set; }
}

/// <summary>Ölçü birimi araması.</summary>
public sealed class UnitSearchFilter
{
    public long LanguageId { get; set; } = Languages.Turkish;
    public string? Search { get; set; }
}

/// <summary>Yalnızca dil isteyen listeler (ör. para birimleri).</summary>
public sealed class LanguageFilter
{
    public long LanguageId { get; set; } = Languages.Turkish;
}

/// <summary>Arama uçlarının istediği dil id'leri.</summary>
public static class Languages
{
    public const long Turkish = 1;
}
