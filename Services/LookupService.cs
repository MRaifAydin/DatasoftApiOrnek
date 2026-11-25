using System.Globalization;
using DatasoftApiSample.Api;
using DatasoftApiSample.Models.Common;

namespace DatasoftApiSample.Services;

/// <summary>
/// Kartlarda id ile gönderilen tanımları (ülke, il, ilçe, vergi dairesi, ölçü birimi, para
/// birimi) adlarından bulur.
/// </summary>
public sealed class LookupService(DatasoftApiClient api)
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<RegionDto> FindCountryAsync(string name)
    {
        var filter = new RegionSearchFilter { SearchText = name };
        List<RegionDto> countries = await api.PostAsync<List<RegionDto>>("/api/Region/SearchCountry", PagedRequest.FirstPage(filter, 10));

        return PickByName(countries, name, country => country.Name, "Ülke");
    }

    public async Task<RegionDto> FindCityAsync(long countryId, string name)
    {
        var filter = new RegionSearchFilter { CountryId = countryId, SearchText = name };
        List<RegionDto> cities = await api.PostAsync<List<RegionDto>>("/api/Region/SearchCity", PagedRequest.FirstPage(filter, 10));

        return PickByName(cities, name, city => city.Name, "İl");
    }

    public async Task<RegionDto> FindCountyAsync(long cityId, string name)
    {
        var filter = new RegionSearchFilter { CityId = cityId, SearchText = name };
        List<RegionDto> counties = await api.PostAsync<List<RegionDto>>("/api/Region/SearchCounty", PagedRequest.FirstPage(filter, 10));

        return PickByName(counties, name, county => county.Name, "İlçe");
    }

    public async Task<TaxOfficeDto> FindTaxOfficeAsync(string name)
    {
        var filter = new TaxOfficeSearchFilter { Name = name };
        List<TaxOfficeDto> taxOffices = await api.PostAsync<List<TaxOfficeDto>>("/api/TaxOffice/Search", PagedRequest.FirstPage(filter, 10));

        return PickByName(taxOffices, name, taxOffice => taxOffice.Name, "Vergi dairesi");
    }

    /// <summary>Şirkette seçili ölçü birimleri arasında arar.</summary>
    public async Task<MeasurementDto> FindUnitAsync(string name)
    {
        var filter = new UnitSearchFilter { Search = name };
        List<MeasurementDto> units = await api.PostAsync<List<MeasurementDto>>("/api/GeneralSetting/GetCompanySelectedUnits", PagedRequest.All(filter));

        return PickByName(units, name, unit => unit.Name, "Ölçü birimi");
    }

    /// <summary>Şirkette seçili para birimleri arasında ISO koduyla (ör. "TRY") arar.</summary>
    public async Task<CurrencyDto> FindCurrencyAsync(string code)
    {
        List<CurrencyDto> currencies = await api.PostAsync<List<CurrencyDto>>("/api/GeneralSetting/GetCompanySelectedCurrencies", PagedRequest.All(new LanguageFilter()));

        return currencies.FirstOrDefault(currency => IsSameText(currency.Code, code))
               ?? throw new InvalidOperationException($"Para birimi şirkette seçili değil: {code}");
    }

    /// <summary>
    /// Arama uçları "içeren" kayıtları döndürür; adı tam eşleşen varsa o, yoksa ilk sonuç seçilir.
    /// </summary>
    private static TItem PickByName<TItem>(List<TItem> items, string name, Func<TItem, string?> getName, string description)
    {
        if (items.Count == 0)
            throw new InvalidOperationException($"{description} bulunamadı: \"{name}\". Ayar dosyasındaki değeri kontrol edin.");

        return items.FirstOrDefault(item => IsSameText(getName(item), name)) ?? items[0];
    }

    /// <summary>Türkçe kurallarıyla büyük/küçük harf duyarsız karşılaştırır ("Çankaya" = "ÇANKAYA").</summary>
    internal static bool IsSameText(string? left, string? right)
        => string.Compare(left?.Trim(), right?.Trim(), Turkish, CompareOptions.IgnoreCase) == 0;
}
