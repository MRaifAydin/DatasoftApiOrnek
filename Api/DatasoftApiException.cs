namespace DatasoftApiSample.Api;

/// <summary>
/// API işlemi başarısız olduğunda (<c>isSuccess: false</c>) ya da yanıt çözülemediğinde fırlatılır.
/// </summary>
public sealed class DatasoftApiException : Exception
{
    /// <summary>Sunucunun bildirdiği hatalar; yanıt çözülemediyse boştur.</summary>
    public IReadOnlyList<ApiError> Errors { get; }

    public DatasoftApiException(string message) : base(message)
    {
        Errors = [];
    }

    public DatasoftApiException(string endpoint, IReadOnlyList<ApiError> errors)
        : base(BuildMessage(endpoint, errors))
    {
        Errors = errors;
    }

    public bool HasErrorCode(string errorCode)
        => Errors.Any(error => string.Equals(error.ErrorCode, errorCode, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// "Kodla getir" uçları kayıt yoksa hata döner; kod cari tarafında "NotFoundError",
    /// stok tarafında "NOT_FOUND" olarak gelir.
    /// </summary>
    public bool IsNotFound => HasErrorCode("NotFoundError") || HasErrorCode("NOT_FOUND");

    private static string BuildMessage(string endpoint, IReadOnlyList<ApiError> errors)
    {
        string details = errors.Count == 0
            ? "- Sunucu hata ayrıntısı döndürmedi."
            : string.Join(Environment.NewLine, errors.Select(error => "- " + error));

        return $"{endpoint} çağrısı başarısız:{Environment.NewLine}{details}";
    }
}
