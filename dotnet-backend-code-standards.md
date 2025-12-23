# .NET Backend Code Standards

This document defines code quality metrics and best practices for .NET backend development. It establishes patterns for maintainable, testable, and scalable enterprise applications.

---

## Table of Contents

1. [SOLID Principles](#part-1-solid-principles)
2. [Design Patterns](#part-2-design-patterns)
3. [Layer-Specific Guidelines](#part-3-layer-specific-guidelines)
4. [Clean Code Practices](#part-4-clean-code-practices)
5. [Infrastructure Patterns](#part-5-infrastructure-patterns)
6. [Code Quality Checklist](#part-6-code-quality-metrics-checklist)

---

## Part 1: SOLID Principles

### 1.1 Single Responsibility Principle (SRP)

Each class should have exactly one reason to change. Services should be scoped to a single domain concern.

#### Service Granularity

**Correct: Focused services with single responsibility**

```csharp
// Each service handles one specific concern
public interface IHashService
{
    string ComputeHash(string input);
}

public interface IDateTimeService
{
    DateTime GetCurrentUtc();
}

public interface IValidationService
{
    IEnumerable<ValidationResult> ValidateUser(UserProperties properties);
}

internal class HashService : IHashService
{
    public string ComputeHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
```

#### Command/Query Pattern for Operation Isolation

```csharp
// Commands: single state-changing operation
public interface ICommand<TParameter>
{
    Task ExecuteAsync(TParameter parameter);
}

// Queries: single data retrieval operation
public interface IQuery<TInput, TOutput>
{
    Task<TOutput> QueryAsync(TInput input);
}

// Each command is a separate class with one job
internal class CreateOrderCommand : ICommand<CreateOrderRequest>
{
    private readonly IApiClient client;
    private readonly ApiEndpoints endpoints;

    public CreateOrderCommand(IApiClient client, ApiEndpoints endpoints)
    {
        this.client = client;
        this.endpoints = endpoints;
    }

    public async Task ExecuteAsync(CreateOrderRequest request)
    {
        var uri = this.endpoints.GetOrdersUri();
        await this.client.PostAsync(uri, request);
    }
}
```

#### Decorators for Cross-Cutting Concerns

```csharp
// Logging is a separate concern, handled via decorator
internal class CommandLogger<T> : ICommand<T>
{
    private readonly ICommand<T> decorated;
    private readonly ILogger logger;

    public CommandLogger(ICommand<T> decorated, ILogger logger)
    {
        this.decorated = decorated;
        this.logger = logger;
    }

    public async Task ExecuteAsync(T parameter)
    {
        var name = typeof(T).Name;
        this.logger.LogInformation("{Command} started", name);
        try
        {
            await this.decorated.ExecuteAsync(parameter);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "{Command} failed", name);
            throw;
        }
        finally
        {
            this.logger.LogInformation("{Command} finished", name);
        }
    }
}
```

#### Anti-Pattern: God Services

```csharp
// WRONG: Service with multiple unrelated responsibilities
public class UserService
{
    public async Task<User> GetUserAsync(string id) { /* ... */ }
    public async Task SendEmailAsync(string to, string body) { /* ... */ }  // Email concern
    public async Task LogAuditAsync(string action) { /* ... */ }            // Audit concern
    public string HashPassword(string password) { /* ... */ }               // Crypto concern
    public async Task SyncCalendarAsync(string userId) { /* ... */ }        // Calendar concern
}

// CORRECT: Split into focused services
public interface IUserService { Task<User> GetUserAsync(string id); }
public interface IEmailService { Task SendAsync(string to, string body); }
public interface IAuditService { Task LogAsync(string action); }
public interface IPasswordService { string Hash(string password); }
public interface ICalendarSyncService { Task SyncAsync(string userId); }
```

---

### 1.2 Open/Closed Principle (OCP)

Software entities should be open for extension but closed for modification. Add new functionality without changing existing code.

#### Extension via Generic Interfaces

```csharp
// Generic interface allows unlimited command types without modification
public interface ICommand<TParameter>
{
    Task ExecuteAsync(TParameter parameter);
}

// New commands extend the system without modifying existing code
internal class CreateUserCommand : ICommand<CreateUserRequest> { /* ... */ }
internal class UpdateUserCommand : ICommand<UpdateUserRequest> { /* ... */ }
internal class DeleteUserCommand : ICommand<DeleteUserRequest> { /* ... */ }
```

#### Abstract Base with Template Method

```csharp
public abstract class ScopeBuilder<TScope> where TScope : Enum
{
    private readonly TScope[] scopes;

    protected ScopeBuilder(TScope[] scopes)
    {
        this.scopes = scopes;
    }

    // Template method - invariant algorithm
    public string Build()
    {
        return string.Join(' ', this.scopes.Select(Normalize));
    }

    // Extension point - subclasses customize this
    protected abstract string Normalize(TScope scope);
}

// Extensions don't modify base class
public class OAuthScopeBuilder : ScopeBuilder<OAuthScope>
{
    public OAuthScopeBuilder(OAuthScope[] scopes) : base(scopes) { }

    protected override string Normalize(OAuthScope scope)
    {
        return scope.ToString().ToLowerInvariant();
    }
}
```

#### Convention-Based Auto-Registration

```csharp
// Scrutor enables adding new implementations without modifying registration
public static IServiceCollection AddCommands(this IServiceCollection services)
{
    return services.Scan(selector => selector
        .FromAssemblyOf<ICommand<object>>()
        .AddClasses(classes => classes.AssignableTo(typeof(ICommand<>)))
        .AsImplementedInterfaces()
        .WithScopedLifetime());
}

// New commands are automatically discovered and registered
```

#### Decorator Pattern for Behavior Extension

```csharp
// Add logging without modifying original commands
services.Decorate(typeof(ICommand<>), typeof(CommandLogger<>));

// Add validation without modifying original commands
services.Decorate(typeof(ICommand<>), typeof(CommandValidator<>));

// Order matters: Validator -> Logger -> Original
```

#### Anti-Pattern: Modifying Existing Classes

```csharp
// WRONG: Adding features by modifying existing class
public class OrderService
{
    public async Task CreateOrderAsync(Order order)
    {
        // Original logic
        await this.repository.SaveAsync(order);

        // Added later - modifies existing code
        await this.emailService.SendConfirmationAsync(order);

        // Added even later - more modifications
        await this.auditService.LogAsync("OrderCreated", order.Id);
    }
}

// CORRECT: Use decorator or event-based extension
public class OrderCreatedNotifier : ICommand<CreateOrderRequest>
{
    private readonly ICommand<CreateOrderRequest> decorated;
    private readonly IEmailService emailService;

    public async Task ExecuteAsync(CreateOrderRequest request)
    {
        await this.decorated.ExecuteAsync(request);
        await this.emailService.SendConfirmationAsync(request.Email);
    }
}
```

---

### 1.3 Liskov Substitution Principle (LSP)

Derived types must be substitutable for their base types without altering program correctness.

#### Interface Hierarchy Design

```csharp
// Base interface defines core contract
public interface ITokenRepository
{
    Task<OAuthToken> ReadAsync(string identifier);
    Task CreateAsync(OAuthToken token);
    Task UpdateAsync(OAuthToken token);
}

// Subtypes maintain the contract, add specificity via marker
public interface IUserTokenRepository : ITokenRepository { }
public interface IServiceTokenRepository : ITokenRepository { }

// Union pattern for type-based selection
public interface ITokenRepositorySelector
{
    ITokenRepository this[string tokenType] { get; }
}

internal class TokenRepositorySelector : ITokenRepositorySelector
{
    private readonly IUserTokenRepository userRepository;
    private readonly IServiceTokenRepository serviceRepository;

    public ITokenRepository this[string tokenType] => tokenType switch
    {
        "user" => this.userRepository,
        "service" => this.serviceRepository,
        _ => throw new ArgumentException($"Unknown token type: {tokenType}")
    };
}
```

#### Proper Inheritance

```csharp
// Base class defines contract and common behavior
public abstract class ApiClientBase
{
    protected readonly HttpClient httpClient;

    protected ApiClientBase(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public abstract Task<T> GetSecureAsync<T>(Uri uri, string accessToken);
}

// Derived class honors the contract
internal class UserApiClient : ApiClientBase
{
    public UserApiClient(HttpClient httpClient) : base(httpClient) { }

    public override async Task<T> GetSecureAsync<T>(Uri uri, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await this.httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>();
    }
}
```

#### Anti-Pattern: Contract Violation

```csharp
// WRONG: Throwing NotImplementedException violates LSP
public class ReadOnlyRepository : IRepository<Entity>
{
    public Task<Entity> GetAsync(string id) => /* works */;

    public Task CreateAsync(Entity entity)
    {
        throw new NotImplementedException(); // Violates base contract
    }

    public Task UpdateAsync(Entity entity)
    {
        throw new NotSupportedException(); // Also violates contract
    }
}

// CORRECT: Use interface segregation instead
public interface IReadRepository<T>
{
    Task<T> GetAsync(string id);
}

public interface IWriteRepository<T>
{
    Task CreateAsync(T entity);
    Task UpdateAsync(T entity);
}

public class ReadOnlyRepository : IReadRepository<Entity>
{
    public Task<Entity> GetAsync(string id) => /* works */;
}
```

---

### 1.4 Interface Segregation Principle (ISP)

Clients should not be forced to depend on interfaces they don't use. Prefer many small interfaces over few large ones.

#### Narrow, Focused Interfaces

```csharp
// Single-method interfaces for maximum flexibility
public interface IDateTimeService
{
    DateTime GetCurrentUtc();
}

public interface IHashService
{
    string ComputeHash(string input);
}

// Focused HTTP client interface
public interface IApiClient
{
    Task<HttpResponseMessage> GetAsync(Uri uri);
    Task<HttpResponseMessage> PostAsync(Uri uri, object content);
    Task<HttpResponseMessage> GetSecureAsync(Uri uri, string token);
    Task<HttpResponseMessage> PostSecureAsync(Uri uri, object content, string token);
}
```

#### Marker Interfaces for Compile-Time Safety

```csharp
// Marker interface provides type safety without forcing method implementations
// This is NOT an anti-pattern when used for compile-time constraints

public interface ICommandParameter { }

public interface IQueryParameter { }

// Generic constraints ensure type safety
public interface ICommand<T> where T : ICommandParameter
{
    Task ExecuteAsync(T parameter);
}

// Compiler prevents incorrect usage
public class CreateUserRequest : ICommandParameter
{
    public string Email { get; set; }
    public string Name { get; set; }
}

// This would fail compilation:
// ICommand<string> command; // string doesn't implement ICommandParameter
```

#### Anti-Pattern: Fat Interfaces

```csharp
// WRONG: Monolithic interface forces unnecessary implementations
public interface IUserService
{
    Task<User> GetByIdAsync(string id);
    Task<User> GetByEmailAsync(string email);
    Task<IEnumerable<User>> SearchAsync(string query);
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(string id);
    Task SendEmailAsync(string userId, string message);
    Task ResetPasswordAsync(string userId);
    Task<bool> ValidateCredentialsAsync(string email, string password);
    Task<IEnumerable<Role>> GetRolesAsync(string userId);
    Task AssignRoleAsync(string userId, string roleId);
}

// CORRECT: Segregated interfaces
public interface IUserReader
{
    Task<User> GetByIdAsync(string id);
    Task<User> GetByEmailAsync(string email);
}

public interface IUserSearchService
{
    Task<IEnumerable<User>> SearchAsync(string query);
}

public interface IUserWriter
{
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(string id);
}

public interface IUserAuthService
{
    Task<bool> ValidateCredentialsAsync(string email, string password);
    Task ResetPasswordAsync(string userId);
}

public interface IUserRoleService
{
    Task<IEnumerable<Role>> GetRolesAsync(string userId);
    Task AssignRoleAsync(string userId, string roleId);
}
```

---

### 1.5 Dependency Inversion Principle (DIP)

High-level modules should not depend on low-level modules. Both should depend on abstractions.

#### Constructor Injection Pattern

```csharp
// All dependencies injected via constructor
internal class OrderService : IOrderService
{
    private readonly IOrderRepository repository;
    private readonly INotificationService notifications;
    private readonly IDateTimeService dateTime;
    private readonly ILogger<OrderService> logger;

    public OrderService(
        IOrderRepository repository,
        INotificationService notifications,
        IDateTimeService dateTime,
        ILogger<OrderService> logger)
    {
        this.repository = repository;
        this.notifications = notifications;
        this.dateTime = dateTime;
        this.logger = logger;
    }

    public async Task<Order> CreateAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CreatedAt = this.dateTime.GetCurrentUtc(),
            Items = request.Items
        };

        await this.repository.SaveAsync(order);
        await this.notifications.SendAsync(request.CustomerEmail, "Order created");

        return order;
    }
}
```

#### Extension Method Registration Pattern

```csharp
// Each layer provides its own registration extension
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IDateTimeService, DateTimeService>()
            .AddSingleton<IHashService, HashService>()
            .AddScoped<IOrderService, OrderService>()
            .AddScoped<INotificationService, NotificationService>();
    }
}

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services
            .AddScoped<IOrderRepository, MongoOrderRepository>()
            .AddScoped<IUserRepository, MongoUserRepository>()
            .AddHttpClient<IExternalApiClient, ExternalApiClient>();
    }
}

// Composition root in Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddDomainServices()
        .AddInfrastructure()
        .AddExternalIntegrations();
}
```

#### DI Precondition Validation

```csharp
public static IServiceCollection AddExternalCalendar(
    this IServiceCollection services,
    CalendarCredentials credentials)
{
    // Validate prerequisites are registered
    services.ValidatePreconditions();

    return services
        .AddSingleton(credentials)
        .AddScoped<ICalendarClient, CalendarClient>()
        .AddCommands()
        .AddQueries();
}

private static void ValidatePreconditions(this IServiceCollection services)
{
    var required = new[] { typeof(ITokenRepository), typeof(ILogger<>) };

    foreach (var type in required)
    {
        if (!services.Any(s => s.ServiceType == type ||
            (type.IsGenericTypeDefinition && s.ServiceType.IsGenericType &&
             s.ServiceType.GetGenericTypeDefinition() == type)))
        {
            throw new InvalidOperationException(
                $"Required service {type.Name} must be registered before calling AddExternalCalendar");
        }
    }
}
```

#### Anti-Pattern: Service Locator

```csharp
// WRONG: Service locator hides dependencies
public class OrderService
{
    private readonly IServiceProvider serviceProvider;

    public OrderService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task CreateAsync(Order order)
    {
        // Hidden dependency - not visible in constructor
        var repository = this.serviceProvider.GetRequiredService<IOrderRepository>();
        await repository.SaveAsync(order);

        // Another hidden dependency
        var notifier = this.serviceProvider.GetRequiredService<INotificationService>();
        await notifier.SendAsync(order.CustomerEmail, "Created");
    }
}

// WRONG: Direct instantiation
public class OrderService
{
    public async Task CreateAsync(Order order)
    {
        // Tight coupling to concrete implementation
        var repository = new SqlOrderRepository(connectionString);
        await repository.SaveAsync(order);
    }
}
```

---

## Part 2: Design Patterns

### 2.1 Command/Query Separation (CQS)

Separate state-changing operations (Commands) from data retrieval (Queries).

#### Command Pattern

```csharp
// Command interface for state-changing operations
public interface ICommand<TParameter> where TParameter : ICommandParameter
{
    Task ExecuteAsync(TParameter parameter);
}

// Parameter objects encapsulate command data
public class CreateEventRequest : ICommandParameter
{
    public string Title { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string CalendarId { get; set; }
}

// Command implementation
internal class CreateEventCommand : ICommand<CreateEventRequest>
{
    private readonly IApiClient client;
    private readonly ApiEndpoints endpoints;

    public CreateEventCommand(IApiClient client, ApiEndpoints endpoints)
    {
        this.client = client;
        this.endpoints = endpoints;
    }

    public async Task ExecuteAsync(CreateEventRequest request)
    {
        ValidateRequest(request);

        var uri = this.endpoints.GetEventsUri(request.CalendarId);
        var body = MapToApiModel(request);

        await this.client.PostSecureAsync(uri, body);
    }

    private static void ValidateRequest(CreateEventRequest request)
    {
        if (request.StartTime >= request.EndTime)
            throw new InvalidOperationException("Start time must be before end time");
    }

    private static object MapToApiModel(CreateEventRequest request) => new
    {
        summary = request.Title,
        start = request.StartTime,
        end = request.EndTime
    };
}
```

#### Query Pattern

```csharp
// Query interface with input and output types
public interface IQuery<TInput, TOutput> where TInput : IQueryParameter
{
    Task<TOutput> QueryAsync(TInput input);
}

// JSON query for raw responses
public interface IJsonQuery<TInput> where TInput : IQueryParameter
{
    Task<string> QueryAsync(TInput input);
}

// Query adapter bridges JSON to typed results
internal abstract class QueryAdapter<TInput, TOutput> : IQuery<TInput, TOutput>
    where TInput : IQueryParameter
{
    private readonly IJsonQuery<TInput> jsonQuery;
    private readonly JsonSerializerOptions options;

    protected QueryAdapter(IJsonQuery<TInput> jsonQuery, JsonSerializerOptions options)
    {
        this.jsonQuery = jsonQuery;
        this.options = options;
    }

    public async Task<TOutput> QueryAsync(TInput input)
    {
        var json = await this.jsonQuery.QueryAsync(input);
        return JsonSerializer.Deserialize<TOutput>(json, this.options);
    }
}
```

---

### 2.2 Repository Pattern

Abstract data access behind a consistent interface with expression-based querying.

#### Generic Repository Base

```csharp
public interface IRepository<TDocument>
{
    Task CreateAsync(TDocument document);
    Task<TDocument> ReadAsync(Expression<Func<TDocument, bool>> predicate);
    Task<IEnumerable<TDocument>> ReadManyAsync(Expression<Func<TDocument, bool>> predicate);
    Task UpdateAsync(Expression<Func<TDocument, bool>> predicate, TDocument document);
    Task<TDocument> DeleteAsync(Expression<Func<TDocument, bool>> predicate);
}

internal class MongoRepository<TDocument> : IRepository<TDocument>
{
    private readonly Lazy<Task<IMongoCollection<TDocument>>> collection;

    public MongoRepository(IMongoContext context, string collectionName)
    {
        this.collection = new Lazy<Task<IMongoCollection<TDocument>>>(
            () => context.GetCollectionAsync<TDocument>(collectionName));
    }

    public async Task<TDocument> ReadAsync(Expression<Func<TDocument, bool>> predicate)
    {
        var col = await this.collection.Value;
        return await col.Find(predicate).FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(Expression<Func<TDocument, bool>> predicate, TDocument document)
    {
        var col = await this.collection.Value;
        var result = await col.ReplaceOneAsync(predicate, document);

        if (result.ModifiedCount == 0)
            throw new UpdateException($"No document matched predicate for {typeof(TDocument).Name}");
    }
}
```

#### Specific Repository with Business Queries

```csharp
public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetPendingOrdersAsync(DateTime since);
    Task<Order> GetByReferenceAsync(string reference);
}

internal class OrderRepository : MongoRepository<Order>, IOrderRepository
{
    public OrderRepository(IMongoContext context)
        : base(context, "orders") { }

    public async Task<IEnumerable<Order>> GetPendingOrdersAsync(DateTime since)
    {
        return await ReadManyAsync(o =>
            o.Status == OrderStatus.Pending &&
            o.CreatedAt >= since);
    }

    public async Task<Order> GetByReferenceAsync(string reference)
    {
        return await ReadAsync(o => o.Reference == reference);
    }
}
```

---

### 2.3 Decorator Pattern

Add behavior to objects without modifying their code. Essential for cross-cutting concerns.

#### Decorator Implementation

```csharp
// Logging decorator
internal class RepositoryLogger<T> : IRepository<T>
{
    private readonly IRepository<T> decorated;
    private readonly ILogger logger;

    public RepositoryLogger(IRepository<T> decorated, ILogger logger)
    {
        this.decorated = decorated;
        this.logger = logger;
    }

    public async Task CreateAsync(T document)
    {
        this.logger.LogInformation("Creating {Type}", typeof(T).Name);
        await this.decorated.CreateAsync(document);
        this.logger.LogInformation("Created {Type}", typeof(T).Name);
    }

    // ... delegate other methods similarly
}

// Audit decorator
internal class AuditedRepository<T> : IRepository<T> where T : IAuditable
{
    private readonly IRepository<T> decorated;
    private readonly IRepository<AuditLog> auditRepository;
    private readonly IDateTimeService dateTime;

    public async Task CreateAsync(T document)
    {
        await this.decorated.CreateAsync(document);

        var audit = new AuditLog
        {
            EntityType = typeof(T).Name,
            Operation = "Create",
            Timestamp = this.dateTime.GetCurrentUtc(),
            Data = JsonSerializer.Serialize(document)
        };

        await this.auditRepository.CreateAsync(audit);
    }
}

// Transaction decorator
internal class TransactionalRepository<T> : IRepository<T>
{
    private readonly IRepository<T> decorated;
    private readonly ITransactionContext transaction;

    public async Task CreateAsync(T document)
    {
        await this.decorated.CreateAsync(document);
        // Operations are enlisted in ambient transaction
    }
}
```

#### Decorator Factory for Layered Composition

```csharp
public interface IRepositoryDecoratorFactory
{
    IRepository<T> CreateDecorated<T>(IRepository<T> repository, DecoratorOptions options);
}

internal class RepositoryDecoratorFactory : IRepositoryDecoratorFactory
{
    private readonly IServiceProvider services;

    public IRepository<T> CreateDecorated<T>(IRepository<T> repository, DecoratorOptions options)
    {
        var decorated = repository;

        // Order matters: innermost first
        if (options.EnableAudit)
            decorated = new AuditedRepository<T>(decorated, /* ... */);

        if (options.EnableTransaction)
            decorated = new TransactionalRepository<T>(decorated, /* ... */);

        if (options.EnableLogging)
            decorated = new RepositoryLogger<T>(decorated, /* ... */);

        return decorated;
    }
}
```

#### DI-Based Decoration with Scrutor

```csharp
public static IServiceCollection AddRepositories(this IServiceCollection services)
{
    return services
        .AddScoped(typeof(IRepository<>), typeof(MongoRepository<>))
        // Decorators applied in order (last = outermost)
        .Decorate(typeof(IRepository<>), typeof(AuditedRepository<>))
        .Decorate(typeof(IRepository<>), typeof(TransactionalRepository<>))
        .Decorate(typeof(IRepository<>), typeof(RepositoryLogger<>));
}
```

---

### 2.4 Builder Pattern

Construct complex objects step-by-step with a fluent API.

#### Scope Builder with Template Method

```csharp
public abstract class ScopeBuilder<TScope> where TScope : Enum
{
    private readonly List<TScope> scopes = new();

    public ScopeBuilder<TScope> Add(TScope scope)
    {
        this.scopes.Add(scope);
        return this;
    }

    public ScopeBuilder<TScope> AddRange(params TScope[] scopes)
    {
        this.scopes.AddRange(scopes);
        return this;
    }

    public string Build()
    {
        return string.Join(' ', this.scopes.Select(Normalize));
    }

    protected abstract string Normalize(TScope scope);
}

public class OAuthScopeBuilder : ScopeBuilder<OAuthScope>
{
    protected override string Normalize(OAuthScope scope)
    {
        return scope switch
        {
            OAuthScope.ReadCalendar => "calendar.read",
            OAuthScope.WriteCalendar => "calendar.write",
            OAuthScope.ReadProfile => "profile.read",
            _ => scope.ToString().ToLowerInvariant()
        };
    }
}

// Usage
var scopes = new OAuthScopeBuilder()
    .Add(OAuthScope.ReadCalendar)
    .Add(OAuthScope.WriteCalendar)
    .Build(); // "calendar.read calendar.write"
```

#### Query String Builder

```csharp
public class QueryStringBuilder
{
    private readonly List<KeyValuePair<string, string>> parameters = new();

    public QueryStringBuilder Append(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
            this.parameters.Add(new KeyValuePair<string, string>(key, value));
        return this;
    }

    public QueryStringBuilder Append(string key, DateTime? value, string format = "yyyy-MM-ddTHH:mm:ssZ")
    {
        if (value.HasValue)
            Append(key, value.Value.ToString(format));
        return this;
    }

    public QueryStringBuilder Append(string key, IEnumerable<string> values)
    {
        foreach (var value in values)
            Append($"{key}[]", value);
        return this;
    }

    public Uri Build(Uri baseUri)
    {
        if (!this.parameters.Any())
            return baseUri;

        var query = string.Join("&", this.parameters
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        return new Uri($"{baseUri}?{query}");
    }
}

// Usage
var uri = new QueryStringBuilder()
    .Append("from", DateTime.UtcNow)
    .Append("status", new[] { "pending", "active" })
    .Append("limit", "100")
    .Build(new Uri("https://api.example.com/orders"));
```

---

### 2.5 Chain of Responsibility

Pass requests along a chain of handlers until one handles it.

#### Exception Handler Chain

```csharp
public interface IExceptionHandler
{
    Task<bool> HandleAsync(Exception exception, HttpResponse response);
}

internal abstract class BaseExceptionHandler : IExceptionHandler
{
    public abstract string ErrorCode { get; }
    public abstract int StatusCode { get; }
    public abstract string Message { get; }

    protected abstract bool CanHandle(Exception exception);

    public async Task<bool> HandleAsync(Exception exception, HttpResponse response)
    {
        if (!CanHandle(exception))
            return false;

        response.StatusCode = StatusCode;
        response.ContentType = "application/json";

        var error = new { message = Message, code = ErrorCode };
        await response.WriteAsJsonAsync(error);

        return true;
    }
}

// Specific handlers
internal class NotFoundExceptionHandler : BaseExceptionHandler
{
    public override string ErrorCode => "NOT_FOUND";
    public override int StatusCode => 404;
    public override string Message => "The requested resource was not found";

    protected override bool CanHandle(Exception ex) => ex is NotFoundException;
}

internal class ValidationExceptionHandler : BaseExceptionHandler
{
    public override string ErrorCode => "VALIDATION_ERROR";
    public override int StatusCode => 400;
    public override string Message => "Validation failed";

    protected override bool CanHandle(Exception ex) => ex is ValidationException;
}

internal class DefaultExceptionHandler : BaseExceptionHandler
{
    public override string ErrorCode => "INTERNAL_ERROR";
    public override int StatusCode => 500;
    public override string Message => "An unexpected error occurred";

    protected override bool CanHandle(Exception ex) => true; // Catch-all
}
```

#### Handler Chain Execution

```csharp
internal class ExceptionHandlerChain
{
    private readonly IEnumerable<IExceptionHandler> handlers;

    public ExceptionHandlerChain()
    {
        // Order matters - most specific first, default last
        this.handlers = new IExceptionHandler[]
        {
            new TokenExpiredExceptionHandler(),
            new NotFoundExceptionHandler(),
            new ValidationExceptionHandler(),
            new UnauthorizedExceptionHandler(),
            new DefaultExceptionHandler()
        };
    }

    public async Task HandleAsync(Exception exception, HttpResponse response)
    {
        foreach (var handler in this.handlers)
        {
            if (await handler.HandleAsync(exception, response))
                return;
        }
    }
}
```

---

### 2.6 Strategy Pattern

Define a family of algorithms and make them interchangeable.

#### Token Repository Selection

```csharp
public interface ITokenRepositoryStrategy
{
    ITokenRepository GetRepository(string tokenType);
}

internal class TokenRepositoryStrategy : ITokenRepositoryStrategy
{
    private readonly IUserTokenRepository userRepository;
    private readonly IServiceTokenRepository serviceRepository;

    public TokenRepositoryStrategy(
        IUserTokenRepository userRepository,
        IServiceTokenRepository serviceRepository)
    {
        this.userRepository = userRepository;
        this.serviceRepository = serviceRepository;
    }

    public ITokenRepository GetRepository(string tokenType)
    {
        return tokenType switch
        {
            "user" or "delegated" => this.userRepository,
            "service" or "application" => this.serviceRepository,
            _ => throw new ArgumentException($"Unknown token type: {tokenType}")
        };
    }
}
```

#### Notification Provider Strategy

```csharp
public interface INotificationProvider
{
    Task SendAsync(NotificationRequest request);
    bool CanHandle(NotificationType type);
}

internal class EmailProvider : INotificationProvider
{
    public bool CanHandle(NotificationType type) => type == NotificationType.Email;

    public async Task SendAsync(NotificationRequest request)
    {
        // Email-specific implementation
    }
}

internal class SmsProvider : INotificationProvider
{
    public bool CanHandle(NotificationType type) => type == NotificationType.Sms;

    public async Task SendAsync(NotificationRequest request)
    {
        // SMS-specific implementation
    }
}

internal class NotificationService : INotificationService
{
    private readonly IEnumerable<INotificationProvider> providers;

    public async Task SendAsync(NotificationType type, NotificationRequest request)
    {
        var provider = this.providers.FirstOrDefault(p => p.CanHandle(type))
            ?? throw new NotSupportedException($"No provider for {type}");

        await provider.SendAsync(request);
    }
}
```

---

## Part 3: Layer-Specific Guidelines

### 3.1 API Client Layer

Guidelines for building robust external API integrations.

#### HttpClientFactory Integration

```csharp
// Registration with named/typed client
public static IServiceCollection AddExternalApi(this IServiceCollection services)
{
    services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
    {
        client.BaseAddress = new Uri("https://api.external.com/v1/");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddRetryPolicy()
    .AddCircuitBreakerPolicy();

    return services;
}

// Client implementation receives typed HttpClient
internal class ExternalApiClient : IExternalApiClient
{
    private readonly HttpClient client;

    public ExternalApiClient(HttpClient client)
    {
        this.client = client;
    }
}
```

#### Token Lifecycle Management

```csharp
internal class SecureApiClient
{
    private readonly ITokenRepository tokenRepository;
    private readonly ITokenRefreshService refreshService;
    private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromMinutes(3);

    public async Task<T> GetSecureAsync<T>(Uri uri, string userId)
    {
        var token = await GetValidTokenAsync(userId);

        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await this.client.SendAsync(request);
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<OAuthToken> GetValidTokenAsync(string userId)
    {
        var token = await this.tokenRepository.ReadAsync(userId);

        // Refresh before actual expiration (buffer for network latency)
        if (token.ExpiresAt - ExpirationBuffer <= DateTime.UtcNow)
        {
            token = await this.refreshService.RefreshAsync(token);
            await this.tokenRepository.UpdateAsync(token);
        }

        return token;
    }
}
```

#### URI Construction

```csharp
internal sealed class ApiEndpoints
{
    private readonly Uri baseUri;

    public ApiEndpoints(string dataCenter)
    {
        this.baseUri = new Uri($"https://api{dataCenter}.example.com/v1/");
    }

    public Uri CalendarsUri => CreateUri("calendars");
    public Uri EventsUri => CreateUri("events");

    public Uri GetEventUri(string calendarId, string eventId)
    {
        return CreateUri($"calendars/{calendarId}/events/{eventId}");
    }

    private Uri CreateUri(string relative)
    {
        return new Uri(this.baseUri, new Uri(relative, UriKind.Relative));
    }
}
```

#### JSON Serialization Configuration

```csharp
internal static class ApiJsonOptions
{
    public static readonly JsonSerializerOptions Options;

    static ApiJsonOptions()
    {
        Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        Options.Converters.Add(new DateTimeConverter());
        Options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }
}

internal class DateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ssZ";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
    }
}
```

#### Anti-Patterns

```csharp
// WRONG: Raw HttpClient instantiation
public class BadApiClient
{
    public async Task<string> GetAsync(string url)
    {
        using var client = new HttpClient(); // Creates new handler each time
        return await client.GetStringAsync(url);
    }
}

// WRONG: Hardcoded URLs
public class BadApiClient
{
    public async Task<User> GetUserAsync(string id)
    {
        return await client.GetFromJsonAsync<User>(
            $"https://api.prod.example.com/users/{id}"); // Hardcoded!
    }
}

// WRONG: No token refresh
public class BadSecureClient
{
    public async Task<T> GetAsync<T>(string path)
    {
        var token = await this.tokenRepo.GetAsync(userId);
        // Token might be expired - no validation!
        return await client.GetFromJsonAsync<T>(path);
    }
}
```

---

### 3.2 Service Layer

Guidelines for business logic organization and orchestration.

#### Service Lifetime Management

```csharp
public static IServiceCollection AddServices(this IServiceCollection services)
{
    return services
        // Singleton: stateless utilities, thread-safe, application-wide
        .AddSingleton<IDateTimeService, DateTimeService>()
        .AddSingleton<IHashService, HashService>()

        // Scoped: business services with request-scoped state or dependencies
        .AddScoped<IOrderService, OrderService>()
        .AddScoped<IUserService, UserService>()

        // Transient: lightweight, stateless, new instance per injection
        .AddTransient<IValidationService, ValidationService>();
}
```

#### Business Logic Orchestration

```csharp
internal class OrderService : IOrderService
{
    public async Task<Order> CreateOrderAsync(string customerEmail, CreateOrderRequest request)
    {
        // Step 1: Validation
        ValidateRequest(request);

        // Step 2: Build entity
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerEmail = customerEmail,
            Items = request.Items,
            Status = OrderStatus.Pending,
            CreatedAt = this.dateTime.GetCurrentUtc()
        };

        // Step 3: Calculate derived data
        order.TotalAmount = CalculateTotal(order.Items);

        // Step 4: Optional features
        if (request.EnableDeliveryTracking)
        {
            order.TrackingId = await this.trackingService.CreateAsync(order);
        }

        // Step 5: Persist
        await this.repository.CreateAsync(order);

        // Step 6: Side effects (async notifications)
        await this.notifications.QueueAsync(new OrderCreatedNotification(order));

        return order;
    }

    private static void ValidateRequest(CreateOrderRequest request)
    {
        if (request.Items == null || !request.Items.Any())
            throw new ValidationException("Order must contain at least one item");

        if (request.Items.Any(i => i.Quantity <= 0))
            throw new ValidationException("Item quantity must be positive");
    }
}
```

#### Validation Patterns

```csharp
// Pattern 1: Return validation results (for forms, batch operations)
public interface IValidationService
{
    IEnumerable<ValidationResult> Validate(UserProfile profile);
}

internal class UserValidationService : IValidationService
{
    public IEnumerable<ValidationResult> Validate(UserProfile profile)
    {
        var results = new List<ValidationResult>();

        results.Add(ValidateName(profile.FirstName, "FirstName"));
        results.Add(ValidateName(profile.LastName, "LastName"));
        results.Add(ValidateEmail(profile.Email));
        results.Add(ValidatePhone(profile.Phone));

        return results.Where(r => !r.IsValid);
    }

    private static ValidationResult ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return ValidationResult.Error("Email", "Email is required");

        if (!Regex.IsMatch(email, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$"))
            return ValidationResult.Error("Email", "Invalid email format");

        return ValidationResult.Success("Email");
    }
}

// Pattern 2: Throw exceptions (for business rule violations)
internal class OrderService
{
    public async Task CreateAsync(CreateOrderRequest request)
    {
        if (request.DeliveryDate <= DateTimeOffset.UtcNow)
            throw new PastDateException(request.DeliveryDate);

        if (request.StartDate > request.EndDate)
            throw new DateRangeException(request.StartDate, request.EndDate);
    }
}
```

#### State Machine Pattern

```csharp
public enum OrderState
{
    Draft,
    Submitted,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

internal class OrderStateService : IOrderStateService
{
    private static readonly Dictionary<OrderState, OrderState[]> AllowedTransitions = new()
    {
        [OrderState.Draft] = new[] { OrderState.Submitted, OrderState.Cancelled },
        [OrderState.Submitted] = new[] { OrderState.Processing, OrderState.Cancelled },
        [OrderState.Processing] = new[] { OrderState.Shipped, OrderState.Cancelled },
        [OrderState.Shipped] = new[] { OrderState.Delivered },
        [OrderState.Delivered] = Array.Empty<OrderState>(),
        [OrderState.Cancelled] = Array.Empty<OrderState>()
    };

    public async Task TransitionAsync(string orderId, OrderState targetState)
    {
        var order = await this.repository.GetAsync(orderId)
            ?? throw new OrderNotFoundException(orderId);

        if (!AllowedTransitions[order.State].Contains(targetState))
        {
            throw new InvalidStateTransitionException(order.State, targetState);
        }

        order.State = targetState;
        order.StateChangedAt = this.dateTime.GetCurrentUtc();

        await this.repository.UpdateAsync(order);
        await this.events.PublishAsync(new OrderStateChanged(orderId, targetState));
    }
}
```

#### Anti-Patterns

```csharp
// WRONG: Business logic in controller
[HttpPost]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
{
    // All this belongs in a service!
    if (request.Items.Count == 0)
        return BadRequest("No items");

    var order = new Order { /* ... */ };
    await this.dbContext.Orders.AddAsync(order);
    await this.dbContext.SaveChangesAsync();

    await this.emailService.SendAsync(order.CustomerEmail, "Order created");

    return Ok(order);
}

// WRONG: Blocking async code
public Order GetOrder(string id)
{
    return this.repository.GetAsync(id).Result; // Deadlock risk!
}

// WRONG: Fire-and-forget without tracking
public async Task CreateAsync(Order order)
{
    await this.repository.SaveAsync(order);

    // If this fails, we'll never know
    _ = Task.Run(() => this.emailService.SendAsync(order.Email, "Created"));
}
```

---

### 3.3 Repository/Data Access Layer

Guidelines for data persistence and transaction management.

#### Lazy Collection Initialization

```csharp
internal class MongoRepository<T> : IRepository<T>
{
    private readonly Lazy<Task<IMongoCollection<T>>> collection;

    public MongoRepository(IMongoContext context, string collectionName)
    {
        // Defer connection until first actual use
        this.collection = new Lazy<Task<IMongoCollection<T>>>(
            () => context.GetCollectionAsync<T>(collectionName),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<T> GetAsync(string id)
    {
        var col = await this.collection.Value;
        return await col.Find(x => x.Id == id).FirstOrDefaultAsync();
    }
}
```

#### Transaction Management with Retry

```csharp
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}

internal class MongoTransaction : ITransaction
{
    private readonly IClientSessionHandle session;
    private readonly CancellationToken cancellationToken;
    private const int MaxRetries = 3;

    public async Task CommitAsync()
    {
        var attempt = 0;

        while (true)
        {
            try
            {
                await this.session.CommitTransactionAsync(this.cancellationToken);
                return;
            }
            catch (MongoException ex) when (
                ex.HasErrorLabel("UnknownTransactionCommitResult") &&
                ++attempt < MaxRetries)
            {
                // Transient error - retry with backoff
                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), this.cancellationToken);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (this.session.IsInTransaction)
        {
            await this.session.AbortTransactionAsync(this.cancellationToken);
        }
        this.session.Dispose();
    }
}
```

#### Audit Trail Implementation

```csharp
internal class AuditedRepository<T> : IRepository<T> where T : class
{
    private readonly IRepository<T> decorated;
    private readonly IRepository<AuditEntry> auditRepository;
    private readonly IUserContext userContext;
    private readonly IDateTimeService dateTime;

    public async Task CreateAsync(T entity)
    {
        await this.decorated.CreateAsync(entity);
        await CreateAuditEntryAsync("Create", entity);
    }

    public async Task UpdateAsync(Expression<Func<T, bool>> predicate, T entity)
    {
        var original = await this.decorated.ReadAsync(predicate);
        await this.decorated.UpdateAsync(predicate, entity);
        await CreateAuditEntryAsync("Update", entity, original);
    }

    public async Task DeleteAsync(Expression<Func<T, bool>> predicate)
    {
        var original = await this.decorated.ReadAsync(predicate);
        await this.decorated.DeleteAsync(predicate);
        await CreateAuditEntryAsync("Delete", original);
    }

    private async Task CreateAuditEntryAsync(string operation, T current, T previous = null)
    {
        var entry = new AuditEntry
        {
            EntityType = typeof(T).Name,
            Operation = operation,
            Timestamp = this.dateTime.GetCurrentUtc(),
            UserId = this.userContext.UserId,
            CurrentState = JsonSerializer.Serialize(current),
            PreviousState = previous != null ? JsonSerializer.Serialize(previous) : null
        };

        await this.auditRepository.CreateAsync(entry);
    }
}
```

#### Cancellation Token Propagation

```csharp
internal class CancellableRepository<T> : IRepository<T>
{
    private readonly IRepository<T> decorated;
    private readonly CancellationTokenSource tokenSource;

    public CancellableRepository(
        IRepository<T> decorated,
        IHttpContextAccessor httpContextAccessor)
    {
        this.decorated = decorated;

        // Link to HTTP request cancellation
        var requestToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        this.tokenSource = CancellationTokenSource.CreateLinkedTokenSource(requestToken);
    }

    public async Task<T> GetAsync(string id)
    {
        this.tokenSource.Token.ThrowIfCancellationRequested();
        return await this.decorated.GetAsync(id);
    }
}
```

#### Anti-Patterns

```csharp
// WRONG: Business logic in repository
internal class OrderRepository
{
    public async Task CreateOrderAsync(Order order)
    {
        // Validation belongs in service layer!
        if (order.Total < 0)
            throw new ArgumentException("Invalid total");

        // Email sending belongs in service layer!
        await this.emailService.SendAsync(order.Email, "Created");

        await this.collection.InsertOneAsync(order);
    }
}

// WRONG: Missing transaction handling
public async Task TransferAsync(string fromId, string toId, decimal amount)
{
    var from = await this.accounts.GetAsync(fromId);
    var to = await this.accounts.GetAsync(toId);

    from.Balance -= amount;
    to.Balance += amount;

    // If second update fails, data is inconsistent!
    await this.accounts.UpdateAsync(from);
    await this.accounts.UpdateAsync(to);
}

// WRONG: Bypassing repository
internal class OrderService
{
    private readonly IMongoDatabase database; // Direct DB access!

    public async Task UpdateStatusAsync(string id, string status)
    {
        var collection = this.database.GetCollection<Order>("orders");
        await collection.UpdateOneAsync(/* ... */);
    }
}
```

---

### 3.4 Domain Layer

Guidelines for entity and value object design.

#### Entity Design

```csharp
// Simple POCO with audit support
public class Order
{
    public string Id { get; set; }
    public string CustomerEmail { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }

    // Audit properties
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string CreatedBy { get; set; }
    public string ModifiedBy { get; set; }
}

// Marker interface for audit-enabled entities
public interface IAuditable { }

public class AuditableOrder : Order, IAuditable { }
```

#### Value Objects

```csharp
// Immutable value object
public sealed class DateRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public DateRange(DateTime start, DateTime end)
    {
        if (start > end)
            throw new ArgumentException("Start must be before or equal to end");

        Start = start;
        End = end;
    }

    public bool Contains(DateTime date) => date >= Start && date <= End;

    public bool Overlaps(DateRange other) =>
        Start <= other.End && End >= other.Start;

    public override bool Equals(object obj) =>
        obj is DateRange other && Start == other.Start && End == other.End;

    public override int GetHashCode() => HashCode.Combine(Start, End);
}

// Address as value object
public sealed class Address
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    public Address(string street, string city, string postalCode, string country)
    {
        Street = street ?? throw new ArgumentNullException(nameof(street));
        City = city ?? throw new ArgumentNullException(nameof(city));
        PostalCode = postalCode ?? throw new ArgumentNullException(nameof(postalCode));
        Country = country ?? throw new ArgumentNullException(nameof(country));
    }
}
```

#### Interface Definitions

```csharp
// Domain interfaces live in Domain layer, implementations elsewhere

// Service interfaces
public interface IOrderService
{
    Task<Order> CreateAsync(string customerEmail, CreateOrderRequest request);
    Task<Order> GetAsync(string id);
    Task CancelAsync(string id);
}

// Repository interfaces
public interface IOrderRepository
{
    Task CreateAsync(Order order);
    Task<Order> GetAsync(string id);
    Task<Order> GetByReferenceAsync(string reference);
    Task UpdateAsync(Order order);
    Task<IEnumerable<Order>> GetPendingAsync(DateTime since);
}

// External service interfaces
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessAsync(PaymentRequest request);
    Task<RefundResult> RefundAsync(string transactionId, decimal amount);
}
```

#### Anti-Patterns

```csharp
// WRONG: Heavy behavior in entity (in this architectural style)
public class Order
{
    private readonly IEmailService emailService; // Dependency in entity!

    public async Task Submit()
    {
        this.Status = OrderStatus.Submitted;
        await this.emailService.SendAsync(this.Email, "Submitted"); // Side effect!
    }
}

// WRONG: Missing null validation in value objects
public class Money
{
    public decimal Amount { get; set; } // Mutable!
    public string Currency { get; set; } // No validation!
}
```

---

### 3.5 Web/Controller Layer

Guidelines for API endpoints and middleware.

#### Policy-Based Authorization

```csharp
// Define policies in Startup
public void ConfigureServices(IServiceCollection services)
{
    services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireMfa", policy =>
            policy.RequireRole("User")
                  .RequireClaim("mfa_passed", "true"));

        options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy("OrderAccess", policy =>
            policy.Requirements.Add(new OrderAccessRequirement()));
    });
}

// Apply to controllers
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireMfa")]
public class OrdersController : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize(Policy = "OrderAccess")]
    public async Task<ActionResult<Order>> GetOrder(string id)
    {
        var order = await this.orderService.GetAsync(id);
        return order == null ? NotFound() : Ok(order);
    }
}
```

#### Claims Extraction via Extensions

```csharp
public static class HttpRequestExtensions
{
    public static string GetUserEmail(this HttpRequest request)
    {
        return request.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new UnauthorizedAccessException("Email claim not found");
    }

    public static string GetUserId(this HttpRequest request)
    {
        return request.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User ID claim not found");
    }

    public static bool HasRole(this HttpRequest request, string role)
    {
        return request.HttpContext.User.IsInRole(role);
    }
}

// Usage in controller
[HttpPost]
public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
{
    var email = Request.GetUserEmail();
    var order = await this.orderService.CreateAsync(email, request);
    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
}
```

#### Exception Handling Middleware

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;
    private readonly IExceptionHandlerChain handlerChain;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IExceptionHandlerChain handlerChain)
    {
        this.next = next;
        this.logger = logger;
        this.handlerChain = handlerChain;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await this.next(context);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            await this.handlerChain.HandleAsync(ex, context.Response);
        }
    }
}

// Registration
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

#### Transaction Middleware

```csharp
public class TransactionMiddleware
{
    private readonly RequestDelegate next;

    public async Task InvokeAsync(HttpContext context, ITransactionFactory transactionFactory)
    {
        // Only wrap mutating requests in transactions
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method))
        {
            await this.next(context);
            return;
        }

        await using var transaction = await transactionFactory.BeginAsync(
            context.RequestAborted);

        try
        {
            await this.next(context);

            if (context.Response.StatusCode < 400)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

#### Anti-Patterns

```csharp
// WRONG: Authorization logic in controller
[HttpGet("{id}")]
public async Task<ActionResult<Order>> GetOrder(string id)
{
    var order = await this.orderService.GetAsync(id);

    // Should use authorization policy/handler!
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (order.CustomerId != userId && !User.IsInRole("Admin"))
        return Forbid();

    return Ok(order);
}

// WRONG: Direct service instantiation
[HttpPost]
public async Task<IActionResult> Create([FromBody] Request request)
{
    var service = new OrderService(new OrderRepository()); // No DI!
    await service.CreateAsync(request);
    return Ok();
}

// WRONG: Missing Swagger documentation
[HttpPost]
public async Task<IActionResult> Create([FromBody] Request request)
{
    // No attributes describing the endpoint
    return Ok();
}

// CORRECT: Proper documentation
[HttpPost]
[ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<Order>> Create([FromBody] CreateOrderRequest request)
{
    var order = await this.orderService.CreateAsync(Request.GetUserEmail(), request);
    return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
}
```

---

## Part 4: Clean Code Practices

### 4.1 Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase with domain terminology | `OrderService`, `UserRepository` |
| Interfaces | PascalCase with `I` prefix | `IOrderService`, `IUserRepository` |
| Methods | PascalCase action verbs | `GetOrderAsync`, `ValidateUser` |
| Async methods | Suffix with `Async` | `CreateAsync`, `FetchDataAsync` |
| Properties | PascalCase | `FirstName`, `TotalAmount` |
| Private fields | Prefix with `this.` | `this.repository`, `this.logger` |
| Constants | SCREAMING_SNAKE_CASE | `MAX_RETRY_COUNT`, `DEFAULT_TIMEOUT` |
| Parameters | camelCase | `orderId`, `customerEmail` |
| Local variables | camelCase | `result`, `filteredItems` |
| Type parameters | Single letter or descriptive | `T`, `TEntity`, `TResponse` |

#### Request/Response Naming

```csharp
// Request objects: [Action][Entity]Request
public class CreateOrderRequest { }
public class UpdateUserRequest { }
public class SearchProductsRequest { }

// Response objects: [Entity]Response or [Action][Entity]Response
public class OrderResponse { }
public class UserProfileResponse { }
public class SearchResultsResponse { }

// Command parameters: Prefix indicating source/purpose
public class CrCreateEvent { }  // Cr = Cronofy Request
public class DbUserRecord { }   // Db = Database Record
```

---

### 4.2 Method Organization

#### Method Size Guidelines

- Target: 5-15 lines per method
- Maximum: 20 lines (extract if longer)
- Single level of abstraction per method

```csharp
// Good: Focused method with clear flow
public async Task<Order> CreateOrderAsync(string customerEmail, CreateOrderRequest request)
{
    ValidateRequest(request);

    var order = BuildOrder(customerEmail, request);

    await this.repository.CreateAsync(order);
    await this.notifications.QueueOrderCreatedAsync(order);

    return order;
}

private static void ValidateRequest(CreateOrderRequest request)
{
    if (request.Items == null || !request.Items.Any())
        throw new ValidationException("Order must have at least one item");
}

private Order BuildOrder(string customerEmail, CreateOrderRequest request)
{
    return new Order
    {
        Id = Guid.NewGuid().ToString(),
        CustomerEmail = customerEmail,
        Items = request.Items.Select(MapItem).ToList(),
        Status = OrderStatus.Pending,
        CreatedAt = this.dateTime.GetCurrentUtc()
    };
}
```

#### Class Organization

```csharp
public class OrderService : IOrderService
{
    // 1. Constants
    private const int MaxRetries = 3;

    // 2. Private fields
    private readonly IOrderRepository repository;
    private readonly INotificationService notifications;
    private readonly ILogger<OrderService> logger;

    // 3. Constructor
    public OrderService(
        IOrderRepository repository,
        INotificationService notifications,
        ILogger<OrderService> logger)
    {
        this.repository = repository;
        this.notifications = notifications;
        this.logger = logger;
    }

    // 4. Public methods (interface implementations)
    public async Task<Order> CreateAsync(CreateOrderRequest request) { }
    public async Task<Order> GetAsync(string id) { }
    public async Task CancelAsync(string id) { }

    // 5. Private helper methods
    private static void ValidateRequest(CreateOrderRequest request) { }
    private Order BuildOrder(CreateOrderRequest request) { }
}
```

#### Early Return / Guard Clauses

```csharp
// Prefer guard clauses over nested conditionals
public async Task<Order> GetOrderAsync(string id, string userId)
{
    if (string.IsNullOrEmpty(id))
        throw new ArgumentException("Order ID is required", nameof(id));

    var order = await this.repository.GetAsync(id);

    if (order == null)
        throw new OrderNotFoundException(id);

    if (order.CustomerId != userId)
        throw new UnauthorizedAccessException("Access denied to order");

    return order;
}

// Avoid
public async Task<Order> GetOrderAsync(string id, string userId)
{
    if (!string.IsNullOrEmpty(id))
    {
        var order = await this.repository.GetAsync(id);
        if (order != null)
        {
            if (order.CustomerId == userId)
            {
                return order;
            }
            throw new UnauthorizedAccessException();
        }
        throw new OrderNotFoundException(id);
    }
    throw new ArgumentException();
}
```

---

### 4.3 Async Patterns

#### Task vs ValueTask

```csharp
// Use Task for most async operations
public async Task<Order> GetOrderAsync(string id)
{
    return await this.repository.GetAsync(id);
}

// Use ValueTask for hot paths or when result is often synchronous
public async ValueTask<CachedData> GetCachedAsync(string key)
{
    if (this.cache.TryGetValue(key, out var cached))
        return cached; // Synchronous return - no allocation

    return await LoadFromSourceAsync(key);
}

// Use ValueTask for channel operations
public async ValueTask<Message> GetNextMessageAsync()
{
    return await this.channel.Reader.ReadAsync();
}
```

#### ConfigureAwait Usage

```csharp
// Library code: use ConfigureAwait(false) to avoid deadlocks
public async Task<T> FetchExternalDataAsync<T>(string url)
{
    var response = await this.httpClient.GetAsync(url).ConfigureAwait(false);
    return await response.Content.ReadFromJsonAsync<T>().ConfigureAwait(false);
}

// Application code (ASP.NET Core): ConfigureAwait(false) not needed
// but doesn't hurt
public async Task<Order> CreateAsync(CreateOrderRequest request)
{
    var order = MapToOrder(request);
    await this.repository.SaveAsync(order);
    return order;
}
```

#### IAsyncEnumerable for Streaming

```csharp
public async IAsyncEnumerable<Event> GetEventsAsync(
    DateTime since,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var page = 1;
    bool hasMore;

    do
    {
        var response = await this.client.GetEventsPageAsync(since, page, cancellationToken);

        foreach (var evt in response.Events)
        {
            yield return evt;
        }

        hasMore = response.HasNextPage;
        page++;
    }
    while (hasMore);
}

// Consumption
await foreach (var evt in service.GetEventsAsync(since, cancellationToken))
{
    await ProcessEventAsync(evt);
}
```

#### Async All the Way

```csharp
// WRONG: Blocking on async
public Order GetOrder(string id)
{
    return this.repository.GetAsync(id).Result; // Deadlock risk!
}

public Order GetOrder(string id)
{
    return this.repository.GetAsync(id).GetAwaiter().GetResult(); // Still bad!
}

// CORRECT: Async all the way up
public async Task<Order> GetOrderAsync(string id)
{
    return await this.repository.GetAsync(id);
}

// If you MUST call async from sync (rare), use a dedicated thread
public Order GetOrderSync(string id)
{
    return Task.Run(() => this.repository.GetAsync(id)).GetAwaiter().GetResult();
}
```

---

### 4.4 Error Handling

#### Domain-Specific Exception Hierarchy

```csharp
// Base domain exception
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }

    protected DomainException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

// Specific exceptions with factory constructors
public class OrderNotFoundException : DomainException
{
    public string OrderId { get; }

    public OrderNotFoundException(string orderId)
        : base("ORDER_NOT_FOUND", $"Order with ID '{orderId}' was not found")
    {
        OrderId = orderId;
    }
}

public class InvalidDateRangeException : DomainException
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public InvalidDateRangeException(DateTime start, DateTime end)
        : base("INVALID_DATE_RANGE", $"Start date {start:O} must be before end date {end:O}")
    {
        Start = start;
        End = end;
    }

    // Convenience constructor for DateTimeOffset
    public InvalidDateRangeException(DateTimeOffset start, DateTimeOffset end)
        : this(start.UtcDateTime, end.UtcDateTime) { }
}
```

#### Retry Patterns with Polly

```csharp
public static IHttpClientBuilder AddRetryPolicy(this IHttpClientBuilder builder)
{
    return builder.AddPolicyHandler(
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(10)
            },
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                // Log retry attempt
            }));
}

