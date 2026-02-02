# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Union.Playwright.NUnit is a .NET 9.0 testing framework built on top of Microsoft Playwright and NUnit. It provides a Page Object Model abstraction with URL-based routing, enabling type-safe navigation and automatic page matching based on URL patterns.

## Build and Test Commands

```bash
# Build the project
dotnet build

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run a single test by name
dotnet test --filter "FullyQualifiedName~HomepageHasPlaywrightInTitleAndGetStartedLinkLinkingtoTheIntroPage"

# Run tests in headed mode (visible browser)
$env:HEADED="1"; dotnet test

# Install Playwright browsers
pwsh bin/Debug/net9.0/playwright.ps1 install
```

## Architecture

### Core Abstractions

**UnionService<T>** (`Services/UnionService.cs`): The main entry point for a service/application under test. Each service:
- Defines a `BaseUrl` for the application
- Auto-registers all page types derived from `T` via reflection
- Provides `Go` (navigation) and `State` (current page info) properties
- Uses `MatchUrlRouter` for URL-to-page matching

**UnionPage** (`Pages/UnionPage.cs`): Base class for page objects. Each page:
- Defines an `AbsolutePath` (can include `{paramName}` placeholders)
- Contains `Data` (path segments) and `Params` (query string) dictionaries
- Is activated when navigated to, triggering component initialization

**BrowserGo** (`TestSession/BrowserGo.cs`): Handles navigation operations:
- `ToPage<T>()` - Navigate to a page by type
- `ToUrl()` - Navigate to raw URL
- `Refresh()`, `Back()` - Browser navigation
- Automatically actualizes state after navigation

**BrowserState** (`TestSession/BrowserState.cs`): Tracks current browser state:
- `PageAs<T>()` - Get current page as specific type
- `PageIs<T>()` - Check if current page matches type
- `Actualize()` - Match current URL to registered pages

### URL Matching System

**UriMatcher** (`Routing/UriMatcher.cs`): Matches URLs to page definitions:
- Supports path parameters like `/users/{userId}/orders/{orderId}`
- Extracts query parameters automatically
- Validates against page's expected Data and Params

**MatchUrlRouter** (`Routing/MatchUrlRouter.cs`):
- Maintains registry of page types
- `RegisterDerivedPages<T>()` auto-discovers pages via reflection
- Creates page instances when URLs match

### Component System (Work in Progress)

**UnionInit** attribute (`Attributes/UnionInit.cs`): Marks fields for automatic initialization
**WebPageBuilder** (`Pages/WebPageBuilder.cs`): Component factory (implementation incomplete)


## Intended Usage Pattern

```csharp
// Define pages for a service
public abstract class MyServicePage : UnionPage { }

public class LoginPage : MyServicePage
{
    public override string AbsolutePath => "/login";
}

public class UserProfilePage : MyServicePage
{
    public override string AbsolutePath => "/users/{userId}";
}

// Define the service
public class MyService : UnionService<MyServicePage>
{
    public override string BaseUrl => "https://app.example.com";
}

// In tests
await myService.Go.ToPage<LoginPage>();
var loginPage = myService.State.PageAs<LoginPage>();
```
