using DatasoftApiSample.Models;
using DatasoftApiSample.Models.Common;
using DatasoftApiSample.Models.Invoices;
using DatasoftApiSample.Models.Stocks;

namespace DatasoftApiSample.Services;

/// <summary>
/// Fatura satırlarını ve toplamlarını hesaplar. API çağrısı yapmaz.
///
/// Sunucu tutarları kendisi hesaplamaz, yalnızca doğrular: satır, vergi ve genel toplamların
/// birbiriyle tutarlı gönderilmesi gerekir. Uyuşmazlıkta kayıt "HAS_INVOICE_TOTAL_ERROR"
/// hatasıyla reddedilir.
///
/// Kapsam: iskonto ve ek masraf yoktur; yalnızca KDV gibi yüzde oranlı ve toplamı artıran
/// vergiler hesaplanır.
/// </summary>
public sealed class InvoiceCalculator
{
    private readonly InvoiceTemplateDto _template;
    private readonly CurrencyDto _currency;
    private readonly decimal _exchangeRate;

    /// <param name="template">Para birimi, kur ve vergi tablolarının okunacağı fatura şablonu.</param>
    public InvoiceCalculator(InvoiceTemplateDto template)
    {
        InvoiceGeneralInfoDto generalInfo = template.GeneralInfo
            ?? throw new InvalidOperationException("Fatura şablonunda genel bilgiler yok.");

        _template = template;
        _currency = generalInfo.Currency ?? throw new InvalidOperationException("Fatura şablonunda para birimi yok.");
        _exchangeRate = generalInfo.CurrencyValue;
    }

    /// <summary>Bir stok için fatura satırı oluşturur.</summary>
    /// <param name="unitPrice">KDV hariç birim fiyat; null ise stok kartındaki satış fiyatı kullanılır.</param>
    public InvoiceLineDto CreateLine(int rowOrder, StockForDocumentDto stock, decimal quantity, decimal? unitPrice)
    {
        StockForDocumentUnitDto unit = stock.Units.FirstOrDefault(candidate => candidate.IsPrimary)
            ?? throw new InvalidOperationException($"\"{stock.Code}\" stokunun ana birimi yok.");

        List<ApplicableTax> taxes = stock.Taxes
            .Where(tax => tax.BuySellType is TradeDirection.Sale or TradeDirection.Both)
            .Select(tax => ResolveTax(stock, tax))
            .ToList();

        decimal price = unitPrice ?? GetSalePriceExcludingTax(stock, taxes);
        decimal lineAmount = Round(quantity * price);

        List<InvoiceLineTaxDto> lineTaxes = taxes.Select(tax => CreateLineTax(tax, lineAmount)).ToList();
        decimal amountIncludingTaxes = lineAmount + lineTaxes.Sum(tax => tax.TaxPrice);

        return new InvoiceLineDto
        {
            RowOrder = rowOrder,
            StockType = stock.Type,
            Stock = new InvoiceLineStockDto
            {
                Id = stock.Id,
                Code = stock.Code,
                Name = stock.Name,
                Type = stock.Type,
                BarcodeId = stock.BarcodeId,
                StockSubType = stock.StockSubType
            },
            Description = "",

            Quantity = quantity,
            StockUnitName = unit.Name,
            StockUnits =
            [
                new InvoiceLineUnitDto
                {
                    RowOrder = 1,
                    Quantity = quantity,
                    StockUnit = new InvoiceLineUnitRefDto { Id = unit.Id, IsPrimary = true }
                }
            ],

            Price = price,
            PriceTL = ToCompanyCurrency(price),
            ConvertedPrice = price,
            ConvertedPriceTL = ToCompanyCurrency(price),
            TaxIncludingExcluding = TaxInclusion.ExcludingTax,
            CurrencyId = _currency.Id,
            CurrencyExchangeRate = _exchangeRate,

            TotalPrice = lineAmount,
            TotalPriceTL = ToCompanyCurrency(lineAmount),
            GrossPrice = lineAmount,
            GrossPriceTL = ToCompanyCurrency(lineAmount),

            TaxIncludedPrice = amountIncludingTaxes,
            TaxIncludedPriceTL = ToCompanyCurrency(amountIncludingTaxes),
            AddedTaxIncludedPrice = amountIncludingTaxes,
            AddedTaxIncludedPriceTL = ToCompanyCurrency(amountIncludingTaxes),
            TaxesIncludedPrice = amountIncludingTaxes,
            TaxesIncludedPriceTL = ToCompanyCurrency(amountIncludingTaxes),

            Taxes = lineTaxes
        };
    }

    /// <summary>Satırlardan belge alt toplamlarını hesaplar.</summary>
    public InvoiceTotalDto CalculateTotal(IReadOnlyCollection<InvoiceLineDto> lines)
    {
        decimal netAmount = lines.Sum(line => line.GrossPrice);
        decimal taxAmount = lines.Sum(line => line.Taxes.Sum(tax => tax.TaxPrice));
        decimal payableAmount = netAmount + taxAmount;

        return new InvoiceTotalDto
        {
            GrossTotal = netAmount,
            GrossTotalTL = ToCompanyCurrency(netAmount),
            Total = netAmount,
            TotalTL = ToCompanyCurrency(netAmount),
            TaxAddition = taxAmount,
            TaxAdditionTL = ToCompanyCurrency(taxAmount),
            NetTaxTotal = taxAmount,
            NetTaxTotalTL = ToCompanyCurrency(taxAmount),
            GeneralTotal = payableAmount,
            GeneralTotalTL = ToCompanyCurrency(payableAmount)
        };
    }

