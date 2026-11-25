using DatasoftApiSample.Api;
using DatasoftApiSample.Configuration;
using DatasoftApiSample.Models.Common;
using DatasoftApiSample.Models.Customers;
using DatasoftApiSample.Models.Invoices;
using DatasoftApiSample.Models.Stocks;

namespace DatasoftApiSample.Services;

/// <summary>Faturaya eklenecek bir satırın girdisi.</summary>
/// <param name="StockCode">Stok kartının kodu.</param>
/// <param name="Quantity">Stokun ana birimi cinsinden miktar.</param>
/// <param name="UnitPrice">KDV hariç birim fiyat; null ise stok kartındaki satış fiyatı kullanılır.</param>
public sealed record InvoiceLineInput(string StockCode, decimal Quantity, decimal? UnitPrice = null);

/// <summary>
/// Satış faturası hazırlama ve kaydetme.
///
/// Fatura modeli çok büyük olduğu için sıfırdan kurulmaz. Akış şöyledir:
/// <list type="number">
///   <item>Sunucudan boş fatura şablonu alınır; şablon belge türü ayarları, varsayılan senaryo,
///         para birimi ve vergi tablolarıyla gelir.</item>
///   <item>Şablona cari, seri numarası, satırlar ve toplamlar yazılır.</item>
///   <item>Fatura kaydedilir.</item>
/// </list>
/// </summary>
public sealed class InvoiceService(DatasoftApiClient api, CustomerService customers, StockService stocks)
{
    /// <summary>
    /// Verilen cari ve satırlarla bir satış faturası hazırlar. Sunucuya kayıt yapmaz;
    /// kaydetmek için <see cref="SaveAsync"/> çağrılır.
    /// </summary>
    public async Task<InvoiceDto> PrepareSalesInvoiceAsync(string customerCode, IReadOnlyList<InvoiceLineInput> lineInputs, InvoiceSettings settings)
    {
        DateTime documentDate = Today();

        DocumentTypeDto documentType = await GetDefaultDocumentTypeAsync(settings.DocumentCode);
        InvoiceTemplateDto template = await GetTemplateAsync(documentType, documentDate);

        InvoiceGeneralInfoDto generalInfo = template.GeneralInfo
            ?? throw new InvalidOperationException("Fatura şablonunda genel bilgiler yok.");
        generalInfo.Date = documentDate;

        // --- Cari ---
        CustomerSummaryDto customer = await customers.FindSummaryByCodeAsync(customerCode)
            ?? throw new InvalidOperationException($"\"{customerCode}\" kodlu cari bulunamadı.");

        CustomerCardDto customerCard = await customers.GetByIdAsync(customer.Id);

        AddressDto address = customerCard.Addresses.FirstOrDefault(candidate => candidate.IsDefault)
            ?? customerCard.Addresses.FirstOrDefault()
            ?? throw new InvalidOperationException($"\"{customerCode}\" carisinin adresi yok; fatura için adres zorunludur.");

        ContactDto? contact = customerCard.Contacts.FirstOrDefault(candidate => candidate.IsDefault);

        // --- Uygulayıcı, senaryo ve seri ---
        await ApplyCustomerOperatorAsync(template, documentType, customer, documentDate, settings.Scenario);
        await AssignSerialAsync(template, documentType, documentDate, settings.SerialCode);

        // --- Satırlar ve toplamlar ---
        var calculator = new InvoiceCalculator(template);
        var lines = new List<InvoiceLineDto>();

        foreach (InvoiceLineInput input in lineInputs)
        {
            StockForDocumentDto stock = await stocks.GetForDocumentAsync(input.StockCode, documentType);
            lines.Add(calculator.CreateLine(lines.Count + 1, stock, input.Quantity, input.UnitPrice));
        }

        // Şablondaki vergi tabloları kaydetme modelinde yer almadığı için yeni bir nesne kurulur.
        return new InvoiceDto
        {
            Id = 0,
            Settings = template.Settings,
            StockGridColumns = template.StockGridColumns,
            GeneralInfo = generalInfo,
            OperatorType = template.OperatorType,
            CurrencyInformations = template.CurrencyInformations,
            Customer = customer,
            CustomerAddress = address,
            CustomerContact = contact,
            Stocks = lines,
            Total = calculator.CalculateTotal(lines),
            TotalTaxResultList = calculator.CalculateTaxTotals(lines),
            AdditionalDescriptions = string.IsNullOrWhiteSpace(settings.Description) ? null : [settings.Description]
        };
    }

