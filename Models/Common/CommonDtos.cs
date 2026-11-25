namespace DatasoftApiSample.Models.Common;

/// <summary>Para birimi. Swagger: CurrencyDto</summary>
public sealed class CurrencyDto : ServerObjectDto
{
    public long Id { get; set; }

    /// <summary>ISO kodu; ör. "TRY".</summary>
    public string? Code { get; set; }

    /// <summary>Tutarların yuvarlanacağı ondalık basamak sayısı.</summary>
    public int AmountNumberOfDigits { get; set; }
}

/// <summary>Ölçü birimi (Adet, Kilogram...). Swagger: MeasurementDto</summary>
public sealed class MeasurementDto : ServerObjectDto
{
    public long Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>Vergi dairesi. Swagger: TaxOfficeDto</summary>
public sealed class TaxOfficeDto
{
    public long Id { get; set; }
    public bool IsSystemDefined { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// Ülke, il veya ilçe. Arama sonuçlarında ve adres içinde aynı biçimde kullanılır;
/// adres kaydederken yalnızca <see cref="Id"/> yeterlidir.
/// </summary>
public sealed class RegionDto : ServerObjectDto
{
    public long Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>Adres. Swagger: AddressDto</summary>
public sealed class AddressDto : ServerObjectDto
{
    /// <summary>Yeni adreste boş bırakılır.</summary>
    public long? Id { get; set; }

    /// <summary>Bir kartta tam olarak bir varsayılan adres bulunmalıdır.</summary>
    public bool IsDefault { get; set; }

    public string? AddressName { get; set; }
    public string? FullAddress { get; set; }
    public RegionDto? Country { get; set; }
    public RegionDto? City { get; set; }
    public RegionDto? County { get; set; }

    // Mahalle, cadde ve sokaktan en az biri dolu olmalıdır.
    public string? Neighborhood { get; set; }
    public string? Avenue { get; set; }
    public string? Street { get; set; }

    /// <summary>Zorunlu.</summary>
    public string? BuildingNo { get; set; }

    public string? PostCode { get; set; }
}

/// <summary>İletişim bilgisi. Swagger: ContactDto</summary>
public sealed class ContactDto : ServerObjectDto
{
    public long? Id { get; set; }
    public bool IsDefault { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