// Manual retry for specific operations
public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
{
    var policy = Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    return await policy.ExecuteAsync(operation);
}
```

#### Exception Unwrapping for Logging

```csharp
public static class ExceptionExtensions
{
    public static string GetFullMessage(this Exception exception)
    {
        var messages = new List<string>();
        var current = exception;

        while (current != null)
        {
            messages.Add(current.Message);
            current = current.InnerException;
        }

        return string.Join(" -> ", messages);
    }

    public static Exception GetRootCause(this Exception exception)
    {
        while (exception.InnerException != null)
            exception = exception.InnerException;

        return exception;
    }
}

// Usage in logging
catch (Exception ex)
{
    this.logger.LogError(ex,
        "Operation failed: {Message}. Root cause: {RootCause}",
        ex.Message,
        ex.GetRootCause().Message);
    throw;
}
```

---

### 4.5 Null Handling

#### Constructor Validation

```csharp
public class OrderService : IOrderService
{
    private readonly IOrderRepository repository;
    private readonly ILogger<OrderService> logger;

    public OrderService(IOrderRepository repository, ILogger<OrderService> logger)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}

// Value objects with null checks
public sealed class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative");

        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }
}
```

#### Null Coalescing and Propagation

```csharp
// Null coalescing operator
var name = user.DisplayName ?? user.Email ?? "Unknown";