    /// <summary>
    /// Faturayı kaydeder ve id'sini döndürür. Yalnızca kayıt yapar; fatura "Gönderilmedi"
    /// durumunda kalır. (Kaydedip e-belge olarak gönderen uç <c>UpsertBsdiAndSend</c>'dir.)
    /// <c>POST /api/BSDI/UpsertBsdi</c>
    /// </summary>
    public Task<long> SaveAsync(InvoiceDto invoice)
        => api.PostAsync<long>("/api/BSDI/UpsertBsdi", invoice);

    /// <summary>
    /// Kaydedilmiş faturayı getirir.
    /// <c>POST /api/BSDI/GetBsdi</c> (gövde yalnızca id)
    /// </summary>
    public Task<SavedInvoiceDto> GetSavedAsync(long invoiceId)
        => api.PostAsync<SavedInvoiceDto>("/api/BSDI/GetBsdi", invoiceId);

    /// <summary>
    /// Belge cinsinin varsayılan alt türünü bulur; Fatura için "Fatura (Genel Tip)".
    /// <c>POST /api/BSDT/GetAllByBSDBCode</c>
    /// </summary>
    private async Task<DocumentTypeDto> GetDefaultDocumentTypeAsync(string documentCode)
    {
        var filter = new DocumentTypeFilter { BsdbCode = documentCode, IsOutgoing = true };
        List<DocumentTypeDto> documentTypes = await api.PostAsync<List<DocumentTypeDto>>("/api/BSDT/GetAllByBSDBCode", PagedRequest.All(filter));

        return documentTypes.FirstOrDefault(documentType => documentType.IsDefault)
               ?? documentTypes.FirstOrDefault()
               ?? throw new InvalidOperationException($"\"{documentCode}\" kodlu belge için alt tür bulunamadı.");
    }

    /// <summary><c>POST /api/BSDI/GetNewBsdi</c></summary>
    private Task<InvoiceTemplateDto> GetTemplateAsync(DocumentTypeDto documentType, DateTime documentDate)
    {
        var request = new NewInvoiceRequest
        {
            BsdtId = documentType.BsdtId,
            BsdtIsSystem = documentType.IsBSDTSystemDefined,
            DocumentDate = documentDate,
            IsIncoming = false
        };

        return api.PostAsync<InvoiceTemplateDto>("/api/BSDI/GetNewBsdi", request);
    }

    /// <summary>
    /// Faturanın uygulayıcısını cariye göre ayarlar.
    ///
    /// Şablon şirketin varsayılan uygulayıcısıyla gelir, ancak belirleyici olan caridir:
    /// cari e-Fatura mükellefiyse e-Fatura, değilse e-Arşiv kesilir. Uyuşmazlıkta kayıt
    /// "Cari, seçili e-Belge türüne kayıtlı değil" hatasıyla reddedilir.
    /// <c>POST /api/BSDI/GetBsdOperatorType</c>
    /// </summary>
    private async Task ApplyCustomerOperatorAsync(InvoiceTemplateDto template, DocumentTypeDto documentType, CustomerSummaryDto customer, DateTime documentDate, string? preferredScenario)
    {
        var request = new OperatorTypeRequest
        {
            BsdbId = documentType.BsdbId,
            BsdbIsSystem = documentType.IsBSDBSystemDefined,
            Identifier = customer.TcknVkn,
            DocumentDate = documentDate
        };

        OperatorTypeDto? customerOperator = await api.PostAsync<OperatorTypeDto?>("/api/BSDI/GetBsdOperatorType", request);

        bool isSameAsTemplate = customerOperator?.OperatorTypeId is null
                                || template.OperatorType is null
                                || customerOperator.OperatorTypeId == template.OperatorType.OperatorTypeId;
        if (isSameAsTemplate)
            return;

        template.OperatorType!.OperatorTypeId = customerOperator!.OperatorTypeId;
        template.OperatorType.HasGovernmentConnection = customerOperator.HasGovernmentConnection;
        template.OperatorType.Name = customerOperator.Name;

        // Senaryolar uygulayıcıya bağlıdır; uygulayıcı değişince yeniden seçilmelidir.
        await SelectScenarioAsync(template.GeneralInfo!, documentType, customerOperator.OperatorTypeId!.Value, preferredScenario);
    }

