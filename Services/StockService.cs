using DatasoftApiSample.Api;
using DatasoftApiSample.Configuration;
using DatasoftApiSample.Models;
using DatasoftApiSample.Models.Common;
using DatasoftApiSample.Models.Invoices;
using DatasoftApiSample.Models.Stocks;

namespace DatasoftApiSample.Services;

/// <summary>Stok kartı işlemleri.</summary>
public sealed class StockService(DatasoftApiClient api, LookupService lookups)
{
    /// <summary>Katma Değer Vergisi'nin resmi vergi kodu.</summary>
    private const string VatTaxCode = "0015";

    /// <summary>Vergi kalemleri ülkeye göre tanımlıdır.</summary>
    private const string TaxCountry = "Türkiye";

    /// <summary>Örnek stokun fiyatının tanımlandığı para birimi.</summary>
    private const string PriceCurrencyCode = "TRY";

    /// <summary>
    /// Koda göre stok kartını getirir; yoksa null döner.
    /// <c>GET /api/Stock/GetByCode/{code}</c>
    /// </summary>
    public Task<StockCardDto?> FindByCodeAsync(string code)
        => api.GetOrDefaultAsync<StockCardDto>("/api/Stock/GetByCode/" + Uri.EscapeDataString(code));

    /// <summary>
    /// Stoku, belge satırında kullanmak için birim, fiyat ve vergileriyle getirir.
    /// Yalnızca aktif stoklar döner.
    /// <c>POST /api/Stock/DetailedSearch</c>
    /// </summary>
    public async Task<StockForDocumentDto> GetForDocumentAsync(string code, DocumentTypeDto documentType)
    {
        var filter = new StockForDocumentSearchFilter
        {
            SearchKeyword = code,
            BuySell = TradeDirection.Sale,
            BsdtId = documentType.BsdtId,
            BsdtIsSystem = documentType.IsBSDTSystemDefined
        };

        List<StockForDocumentDto> stocks = await api.PostAsync<List<StockForDocumentDto>>("/api/Stock/DetailedSearch", PagedRequest.FirstPage(filter));

        // Arama kodu içeren stokları da getirdiği için tam eşleşen ayrıca seçilir.
        return stocks.FirstOrDefault(stock => stock.Code == code)
               ?? throw new InvalidOperationException($"\"{code}\" kodlu stok belge için bulunamadı. Stok pasif olabilir.");
    }

    /// <summary>
    /// Tek birimli, KDV'li ve satış fiyatı tanımlı bir ürün kartı oluşturur; id'sini döndürür.
    /// <c>POST /api/Stock/Upsert</c>
    /// </summary>
    public async Task<long> CreateAsync(StockSettings stock)
    {
        MeasurementDto unit = await lookups.FindUnitAsync(stock.Unit);
        CurrencyDto currency = await lookups.FindCurrencyAsync(PriceCurrencyCode);
        TaxItemDto vat = await FindVatAsync();
        TaxItemDetailDto vatRate = await FindTaxRateAsync(vat, stock.VatRate);
        StockPriceKindDto salePriceKind = await FindSalePriceKindAsync();

        var card = new StockCardDto
        {
            GeneralInfo = new StockGeneralInfoDto
            {
                Code = stock.Code,
                Name = stock.Name,
                Status = RecordStatus.Active,
                Type = StockType.Inventory,
                StockInventoryType = StockInventoryType.Goods
            },
            StockAdditionalInfo = new StockAdditionalInfoDto
            {
                Status = RecordStatus.Active,
                StockSubType = StockSubType.Other
            },
            Units =
            [
                new StockUnitDto
                {
                    MeasurementId = unit.Id,
                    Measurement = unit,
                    IsPrimary = true
                }
            ],
            TaxInfos =
            [
                new StockTaxDto
                {
                    Definition = vat.Definition,
                    BuySellType = TradeDirection.Both,   // alışta da satışta da aynı KDV
                    TaxItemId = vat.Id,
                    TaxItem = vat,
                    TaxItemDetailId = vatRate.Id,
                    TaxItemDetail = vatRate,
                    TaxType = TaxType.Normal,
                    Ratio = stock.VatRate
                }
            ],
            Barcodes =
            [
                new StockPriceDto
                {
                    MeasurementId = unit.Id,
                    Measurement = unit,
                    Price = stock.SalePrice,
                    CurrencyId = currency.Id,
                    Currency = currency,
                    PriceType = TradeDirection.Sale,
                    StockPriceKindId = salePriceKind.Id,
                    StockPriceKind = salePriceKind,
                    TaxIncludedType = TaxInclusion.ExcludingTax,
                    StockPriceMethod = StockPriceMethod.UserEntered
                }
            ]
        };

        return await api.PostAsync<long>("/api/Stock/Upsert", card);
    }

    /// <summary><c>POST /api/TaxLegislation/GetAllTaxItems</c> (filtre olarak ülke id'si)</summary>
    private async Task<TaxItemDto> FindVatAsync()
    {
        RegionDto country = await lookups.FindCountryAsync(TaxCountry);
        List<TaxItemDto> taxItems = await api.PostAsync<List<TaxItemDto>>("/api/TaxLegislation/GetAllTaxItems", PagedRequest.All(country.Id));

        return taxItems.FirstOrDefault(taxItem => taxItem.Code == VatTaxCode)
               ?? throw new InvalidOperationException($"KDV vergi kalemi (kod {VatTaxCode}) bulunamadı.");
    }

    /// <summary><c>POST /api/TaxLegislation/SearchTaxItemDetail</c></summary>
    private async Task<TaxItemDetailDto> FindTaxRateAsync(TaxItemDto taxItem, decimal rate)
    {
        var filter = new TaxItemDetailSearchFilter { TaxGroupId = taxItem.Id, BuySellType = TradeDirection.Both };
        List<TaxItemDetailDto> rates = await api.PostAsync<List<TaxItemDetailDto>>("/api/TaxLegislation/SearchTaxItemDetail", PagedRequest.All(filter));

        return rates.FirstOrDefault(candidate => candidate.Result == rate)
               ?? throw new InvalidOperationException(
                   $"%{rate} {taxItem.Name} oranı tanımlı değil. Tanımlı oranlar: {string.Join(", ", rates.Select(candidate => candidate.GroupName))}");
    }

    /// <summary>
    /// Her fiyat satırı bir fiyat adına bağlıdır; satış yönlü ilk fiyat adı ("Genel Satış") seçilir.
    /// <c>POST /api/StockPriceKind/Search</c>
    /// </summary>
    private async Task<StockPriceKindDto> FindSalePriceKindAsync()
    {
        List<StockPriceKindDto> priceKinds = await api.PostAsync<List<StockPriceKindDto>>("/api/StockPriceKind/Search", PagedRequest.All(""));

        return priceKinds.FirstOrDefault(priceKind => priceKind.PriceType == TradeDirection.Sale)
               ?? throw new InvalidOperationException("Satış yönlü fiyat adı tanımlı değil.");
    }
}
