# Feature Scaffold Agent

## Purpose

The feature scaffold agent helps developers create new features in the Crypt template following the established patterns.

## Available Commands

### Create Feature
Creates a new feature with service and endpoints:
```
/agent-scaffold Feature <FeatureName> [Properties]
```

Example:
```
/agent-scaffold Feature Customers Name:string Email:string Phone:string
```

### Create Service Only
Creates just a service class:
```
/agent-scaffold Service <ServiceName>
```

### Create Endpoints Only
Creates just endpoint mappings:
```
/agent-scaffold Endpoints <EntityName>
```

## Generated Structure

When creating a feature named "Customers", the following is generated:

```
Features/Customers/
├── CustomersService.cs    # Business logic
└── CustomersEndpoints.cs  # API endpoints
```

## Service Template

```csharp
public class CustomersService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CustomersService> _logger;

    public CustomersService(ApplicationDbContext dbContext, ILogger<CustomersService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all customers");
        return await _dbContext.Customers
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.Customers.FindAsync([id], cancellationToken);
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        customer.CreatedAt = DateTime.UtcNow;
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<bool> DeleteCustomerAsync(int id, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers.FindAsync([id], cancellationToken);
        if (customer == null) return false;
        
        _dbContext.Customers.Remove(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
```

## Endpoints Template

```csharp
public static class CustomersEndpoints
{
    public static void MapCustomersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers");
        
        group.MapGet("/", GetCustomers);
        group.MapGet("/{id}", GetCustomer);
        group.MapPost("/", CreateCustomer);
        group.MapDelete("/{id}", DeleteCustomer);
    }

    private static async Task<IResult> GetCustomers(
        CustomersService service, 
        ILogger<CustomersService> logger, 
        CancellationToken cancellationToken)
    {
        var customers = await service.GetCustomersAsync(cancellationToken);
        return Results.Ok(customers);
    }

    // ... other endpoints
}
```

## Manual Feature Creation

To manually create a feature:

1. Create the feature folder: `Features/<FeatureName>/`
2. Create the service: `Features/<FeatureName>/<FeatureName>Service.cs`
3. Create the endpoints: `Features/<FeatureName>/<FeatureName>Endpoints.cs`
4. Register in Program.cs:
   ```csharp
   builder.Services.AddScoped<<FeatureName>Service>();
   app.Map<FeatureName>Endpoints();
   ```
5. Create entity model in `Data/Models/`
6. Add DbSet to ApplicationDbContext
7. Create migration: `dotnet ef migrations add Add<FeatureName>`
8. Update tests

## Best Practices

- Use feature-based organization, not technical layer
- Follow async/await patterns with CancellationToken
- Add logging at request and response boundaries
- Use meaningful names (not "Feature1" or "TestFeature")
- Keep services focused on business logic
- Use minimal APIs for endpoints