    /// <summary>
    /// Uygulayıcının senaryolarından birini seçer; şablondaki e-belge tipi ("Satış") korunur.
    /// <c>POST /api/BSDI/GetAllScenarioByBsdtCodeAndOperatorTypeId</c>
    /// </summary>
    private async Task SelectScenarioAsync(InvoiceGeneralInfoDto generalInfo, DocumentTypeDto documentType, long operatorTypeId, string? preferredScenario)
    {
        var request = new ScenarioRequest { BsdtCode = documentType.Code, OperatorTypeId = operatorTypeId };
        List<ScenarioDto> scenarios = await api.PostAsync<List<ScenarioDto>>("/api/BSDI/GetAllScenarioByBsdtCodeAndOperatorTypeId", request);

        long? eTypeId = generalInfo.EType?.Id;
        bool SupportsEType(ScenarioDto scenario) => scenario.ETypes?.Any(eType => eType.Id == eTypeId) == true;

        // Ayarda adı verilen senaryo; yoksa mevcut e-belge tipini destekleyen ilk senaryo.
        ScenarioDto selected = scenarios.FirstOrDefault(scenario => LookupService.IsSameText(scenario.Name, preferredScenario))
                               ?? scenarios.FirstOrDefault(SupportsEType)
                               ?? throw new InvalidOperationException("Carinin e-belge uygulayıcısı için uygun senaryo bulunamadı.");

        generalInfo.Scenario = new ScenarioDto { Id = selected.Id, Code = selected.Code, Name = selected.Name };
        generalInfo.EType = selected.ETypes?.FirstOrDefault(eType => eType.Id == eTypeId)
                            ?? selected.ETypes?.FirstOrDefault();
    }

    /// <summary>
    /// Faturaya seri ve sıradaki seri numarasını yazar. Numara sunucudan alınır; elle
    /// üretilirse yıl uyuşmazlığı ya da "zaten kullanılmış" hatası alınabilir.
    /// <c>POST /api/BSDSerial/GetSerialsByBsdb</c>, <c>POST /api/BSDSerial/GetSerialNumberNext</c>
    /// </summary>
    private async Task AssignSerialAsync(InvoiceTemplateDto template, DocumentTypeDto documentType, DateTime documentDate, string? preferredSerialCode)
    {
        long? operatorTypeId = template.OperatorType?.OperatorTypeId;

        var listRequest = new SerialListRequest
        {
            BsdbId = documentType.BsdbId,
            IsSystemDefined = documentType.IsBSDBSystemDefined,
            OperatorTypeId = operatorTypeId,
            IsIncoming = false
        };

        List<SerialDto> serials = await api.PostAsync<List<SerialDto>>("/api/BSDSerial/GetSerialsByBsdb", listRequest);

        SerialDto? serial = string.IsNullOrWhiteSpace(preferredSerialCode)
            ? serials.FirstOrDefault(candidate => candidate.IsDefault) ?? serials.FirstOrDefault()
            : serials.FirstOrDefault(candidate => LookupService.IsSameText(candidate.Code, preferredSerialCode));

        if (serial is null)
            throw new InvalidOperationException("Bu belge türü ve uygulayıcı için kullanılabilir seri bulunamadı. Arayüzden fatura serisi tanımlayın.");

        var numberRequest = new NextSerialNumberRequest
        {
            BsdbId = documentType.BsdbId,
            BsdbIsSystem = documentType.IsBSDBSystemDefined,
            IsIncoming = false,
            SerialId = serial.Id,
            SerialCode = serial.Code,
            OperatorTypeId = operatorTypeId,
            DocumentDate = documentDate,
            Year = documentDate.Year
        };

        template.GeneralInfo!.Serial = new SerialRefDto { Id = serial.Id, Code = serial.Code };
        template.GeneralInfo.SerialNumber = await api.PostAsync<string>("/api/BSDSerial/GetSerialNumberNext", numberRequest);
    }

    /// <summary>
    /// Bugünün tarihi, saat dilimi bilgisi olmadan. API tarihleri "2026-11-25T00:00:00"
    /// biçiminde bekler; <see cref="DateTime.Today"/> doğrudan kullanılırsa sonuna saat farkı eklenir.
    /// </summary>
    private static DateTime Today()
    {
        DateTime today = DateTime.Today;
        return new DateTime(today.Year, today.Month, today.Day);
    }
}