// Null conditional operator
var city = order?.ShippingAddress?.City;

// Null coalescing assignment
order.Notes ??= new List<string>();

// Pattern matching with null check
if (result is { IsSuccess: true, Data: var data })
{
    ProcessData(data);
}

// Throw if null
var order = await this.repository.GetAsync(id)
    ?? throw new OrderNotFoundException(id);
```

#### Defensive Programming at Boundaries

```csharp
// Public API boundaries should validate inputs
public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
{
    // Validate at system boundary
    ArgumentNullException.ThrowIfNull(request);

    if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        throw new ValidationException("Customer email is required");

    if (request.Items == null || request.Items.Count == 0)
        throw new ValidationException("At least one item is required");

    // Internal code can trust validated data
    return await ProcessOrderAsync(request);
}

// Internal methods can be more trusting
private async Task<Order> ProcessOrderAsync(CreateOrderRequest request)
{
    // No null checks needed - boundary already validated
    var order = new Order
    {
        CustomerEmail = request.CustomerEmail,
        Items = request.Items.Select(MapItem).ToList()
    };

    return order;
}
```

---

## Part 5: Infrastructure Patterns

### 5.1 Dependency Injection Setup

#### Extension Method Pattern

```csharp
// Each layer provides its own extension method
public static class DomainServiceExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IDateTimeService, DateTimeService>()
            .AddSingleton<IHashService, HashService>()
            .AddScoped<IOrderService, OrderService>()
            .AddScoped<IUserService, UserService>();
    }
}

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection("Database").Get<DatabaseSettings>();

        return services
            .AddSingleton(settings)
            .AddScoped<IOrderRepository, MongoOrderRepository>()
            .AddScoped<IUserRepository, MongoUserRepository>();
    }
}

