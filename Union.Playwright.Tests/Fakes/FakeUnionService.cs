using Microsoft.Playwright;
using NSubstitute;
using Union.Playwright.Core;
using Union.Playwright.Pages.Interfaces;
using Union.Playwright.Routing;
using Union.Playwright.Services;
using Union.Playwright.TestSession;

namespace Union.Playwright.Tests.Fakes;

/// <summary>
/// A fake IUnionService implementation for testing.
/// </summary>
public class FakeUnionService : IUnionService
{
    private readonly string _baseUrl;

    public FakeUnionService(string baseUrl = "https://test.example.com")
    {
        _baseUrl = baseUrl;
        ServiceContextsPool = Substitute.For<IServiceContextsPool>();
        State = Substitute.For<IBrowserState>();
        Go = Substitute.For<IBrowserGo>();
    }

    public string BaseUrl => _baseUrl;

    public BaseUrlPattern BaseUrlPattern => new BaseUrlPattern(new BaseUrlRegexBuilder(_baseUrl).Build());

    public IServiceContextsPool ServiceContextsPool { get; }

    public IBrowserState State { get; }

    public IBrowserGo Go { get; }

    public IUnionPage GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo)
    {
        return Substitute.For<IUnionPage>();
    }

    public RequestData GetRequestData(IUnionPage page)
    {
        return new RequestData(_baseUrl);
    }

    public bool HasPage(IUnionPage page)
    {
        return true;
    }
}
