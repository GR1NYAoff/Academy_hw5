using System.Globalization;
using System.Text;
using System.Text.Json;
using Common;
using RichardSzalay.MockHttp;

namespace Hw5.Exercise0;

public class HttpClientApplication
{
    private readonly IFileSystemProvider _fileSystemProvider;
    private readonly HttpClient _httpClient;
    private static readonly string _today = DateTime.Today.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
    private static readonly string _urlApi = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json";
    private static readonly string _cache = "cache.json";

    public HttpClientApplication(MockHttpMessageHandler httpMessageHandler, IFileSystemProvider fileSystemProvider)
    {
        _fileSystemProvider = fileSystemProvider;
        _httpClient = httpMessageHandler.ToHttpClient();
    }

    /// <summary>
    /// Runs http client app.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>
    /// Returns <see cref="ReturnCode.Success"/> in case of successful exchange calculation.
    /// Returns <see cref="ReturnCode.InvalidArgs"/> in case of invalid <paramref name="args"/>.
    /// Returns <see cref="ReturnCode.Error"/> in case of error <paramref name="args"/>.
    /// </returns>
    public ReturnCode Run(params string[] args)
    {
        if (args.Length != 3)
            return ReturnCode.InvalidArgs;

        var currentCurrency = args[0].ToUpper(CultureInfo.InvariantCulture);
        var desiredСurrency = args[1].ToUpper(CultureInfo.InvariantCulture);
        var parseSum = decimal.TryParse(args[2], out var sum);

        if (!parseSum && sum >= 0)
            return ReturnCode.InvalidArgs;

        _httpClient.BaseAddress = new Uri(_urlApi);

        var cache = string.Empty;

        if (!_fileSystemProvider.Exists(_cache))
        {
            var webContent = GetWebContent(_httpClient.BaseAddress.AbsoluteUri);

            var bytes = Encoding.UTF8.GetBytes(webContent);
            var ms = new MemoryStream(bytes);

            _fileSystemProvider.Write(_cache, ms);

            using var sr = new StreamReader(ms);
            cache = sr.ReadToEnd();

        }
        else
        {
            using var cacheStream = _fileSystemProvider.Read(_cache);
            var sr = new StreamReader(cacheStream);
            cache = sr.ReadToEnd();
        }

        var currentRate = JsonSerializer.Deserialize<CurrencyRate[]>(cache);

        if (currentRate!.Any(r => r.Exchangedate != _today))
        {
            var webContent = GetWebContent(_httpClient.BaseAddress.AbsoluteUri);

            var bytes = Encoding.UTF8.GetBytes(webContent);
            var ms = new MemoryStream(bytes);

            _fileSystemProvider.Write(_cache, ms);
            ms.Dispose();

            var document = JsonDocument.Parse(webContent);
            currentRate = document.Deserialize<CurrencyRate[]>();
        }

        decimal current = default;
        decimal desired = default;

        if (currentCurrency == "UAH")
            current = 1;
        else if (desiredСurrency == "UAH")
            desired = 1;

        for (var i = 0; i < currentRate?.Length; i++)
        {
            if (currentRate[i].Cc == currentCurrency)
            {
                current = currentRate[i].Rate;
            }
            else if (currentRate[i].Cc == desiredСurrency)
            {
                desired = currentRate[i].Rate;
            }
        }

        if (current == default || desired == default)
            return ReturnCode.InvalidArgs;

        var rate = current / desired;
        var result = sum * rate;

        Console.WriteLine("Rate {0}:{1} = {2}, Date: {3}, Sum: {4}", currentCurrency, desiredСurrency, rate, _today, result);

        return ReturnCode.Success;

    }

    private static string GetWebContent(string url)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        var client = new HttpClient();
        var v = client.SendAsync(requestMessage).Result.Content.ReadAsStream();

        using var reader = new StreamReader(v);

        return reader.ReadToEnd();

    }

}
