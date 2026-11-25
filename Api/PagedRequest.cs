namespace DatasoftApiSample.Api;

/// <summary>
/// Liste ve arama uçlarının ortak istek kalıbı:
/// <c>{ "data": filtre, "pagingOptions": { ... } }</c>
/// </summary>
public sealed class PagedRequest<TFilter>
{
    public TFilter? Data { get; set; }
    public PagingOptions PagingOptions { get; set; } = new();
}

public sealed class PagingOptions
{
    /// <summary>false ise tüm kayıtlar tek seferde döner.</summary>
    public bool EnablePaging { get; set; } = true;

    /// <summary>1'den başlar.</summary>
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}

/// <summary><see cref="PagedRequest{TFilter}"/> oluşturmak için kısayollar.</summary>
public static class PagedRequest
{
    /// <summary>Filtreye uyan ilk <paramref name="pageSize"/> kaydı ister.</summary>
    public static PagedRequest<TFilter> FirstPage<TFilter>(TFilter filter, int pageSize = 50) => new()
    {
        Data = filter,
        PagingOptions = new PagingOptions { PageSize = pageSize }
    };

    /// <summary>Sayfalama olmadan tüm kayıtları ister; küçük tanım listeleri için uygundur.</summary>
    public static PagedRequest<TFilter> All<TFilter>(TFilter filter) => new()
    {
        Data = filter,
        PagingOptions = new PagingOptions { EnablePaging = false }
    };
}