public static class ExternalIntegrationExtensions
{
    public static IServiceCollection AddCalendarIntegration(
        this IServiceCollection services,
        CalendarCredentials credentials)
    {
        services.EnsurePreconditions();

        return services
            .AddSingleton(credentials)
            .AddScoped<ICalendarClient, CalendarClient>()
            .AddCommands()
            .AddQueries()
            .DecorateWithLogging();
    }

    private static void EnsurePreconditions(this IServiceCollection services)
    {
        if (!services.Any(s => s.ServiceType == typeof(ITokenRepository)))
            throw new InvalidOperationException("ITokenRepository must be registered first");
    }
}
```

#### Lifetime Management Guidelines

```csharp
services
    // SINGLETON: Stateless, thread-safe, expensive to create
    .AddSingleton<IDateTimeService, DateTimeService>()
    .AddSingleton<IHashService, HashService>()
    .AddSingleton<ApiEndpoints>()

    // SCOPED: Per-request state, database connections, unit of work
    .AddScoped<IOrderService, OrderService>()
    .AddScoped<IUnitOfWork, UnitOfWork>()
    .AddScoped<IUserContext, HttpUserContext>()

    // TRANSIENT: Lightweight, stateless, cheap to create
    .AddTransient<IValidationService, ValidationService>()
    .AddTransient<IMapper, Mapper>()

    // HOSTED SERVICE: Background workers
    .AddHostedService<NotificationQueueWorker>()
    .AddHostedService<CacheRefreshService>();