    /// <summary>Satırlardaki vergileri orana göre toplayarak belge altındaki vergi tablosunu oluşturur.</summary>
    public List<InvoiceTaxTotalDto> CalculateTaxTotals(IReadOnlyCollection<InvoiceLineDto> lines)
    {
        var lineTaxes = lines.SelectMany(line => line.Taxes.Select(tax => new { Line = line, Tax = tax }));

        return lineTaxes
            .GroupBy(item => item.Tax.TaxDetail!.Id)
            .Select(group =>
            {
                InvoiceLineTaxDto sample = group.First().Tax;
                decimal taxableAmount = group.Sum(item => item.Line.GrossPrice);
                decimal taxAmount = group.Sum(item => item.Tax.TaxPrice);

                return new InvoiceTaxTotalDto
                {
                    TaxItemId = sample.Tax!.Id,
                    TaxItemName = sample.Tax.Name,
                    Code = sample.Tax.Code,
                    UpperLimitControl = sample.Tax.UpperLimitControl,
                    TaxItemDetailId = sample.TaxDetail!.Id,
                    ValueType = sample.TaxCondition!.ResultType,
                    Value = sample.TaxCondition.Result,
                    TotalAmountOnEffect = sample.TaxCondition.TotalAmountOnEffect,
                    MatrahAmount = taxableAmount,
                    MatrahAmountTL = ToCompanyCurrency(taxableAmount),
                    TotalAmount = taxAmount,
                    TotalAmountTL = ToCompanyCurrency(taxAmount),
                    NetTaxAmount = taxAmount,
                    NetTaxAmountTL = ToCompanyCurrency(taxAmount)
                };
            })
            .ToList();
    }

    /// <summary>
    /// Stok kartındaki vergiyi (kalem + oran id'leri), şablondaki vergi tablolarıyla eşleştirir.
    /// Şablon, belge tarihinde geçerli oran ve koşulları içerir.
    /// </summary>
    private ApplicableTax ResolveTax(StockForDocumentDto stock, StockForDocumentTaxDto stockTax)
    {
        TaxItemDto taxItem = _template.StockGridTaxItems.FirstOrDefault(item => item.Id == stockTax.TaxItemId)
            ?? throw new InvalidOperationException($"\"{stock.Code}\" stokundaki vergi kalemi (id {stockTax.TaxItemId}) bu belge türünde kullanılamıyor.");

        TaxDetailWithConditionsDto rate = _template.TaxDetailsWithConditions.FirstOrDefault(detail => detail.Id == stockTax.TaxItemDetailId)
            ?? throw new InvalidOperationException($"\"{stock.Code}\" stokundaki vergi oranı (id {stockTax.TaxItemDetailId}) belge tarihinde geçerli değil.");

        TaxConditionDto condition = rate.TaxConditions.FirstOrDefault()
            ?? throw new InvalidOperationException($"{taxItem.Name} {rate.GroupName} için geçerli koşul bulunamadı.");

        if (condition.ResultType != TaxResultType.Rate || condition.TotalAmountOnEffect != TotalEffect.Added)
            throw new NotSupportedException(
                $"\"{stock.Code}\" stokunda bu örneğin hesaplamadığı türde bir vergi var ({taxItem.Name}). " +
                "Örnek yalnızca KDV gibi oran bazlı ve toplama eklenen vergileri destekler.");

        return new ApplicableTax(taxItem, rate, condition);
    }

    private InvoiceLineTaxDto CreateLineTax(ApplicableTax tax, decimal taxableAmount)
    {
        decimal taxAmount = Round(taxableAmount * tax.Condition.Result / 100m);

        return new InvoiceLineTaxDto
        {
            Tax = tax.Item,
            TaxDetail = new InvoiceLineTaxDetailDto
            {
                Id = tax.Rate.Id,
                GroupName = tax.Rate.GroupName,
                Code = tax.Rate.Code,
                Operation = tax.Rate.Operation,
                Definition = tax.Rate.Definition,
                TaxItemId = tax.Rate.TaxItemId,
                TaxItemApplyOrder = tax.Item.ApplyOrder
            },
            TaxCondition = tax.Condition,
            TaxPrice = taxAmount,
            TaxPriceTL = ToCompanyCurrency(taxAmount)
        };
    }

    /// <summary>Stok kartındaki satış fiyatını, vergi dahil tanımlandıysa vergisiz hale getirerek döndürür.</summary>
    private static decimal GetSalePriceExcludingTax(StockForDocumentDto stock, List<ApplicableTax> taxes)
    {
        StockForDocumentPriceDto? salePrice = stock.Prices.FirstOrDefault(price => price.PriceType == TradeDirection.Sale);

        if (salePrice?.Price is not decimal price)
            throw new InvalidOperationException($"\"{stock.Code}\" stokunun satış fiyatı tanımlı değil; ayar dosyasında Invoice.UnitPrice verin.");

        if (salePrice.TaxIncludedType != TaxInclusion.IncludingAllTaxes)
            return price;

        decimal totalTaxRate = taxes.Sum(tax => tax.Condition.Result);
        return Math.Round(price / (1 + totalTaxRate / 100m), 6);
    }

    /// <summary>Tutarı belge para biriminin basamak sayısına yuvarlar.</summary>
    private decimal Round(decimal amount)
        => Math.Round(amount, _currency.AmountNumberOfDigits, MidpointRounding.AwayFromZero);

    /// <summary>Tutarın şirket para birimindeki karşılığı ("...TL" alanları).</summary>
    private decimal ToCompanyCurrency(decimal amount) => Round(amount * _exchangeRate);

    /// <summary>Bir stok vergisinin hesaplama için gereken parçaları.</summary>
    private sealed record ApplicableTax(TaxItemDto Item, TaxDetailWithConditionsDto Rate, TaxConditionDto Condition);
}
