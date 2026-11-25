namespace DatasoftApiSample.Models.Invoices;

// Fatura hazırlanırken çağrılan yardımcı uçların istek ve yanıt nesneleri.

/// <summary>BSDT/GetAllByBSDBCode filtresi: bir belge cinsinin alt türlerini listeler.</summary>
public sealed class DocumentTypeFilter
{
    /// <summary>Belge cinsinin kodu; Fatura için "S4100".</summary>
    public required string BsdbCode { get; set; }

    /// <summary>true: satış belgesi olarak kullanılabilen türler.</summary>
    public bool? IsOutgoing { get; set; }
}

/// <summary>Belge alt türü (BSDT) ve bağlı olduğu belge cinsi (BSDB).</summary>
public sealed class DocumentTypeDto
{
    public long BsdtId { get; set; }
    public bool IsBSDTSystemDefined { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public long BsdbId { get; set; }
    public bool IsBSDBSystemDefined { get; set; }

    /// <summary>Belge cinsinin varsayılan alt türü; Fatura için "Fatura (Genel Tip)".</summary>
    public bool IsDefault { get; set; }
}

/// <summary>BSDI/GetNewBsdi isteği. Swagger: BSDTBaseDto</summary>
public sealed class NewInvoiceRequest
{
    public long BsdtId { get; set; }
    public bool BsdtIsSystem { get; set; }
    public DateTime DocumentDate { get; set; }

    /// <summary>false: satış belgesi, true: alış belgesi.</summary>
    public bool IsIncoming { get; set; }
}

/// <summary>BSDI/GetBsdOperatorType isteği: carinin e-belge uygulayıcısını sorar.</summary>
public sealed class OperatorTypeRequest
{
    public long BsdbId { get; set; }
    public bool BsdbIsSystem { get; set; }

    /// <summary>Carinin VKN veya TCKN'si.</summary>
    public string? Identifier { get; set; }

    public DateTime DocumentDate { get; set; }
}

/// <summary>BSDI/GetAllScenarioByBsdtCodeAndOperatorTypeId isteği.</summary>
public sealed class ScenarioRequest
{
    public string? BsdtCode { get; set; }
    public long OperatorTypeId { get; set; }
}

/// <summary>BSDSerial/GetSerialsByBsdb isteği. Swagger: GetSerialsDto</summary>
public sealed class SerialListRequest
{
    public long BsdbId { get; set; }
    public bool IsSystemDefined { get; set; }

    /// <summary>Seriler uygulayıcıya göre tanımlıdır; boş gönderilirse liste boş döner.</summary>
    public long? OperatorTypeId { get; set; }

    public bool IsIncoming { get; set; }
}

/// <summary>Belge serisi; ör. e-Arşiv için "AKA".</summary>
public sealed class SerialDto
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>BSDSerial/GetSerialNumberNext isteği. Swagger: GetSerialNextDto</summary>
public sealed class NextSerialNumberRequest
{
    public long BsdbId { get; set; }
    public bool BsdbIsSystem { get; set; }
    public bool IsIncoming { get; set; }
    public long SerialId { get; set; }
    public string? SerialCode { get; set; }
    public long? OperatorTypeId { get; set; }
    public DateTime DocumentDate { get; set; }
    public int Year { get; set; }
}