```

#### Convention-Based Registration with Scrutor

```csharp
public static IServiceCollection AddCommandsAndQueries(this IServiceCollection services)
{
    return services
        // Auto-register all commands
        .Scan(scan => scan
            .FromAssemblyOf<ICommand<object>>()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommand<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime())

        // Auto-register all queries
        .Scan(scan => scan
            .FromAssemblyOf<IQuery<object, object>>()
            .AddClasses(classes => classes.AssignableTo(typeof(IQuery<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime())

        // Apply decorators
        .Decorate(typeof(ICommand<>), typeof(CommandLogger<>))
        .Decorate(typeof(IQuery<,>), typeof(QueryLogger<,>));
}
```

---

### 5.2 Configuration Management

#### Strongly-Typed Settings

```csharp
public class ApplicationSettings
{
    public string UIHostUrl { get; set; }
    public string ApiBaseUrl { get; set; }
    public DatabaseSettings Database { get; set; }
    public AuthSettings Authentication { get; set; }
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
    public int MaxPoolSize { get; set; } = 100;
}

public class AuthSettings
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string Audience { get; set; }
}

// Registration
public static IServiceCollection AddSettings(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var settings = configuration.GetSection("Application").Get<ApplicationSettings>()
        ?? throw new InvalidOperationException("Application settings not found");

    return services.AddSingleton(settings);
}
```

#### Secret Management

```csharp
public interface ISecretService
{
    string GetSecret(string name);
    Task<string> GetSecretAsync(string name);
}

// File-based secrets (Kubernetes secrets mounted as files)
internal class FileSecretService : ISecretService
{
    private readonly string secretsPath;

    public FileSecretService(string secretsPath)
    {
        this.secretsPath = secretsPath;
    }

    public string GetSecret(string name)
    {
        var path = Path.Combine(this.secretsPath, name);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Secret '{name}' not found at {path}");

        return File.ReadAllText(path).Trim();
    }
}

// Usage
public class Credentials
{
    public string DatabaseConnectionString { get; }
    public string ApiClientId { get; }
    public string ApiClientSecret { get; }
    public string JwtSigningKey { get; }

    public Credentials(ISecretService secrets)
    {
        DatabaseConnectionString = secrets.GetSecret("database/connection-string");
        ApiClientId = secrets.GetSecret("api/client-id");
        ApiClientSecret = secrets.GetSecret("api/client-secret");
        JwtSigningKey = secrets.GetSecret("jwt/signing-key");
    }
}
```

---

### 5.3 Logging & Monitoring

#### Abstracted Logger Service

```csharp
public interface IAppLogger
{
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
    void LogEvent(string eventName, IDictionary<string, string> properties = null);
}

internal class AppInsightLogger : IAppLogger
{
    private readonly TelemetryClient telemetry;
    private readonly ILogger<AppInsightLogger> logger;

    public void LogInformation(string message, params object[] args)
    {
        this.logger.LogInformation(message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        this.logger.LogError(exception, message, args);
        this.telemetry.TrackException(exception);
    }

    public void LogEvent(string eventName, IDictionary<string, string> properties = null)
    {
        this.telemetry.TrackEvent(eventName, properties);
    }
}
```

#### Structured Logging Patterns

```csharp
// Use structured logging with named placeholders
this.logger.LogInformation(
    "Order {OrderId} created for customer {CustomerEmail} with {ItemCount} items",
    order.Id, order.CustomerEmail, order.Items.Count);

// Include correlation context
this.logger.LogWarning(
    "Payment retry {Attempt} for order {OrderId} failed: {ErrorMessage}",
    attempt, orderId, exception.Message);

// Log with scope for context
using (this.logger.BeginScope(new Dictionary<string, object>
{
    ["OrderId"] = orderId,
    ["UserId"] = userId
}))
{
    this.logger.LogInformation("Processing order");
    await ProcessAsync();
    this.logger.LogInformation("Order processed successfully");
}
```

---

### 5.4 HTTP Resilience

#### Polly Integration

```csharp
public static IHttpClientBuilder AddResiliencePolicies(this IHttpClientBuilder builder)
{
    return builder
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy())
        .AddPolicyHandler(GetTimeoutPolicy());
}

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                // Log retry
            });
}

