using System.Text.Json;
using System.Text.Json.Serialization;

namespace DatasoftApiSample.Models.Common;

/// <summary>
/// Sunucudan okunup başka bir isteğin içinde geri gönderilen nesnelerin taban sınıfı
/// (ör. arama sonucundan gelen para birimi, stok kartına konur).
///
/// Sunucu, istekte gelen nesneyi olduğu gibi saklar; eksik gönderilen alan boş kaydedilir.
/// DTO'larda yalnızca bu örneğin kullandığı alanlar tanımlıdır; tanımlı olmayan alanlar
/// burada tutulur ve nesne geri gönderilirken aynen yazılır. Böylece DTO'lar kısa kalır,
/// veri de kaybolmaz.
/// </summary>
public abstract class ServerObjectDto
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? UnmappedFields { get; set; }
}
