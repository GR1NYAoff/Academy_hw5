using System.Text.Json.Serialization;

namespace Hw5.Exercise0;

internal class CurrencyRate
{
    [JsonPropertyName("r030")]
    public int R030 { get; set; }

    [JsonPropertyName("txt")]
    public string? Txt { get; set; }

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("cc")]
    public string? Cc { get; set; }

    [JsonPropertyName("exchangedate")]
    public string? Exchangedate { get; set; }

}