private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
}

private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
}
```

#### Token Propagation Middleware

```csharp
public class TokenPropagationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public TokenPropagationHandler(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = this.httpContextAccessor.HttpContext;

        if (context?.Request.Headers.TryGetValue("Authorization", out var token) == true)
        {
            request.Headers.TryAddWithoutValidation("Authorization", token.ToString());
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

// Registration
services.AddHttpClient<IDownstreamService, DownstreamService>()
    .AddHttpMessageHandler<TokenPropagationHandler>()
    .AddResiliencePolicies();
```

---

## Part 6: Code Quality Metrics Checklist

Use this checklist to evaluate code against these standards.

### SOLID Principles

- [ ] **SRP**: Each class has a single, clear responsibility
- [ ] **SRP**: Services are focused on one domain concern
- [ ] **SRP**: Cross-cutting concerns use decorators
- [ ] **OCP**: New features added via extension, not modification
- [ ] **OCP**: Generic interfaces enable extensibility
- [ ] **LSP**: Derived types honor base type contracts
- [ ] **LSP**: No NotImplementedException in overrides
- [ ] **ISP**: Interfaces are narrow and focused
- [ ] **ISP**: No forced empty implementations
- [ ] **DIP**: All dependencies injected via constructor
- [ ] **DIP**: No service locator or direct instantiation

### Design Patterns

- [ ] Command/Query separation for API operations
- [ ] Repository pattern for data access
- [ ] Decorators for cross-cutting concerns (logging, audit, transactions)
- [ ] Builder pattern for complex object construction
- [ ] Strategy pattern for interchangeable algorithms

### Layer Guidelines

- [ ] API clients use HttpClientFactory
- [ ] Token refresh implemented with expiration buffer
- [ ] Services have appropriate lifetimes (Singleton/Scoped/Transient)
- [ ] Business logic in service layer, not controllers
- [ ] Repositories handle only data access
- [ ] Domain entities are POCOs with audit support
- [ ] Controllers use policy-based authorization

### Clean Code

- [ ] Naming follows conventions (PascalCase, camelCase, etc.)
- [ ] Methods are 5-20 lines with single responsibility
- [ ] Early returns/guard clauses used
- [ ] Async patterns correctly applied (Task, ValueTask, ConfigureAwait)
- [ ] Domain-specific exceptions with context
- [ ] Null validation at boundaries
- [ ] Structured logging with named placeholders

### Infrastructure

- [ ] DI registration via extension methods
- [ ] Configuration strongly typed
- [ ] Secrets externalized (not in code)
- [ ] Logging abstracted behind interface
- [ ] HTTP clients have retry and circuit breaker policies

---

## Quick Reference

### Service Lifetimes

| Lifetime | Use Case | Example |
|----------|----------|---------|
| Singleton | Stateless utilities, expensive creation | `IDateTimeService`, `IHashService` |
| Scoped | Per-request state, DB contexts | `IOrderService`, `IUnitOfWork` |
| Transient | Lightweight, stateless | `IValidator`, `IMapper` |

### Exception Status Codes

| Exception Type | HTTP Status | Error Code |
|----------------|-------------|------------|
| NotFoundException | 404 | NOT_FOUND |
| ValidationException | 400 | VALIDATION_ERROR |
| UnauthorizedException | 401 | UNAUTHORIZED |
| ForbiddenException | 403 | FORBIDDEN |
| ConflictException | 409 | CONFLICT |
| DomainException | 422 | BUSINESS_RULE_VIOLATION |

### Async Method Selection

| Return Type | Use When |
|-------------|----------|
| `Task` | Void async operation |
| `Task<T>` | Standard async returning value |
| `ValueTask<T>` | Hot path, often synchronous result |
| `IAsyncEnumerable<T>` | Streaming multiple values |

---

*This document defines the code quality standards for .NET backend development. All new code should adhere to these guidelines, and existing code should be refactored to comply during maintenance.*
