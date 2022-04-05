using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Common;
using FluentAssertions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Xunit;

namespace Hw5.Exercise0.Tests;

public class HttpClientApplicationTests
{
    private const string CacheFile = "cache.json";
    private const string ApiUrl = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json";
    private static readonly string _today = DateTime.Today.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
    private static readonly string _yesterday = DateTime.Today.Subtract(TimeSpan.FromDays(1)).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    private readonly object _usd = new
    {
        cc = "USD",
        rate = 27.5093,
        exchangedate = _today
    };

    private readonly object _eur = new
    {
        cc = "EUR",
        rate = 31.1722,
        exchangedate = _today
    };

    private readonly object _staleUsd = new
    {
        cc = "USD",
        rate = 27.5093,
        exchangedate = _yesterday
    };

    private readonly object _staleEur = new
    {
        cc = "EUR",
        rate = 31.1722,
        exchangedate = _yesterday
    };

    [Fact]
    public void Run_WhenStaleCacheDataAndNbuAvailable_ExpectedNbuApiCallAndReturnsSuccess()
    {
        // arrange
        var fileSystemProvider = Substitute.For<IFileSystemProvider>();
        fileSystemProvider.Exists(Arg.Is(CacheFile)).Returns(true);
        fileSystemProvider.Read(Arg.Is(CacheFile)).Returns(ToUtf8ByteStream(_staleUsd, _staleEur));

        var httpClientMock = new MockHttpMessageHandler();
        var app = new HttpClientApplication(httpClientMock, fileSystemProvider);
        httpClientMock
            .When(HttpMethod.Get, ApiUrl)
            .Respond(HttpStatusCode.OK, "application/json", ToUtf8ByteStream(_usd, _eur));

        // act
        var exitCode = app.Run("usd", "uah", "10");

        // assert
        exitCode.Should().Be(ReturnCode.Success);
        fileSystemProvider.Received().Write(Arg.Is(CacheFile), Arg.Any<Stream>());
        httpClientMock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public void Run_WhenCacheDataAndNbuAvailable_ExpectedReturnsSuccess()
    {
        // arrange
        var fileSystemProvider = Substitute.For<IFileSystemProvider>();
        fileSystemProvider.Exists(Arg.Is(CacheFile)).Returns(true);
        fileSystemProvider.Read(Arg.Is(CacheFile)).Returns(ToUtf8ByteStream(_usd, _eur));

        var httpClientMock = new MockHttpMessageHandler();
        var app = new HttpClientApplication(httpClientMock, fileSystemProvider);

        // act
        var exitCode = app.Run("usd", "uah", "10");

        // assert
        exitCode.Should().Be(ReturnCode.Success);
        httpClientMock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public void Run_WhenNoCacheDataAndNbuAvailable_ExpectedWriteToCacheAndReturnsSuccess()
    {
        // arrange
        var fileSystemProvider = Substitute.For<IFileSystemProvider>();
        fileSystemProvider.Exists(Arg.Is(CacheFile)).Returns(false);

        var httpClientMock = new MockHttpMessageHandler();
        httpClientMock
            .When(HttpMethod.Get, ApiUrl)
            .Respond(HttpStatusCode.OK, "application/json", ToUtf8ByteStream(_usd, _eur));

        var app = new HttpClientApplication(httpClientMock, fileSystemProvider);

        // act
        var exitCode = app.Run("usd", "uah", "10");

        // assert
        exitCode.Should().Be(ReturnCode.Success);
        fileSystemProvider.Received().Write(Arg.Is(CacheFile), Arg.Any<Stream>());
        httpClientMock.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [InlineData("usd", "uah")]
    [InlineData("usdt", "uah", "10")]
    [InlineData("xxx", "uah", "10")]
    [InlineData("usd", "yyy", "10")]
    [InlineData("usd", "uaht", "10")]
    [InlineData("usd", "uah", "ten")]
    [InlineData("usd", "uah", "10", "some arg")]
    public void Run_WhenNotValidArgs_ReturnsInvalidArgsExitCode(params string[] args)
    {
        // arrange
        var fileSystemProvider = GetFilesProvider(new { });
        var httpClientMock = new MockHttpMessageHandler();
        var app = new HttpClientApplication(httpClientMock, fileSystemProvider);

        // act
        var exitCode = app.Run(args);

        // assert
        exitCode.Should().Be(ReturnCode.InvalidArgs);
    }

    private static Stream ToUtf8ByteStream<T>(params T[] content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(content)));
    }

    private static IFileSystemProvider GetFilesProvider<T>(params T[] content)
    {
        var filesProvider = Substitute.For<IFileSystemProvider>();
        filesProvider
            .Read(Arg.Is(CacheFile))
            .Returns(ToUtf8ByteStream(content));

        return filesProvider;
    }
}
