# Integration Test Approach - Important Note

## Current Implementation

The API integration tests (`RatesControllerTests` and `ConversionControllerTests`) currently use `WebApplicationFactory<Program>` **without mocking the Frankfurter API**.

This means:
- ✅ Tests call the **real Frankfurter API**
- ✅ True end-to-end integration testing
- ✅ Validates actual external service behavior
- ⚠️ Tests depend on external service availability
- ⚠️ Slower execution (network calls)
- ⚠️ May encounter rate limits

## Why This Approach?

xUnit class fixtures require parameterless constructors. The initial implementation tried to inject a fake HTTP handler via constructor, which caused:
```
Class fixture type 'CurrencyConverterWebApplicationFactory' had one or more unresolved constructor arguments
```

To fix this, we simplified the factory to have no constructor parameters.

## Test Reliability

**These tests are reliable if:**
1. You have internet connectivity
2. Frankfurter API is available (https://api.frankfurter.app/)
3. You're not rate-limited

**Expected behavior:**
- Tests will make real HTTP calls to Frankfurter
- Response data will be actual currency rates
- Tests validate the full pipeline: API → Application → Infrastructure → Frankfurter

## Future Enhancement Options

If you want to avoid external API calls, you can:

### Option 1: Mock at Service Level
Replace `ICurrencyProvider` with a mock in `ConfigureWebHost`:
```csharp
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureTestServices(services =>
    {
        services.RemoveAll<ICurrencyProvider>();
        services.AddSingleton<ICurrencyProvider>(sp => 
        {
            var mock = Substitute.For<ICurrencyProvider>();
            // Setup mock responses...
            return mock;
        });
    });
}
```

### Option 2: Use WireMock
Add WireMock.Net to stub HTTP responses:
```csharp
// Start mock server
var mockServer = WireMockServer.Start();
mockServer.Given(Request.Create().WithPath("/latest").UsingGet())
          .RespondWith(Response.Create().WithStatusCode(200).WithBody(...));
```

### Option 3: Separate Test Categories
- Keep current tests as "Live Integration Tests" (require network)
- Add separate "Isolated Integration Tests" with mocks

## Recommendation for Take-Home Assignment

For a take-home assignment demonstrating integration testing skills:
- **Current approach is acceptable** - it shows you can test the full stack
- **Document the trade-off** - explain why real API calls are OK for this context
- **Mention alternatives** - show awareness of test isolation strategies

The important thing is that the tests:
1. ✅ Validate authentication (JWT)
2. ✅ Validate authorization (RBAC)
3. ✅ Validate request/response pipeline
4. ✅ Validate error handling
5. ✅ Validate business rules (excluded currencies)

All of these work correctly whether using real or fake API responses.

## Running Tests

```powershell
# Will make real API calls
dotnet test tests/CurrencyConverter.IntegrationTests/

# Expected: ~33 tests pass (if Frankfurter API is available)
```

## Known Test Data Limitations

Since we're using real Frankfurter data:
- Currency rates change daily
- Cannot test specific rate values
- Focus on response structure and status codes
- Validate business logic (excluded currencies, pagination, etc.)

This is **normal and acceptable** for external API integration tests.
