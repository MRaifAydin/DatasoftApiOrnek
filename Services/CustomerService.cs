using DatasoftApiSample.Api;
using DatasoftApiSample.Configuration;
using DatasoftApiSample.Models;
using DatasoftApiSample.Models.Common;
using DatasoftApiSample.Models.Customers;

namespace DatasoftApiSample.Services;

/// <summary>Cari kart (müşteri / tedarikçi) işlemleri.</summary>
public sealed class CustomerService(DatasoftApiClient api, LookupService lookups)
{
    /// <summary>
    /// Anahtar kelimeyle cari arar. Benzer kayıtları da döndürür; tam eşleşme için
    /// <see cref="FindByCodeAsync"/> kullanılmalıdır.
    /// <c>POST /api/CustomerSupplier/Search</c>
    /// </summary>
    public Task<List<CustomerSummaryDto>> SearchAsync(string keyword, int maxCount)
        => api.PostAsync<List<CustomerSummaryDto>>("/api/CustomerSupplier/Search", PagedRequest.FirstPage(keyword, maxCount));

    /// <summary>
    /// Koda göre cari kartı getirir; yoksa null döner.
    /// <c>POST /api/CustomerSupplier/GetByCode</c> (gövde yalnızca kod)
    /// </summary>
    public Task<CustomerCardDto?> FindByCodeAsync(string code)
        => api.PostOrDefaultAsync<CustomerCardDto>("/api/CustomerSupplier/GetByCode", code);

    /// <summary>
    /// Id'ye göre cari kartı getirir.
    /// <c>POST /api/CustomerSupplier/GetById</c> (gövde yalnızca id)
    /// </summary>
    public Task<CustomerCardDto> GetByIdAsync(long id)
        => api.PostAsync<CustomerCardDto>("/api/CustomerSupplier/GetById", id);

    /// <summary>
    /// Koda göre carinin özetini getirir; yoksa null döner.
    /// <c>POST /api/CustomerSupplier/SearchDetail</c>
    /// </summary>
    public async Task<CustomerSummaryDto?> FindSummaryByCodeAsync(string code)
    {
        var filter = new CustomerDetailSearchFilter { Code = code };
        List<CustomerSummaryDto> customers = await api.PostAsync<List<CustomerSummaryDto>>("/api/CustomerSupplier/SearchDetail", PagedRequest.FirstPage(filter));

        // Filtre kodu içeren kayıtları da getirebildiği için tam eşleşen ayrıca seçilir.
        return customers.FirstOrDefault(customer => customer.Code == code);
    }

    /// <summary>
    /// Yeni bir tüzel kişi (şirket) müşteri kartı oluşturur ve id'sini döndürür.
    /// <c>POST /api/CustomerSupplier/Upsert</c>
    /// </summary>
    public async Task<long> CreateAsync(CustomerSettings customer)
    {
        // Adres ve vergi dairesi id ile gönderilir; adlarından bulunur.
        RegionDto country = await lookups.FindCountryAsync(customer.Country);
        RegionDto city = await lookups.FindCityAsync(country.Id, customer.City);
        RegionDto county = await lookups.FindCountyAsync(city.Id, customer.County);
        TaxOfficeDto taxOffice = await lookups.FindTaxOfficeAsync(customer.TaxOffice);

        var card = new CustomerCardDto
        {
            GeneralInformation = new CustomerGeneralInformationDto
            {
                Code = customer.Code,
                Type = CustomerSupplierType.Customer,
                Status = RecordStatus.Active,
                LegalStructure = LegalStructure.LegalEntity,
                Origin = CustomerOrigin.Domestic,
                PublicPrivate = PublicPrivate.Private,
                PaymentCollectionMethod = PaymentCollectionMethod.InvoiceBased,
                Title = customer.Title,
                TaxNumber = customer.TaxNumber,
                TaxOffice = taxOffice
            },
            Addresses =
            [
                new AddressDto
                {
                    IsDefault = true,
                    AddressName = "Merkez",
                    FullAddress = customer.FullAddress,
                    Country = new RegionDto { Id = country.Id },
                    City = new RegionDto { Id = city.Id },
                    County = new RegionDto { Id = county.Id },
                    Neighborhood = NullIfEmpty(customer.Neighborhood),
                    Avenue = NullIfEmpty(customer.Avenue),
                    Street = NullIfEmpty(customer.Street),
                    BuildingNo = customer.BuildingNo,
                    PostCode = NullIfEmpty(customer.PostCode)
                }
            ],
            DefaultCollectionPlan = new PaymentPlanDto { BillingType = BillingType.Cash },
            DefaultPaymentPlan = new PaymentPlanDto { BillingType = BillingType.Cash }
        };

        // E-posta, e-Arşiv faturasının alıcıya iletilmesinde kullanılır; isteğe bağlıdır.
        if (!string.IsNullOrWhiteSpace(customer.Email))
            card.Contacts.Add(new ContactDto { IsDefault = true, Email = customer.Email });

        return await api.PostAsync<long>("/api/CustomerSupplier/Upsert", card);
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
