namespace DatasoftApiSample.Models;

// API bu alanları SAYI olarak alır ve döndürür; Swagger yalnızca izin verilen sayıları listeler.
// Aşağıdaki ad-değer eşleşmeleri sunucu kaynak kodundaki enum tanımlarından alınmıştır.
// Her enum'un açıklamasında sunucudaki adı yazar.

/// <summary>Kart durumu; cari ve stokta ortak. Sunucu: ActivePassive</summary>
public enum RecordStatus
{
    Active = 1,
    Passive = 2
}

/// <summary>Cari tipi. Sunucu: BuySell</summary>
public enum CustomerSupplierType
{
    /// <summary>Alıcı (müşteri): satış faturası kesilir.</summary>
    Customer = 1,

    /// <summary>Satıcı (tedarikçi): alış faturası kesilir.</summary>
    Supplier = 2,

    Both = 3
}

/// <summary>
/// Alış/satış yönü; stok fiyatı, stok vergisi ve stok aramasında kullanılır.
/// Sunucuda cari tipiyle aynı enum'dur (BuySell); okunabilirlik için ayrı tanımlandı.
/// </summary>
public enum TradeDirection
{
    Purchase = 1,
    Sale = 2,
    Both = 3
}

/// <summary>Hukuki yapı. Sunucu: LegalStructure</summary>
public enum LegalStructure
{
    None = 0,

    /// <summary>Gerçek kişi: TCKN + ad + soyad ile tanımlanır.</summary>
    RealPerson = 1,

    Partnership = 2,

    /// <summary>Tüzel kişi: VKN + ünvan ile tanımlanır.</summary>
    LegalEntity = 3
}

/// <summary>Yerli / yabancı. Sunucu: CustomerOrigin</summary>
public enum CustomerOrigin
{
    None = -1,
    Domestic = 0,
    Foreign = 1
}

/// <summary>Kamu / özel. Sunucu: PublicPrivate</summary>
public enum PublicPrivate
{
    None = -1,
    Private = 0,
    Public = 1
}

/// <summary>Ödeme/tahsilat biçimi. Sunucu: PaymentCollectionMethodType</summary>
public enum PaymentCollectionMethod
{
    InvoiceBased = 0,
    OpenAccount = 1
}

/// <summary>Varsayılan tahsilat/ödeme planı biçimi. Sunucu: DefaultPaymentCollectionBillingType</summary>
public enum BillingType
{
    Cash = 0,
    Deferred = 1,
    Installment = 2
}

/// <summary>Stok tipi. Sunucu: StockType</summary>
public enum StockType
{
    None = 0,

    /// <summary>Envanter: alınıp satılan, miktarı takip edilen ürün.</summary>
    Inventory = 1,

    Service = 2,
    Cost = 3,

    /// <summary>Amortismana tabi iktisadi kıymet (ATİK).</summary>
    FixedAsset = 4
}

/// <summary>Envanter tipi. Sunucu: StockInventoryType</summary>
public enum StockInventoryType
{
    None = 0,
    Goods = 1,
    RawMaterial = 2,
    SemiProduct = 3,
    Product = 4
}

/// <summary>Stok alt türü. Sunucu: Medicine</summary>
public enum StockSubType
{
    Other = 0,
    Medicine = 1,
    MedicalDevice = 2
}

/// <summary>Fiyatın vergiyi içerip içermediği. Sunucu: UnitPriceEntry</summary>
public enum TaxInclusion
{
    IncludingAllTaxes = 1,
    ExcludingTax = 2,
    IncludingAddedTaxes = 3
}

/// <summary>Stok fiyat yöntemi. Sunucu: StockPriceMethod</summary>
public enum StockPriceMethod
{
    None = 0,

    /// <summary>Başka bir fiyattan oranla hesaplanır.</summary>
    Calculated = 1,

    /// <summary>Fiyat elle girilir.</summary>
    UserEntered = 2
}

/// <summary>Stok vergi türü. Sunucu: TaxType</summary>
public enum TaxType
{
    Normal = 0,
    OptionalFullWithholding = 100
}

/// <summary>Vergi tanımının kaynağı. Sunucu: TaxItemDefinitionType</summary>
public enum TaxDefinitionSource
{
    User = 0,
    System = 1
}

/// <summary>Vergi koşulunun sonuç tipi. Sunucu: TaxConditionResultType</summary>
public enum TaxResultType
{
    /// <summary>Yüzde oranı; ör. sonuç 20 ise %20.</summary>
    Rate = 0,

    Rule = 1,
    ConstantAmount = 2
}

/// <summary>Verginin belge toplamına etkisi. Sunucu: TotalAmountOnEffectType</summary>
public enum TotalEffect
{
    /// <summary>Toplamdan düşülür (tevkifat, stopaj).</summary>
    Subtracted = 0,

    /// <summary>Toplama eklenir (KDV).</summary>
    Added = 1
}
