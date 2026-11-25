namespace DatasoftApiSample.Api;

/// <summary>
/// Her iki servisin de kullandığı yanıt zarfı:
/// <c>{ "isSuccess": true, "data": ..., "errors": [...] }</c>
///
/// Doğrulama hatalarında HTTP kodu çoğunlukla yine 200'dür; işlemin başarısı
/// <see cref="IsSuccess"/> alanından anlaşılır.
/// </summary>
public sealed class ApiResponse<TData>
{
    public bool IsSuccess { get; set; }
    public TData? Data { get; set; }
    public List<ApiError>? Errors { get; set; }
}

/// <summary>Yanıt zarfındaki tek bir hata.</summary>
public sealed class ApiError
{
    /// <summary>Hata kodu; ör. "REQUIRED", "ALREADY_EXIST", "NOT_FOUND".</summary>
    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>Hatanın ilgili olduğu alanlar; ör. "Addresses[0].BuildingNo".</summary>
    public List<string>? PropertyNames { get; set; }

    public override string ToString()
    {
        string message = string.IsNullOrWhiteSpace(ErrorMessage) ? "(mesaj yok)" : ErrorMessage;
        string fields = string.Join(", ", (PropertyNames ?? []).Where(name => !string.IsNullOrEmpty(name)));

        return fields.Length == 0
            ? $"{ErrorCode}: {message}"
            : $"{ErrorCode}: {message} [alan: {fields}]";
    }
}
