using Common;
using Hw5.Exercise0;
using RichardSzalay.MockHttp;

var httpClientMock = new MockHttpMessageHandler();
using var fallbackHttpClient = new HttpClient();
httpClientMock.Fallback.WithAny().Respond(fallbackHttpClient);
var app = new HttpClientApplication(httpClientMock, new FileSystemProvider());

return (int)app.Run(args);
