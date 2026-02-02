# Currency Converter Platform

A full-stack currency conversion platform demonstrating enterprise-grade architecture, resilience patterns, security, and observability.

**Tech Stack:**
- **Backend:** ASP.NET Core 9.0 (C#)
- **Frontend:** React with TypeScript (Vite)
- **External API:** Frankfurter API (https://api.frankfurter.app/)

---

## Table of Contents

1. [Setup Instructions](#setup-instructions)
2. [Architecture Overview](#architecture-overview)
3. [Features & Requirements](#features--requirements)
4. [AI Usage & Collaboration](#ai-usage--collaboration)
5. [Assumptions & Trade-offs](#assumptions--trade-offs)
6. [Future Improvements](#future-improvements)

---

## Setup Instructions

### Prerequisites

- .NET 9.0 SDK
- Node.js 18+ (for frontend)
- IDE: Visual Studio 2022, JetBrains Rider, or VS Code

### Backend Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd CurrencyConverter
   ```

2. **Configure JWT Secret** (Development only)
   
   Edit `CurrencyConverter.Api/appsettings.Development.json`:
   ```json
   {
     "Jwt": {
       "SigningKey": "your-secret-key-here-minimum-32-characters"
     }
   }
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Run the API**
   ```bash
   dotnet run --project CurrencyConverter.Api
   ```

   The API will be available at: `https://localhost:7001`

5. **Explore API Documentation**
   
   In Development mode, navigate to: `https://localhost:7001/scalar/v1`
   
   This opens the Scalar API documentation UI.

### Obtaining JWT Token (Development)

The API is secured with JWT authentication. For development/testing:

**POST** `https://localhost:7001/api/dev/token`

```json
{
  "clientId": "test-user",
  "roles": ["rates.read", "convert", "history.read"]
}
```

Or use the `admin` role for full access:
```json
{
  "clientId": "admin-user",
  "roles": ["admin"]
}
```

**Response:**
```json
{
  "token": "eyJhbGc..."
}
```

Use this token in the `Authorization` header:
```
Authorization: Bearer eyJhbGc...
```

### Frontend Setup

```bash
cd client
npm install
npm run dev
```

The frontend will be available at: `http://localhost:5173`

### Running Tests

**All tests with coverage:**
```bash
.\tests\run-tests-with-detailed-coverage.ps1
```

**Unit tests only:**
```bash
dotnet test tests/CurrencyConverter.UnitTests/
```

**Integration tests only:**
```bash
dotnet test tests/CurrencyConverter.IntegrationTests/
```

**Current Coverage:** 97%+ (Line Coverage)

---

## Architecture Overview

### Clean Architecture Structure

The solution follows **Clean Architecture** principles with clear separation of concerns:

```
CurrencyConverter/
├── CurrencyConverter.Domain/          # Enterprise business rules
│   ├── ValueObjects/                  # Currency, Money, DateRange
│   ├── Requests/                      # ConversionRequest with guards
│   ├── Paging/                        # PageRequest, PagedResult
│   └── Exceptions/                    # DomainValidationException
│
├── CurrencyConverter.Application/     # Application business rules
│   ├── Services/                      # ConversionService, ExchangeRatesService
│   ├── Abstractions/
│   │   ├── Providers/                 # ICurrencyProvider interface
│   │   ├── Caching/                   # IExchangeRateCache interface
│   │   └── Observability/             # ICorrelationIdAccessor interface
│   └── DTOs/                          # Data transfer objects
│
├── CurrencyConverter.Infrastructure/  # External concerns
│   ├── Providers/                     # FrankfurterCurrencyProvider
│   ├── Caching/                       # MemoryExchangeRateCache
│   ├── Http/                          # Outbound HTTP logging
│   ├── Helpers/                       # Resilience error classification
│   └── DependencyInjection/           # Service registration
│
└── CurrencyConverter.Api/             # Web API layer
    ├── Controllers/                   # REST endpoints (v1)
    ├── Middleware/                    # Exception, Correlation, Logging
    ├── Security/                      # JWT generation & options
    └── DependencyInjection/           # API-specific registration
```

### Dependency Flow

```
Api → Application → Domain
 ↓
Infrastructure → Application → Domain
```

- **Domain** has no dependencies (pure business logic)
- **Application** depends only on Domain (orchestration)
- **Infrastructure** implements Application interfaces
- **Api** depends on Application and Infrastructure (composition root)

---

### Key Design Patterns

#### 1. **Provider Factory Pattern**

```csharp
ICurrencyProviderFactory → ICurrencyProvider
                              ↑
                    FrankfurterCurrencyProvider
```

**Benefits:**
- Easy to add new currency data providers (e.g., Fixer.io, ExchangeRate-API)
- Runtime provider selection via configuration
- Keyed dependency injection for multiple implementations

**Configuration:**
```json
{
  "CurrencyProviders": {
    "ActiveProvider": "Frankfurter"
  }
}
```

#### 2. **Repository Pattern (Implicit)**

`IExchangeRateCache` abstracts caching:
- **Current:** In-memory cache (`IMemoryCache`)
- **Future:** Redis, Distributed cache (zero code changes in services)

#### 3. **Domain-Driven Value Objects**

- `CurrencyCode`: Validates 3-letter ISO codes, enforces excluded currencies
- `Money`: Amount + Currency pairing
- `DateRange`: Validates start ≤ end
- `PageRequest`: Validates page number and size

**Guard Clauses:** All validation happens in value object constructors, throwing `DomainValidationException`.

---

### Resilience & Fault Tolerance

**Microsoft.Extensions.Http.Resilience** pipeline on outbound HTTP:

1. **Timeout:** Prevents hanging requests
2. **Retry with Exponential Backoff:**
   - Transient errors: 5xx, 408, 429, exceptions
   - Non-transient: 4xx (except 408/429) fail immediately
3. **Circuit Breaker:** Opens after failure threshold, prevents cascading failures

**Configuration:**
```json
{
  "FrankfurterResilience": {
    "TimeoutSeconds": 10,
    "RetryMaxAttempts": 3,
    "RetryBaseDelayMilliseconds": 500,
    "CircuitBreakerSamplingSeconds": 30,
    "CircuitBreakerMinimumThroughput": 5,
    "CircuitBreakerFailureRatio": 0.5,
    "CircuitBreakerBreakSeconds": 30
  }
}
```

---

### Security Implementation

#### JWT Authentication

- **Algorithm:** HMAC-SHA256
- **Token Lifetime:** Configurable (default: 60 minutes)
- **Claims:** `sub` (client ID), `role` (permissions)

#### Role-Based Access Control (RBAC)

| Endpoint | Required Role |
|----------|---------------|
| `GET /api/v1/rates/latest` | `rates.read` or `admin` |
| `GET /api/v1/convert` | `convert` or `admin` |
| `GET /api/v1/rates/historical` | `history.read` or `admin` |

**Fallback Policy:** All endpoints require authentication by default.

#### Rate Limiting

- **Algorithm:** Token bucket
- **Partition Key:** 
  - Authenticated: JWT `sub` claim
  - Anonymous: Client IP address
- **Configuration:**
```json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 0
  }
}
```

**Behavior:** HTTP 429 when limit exceeded.

---

### Observability & Logging

#### Structured Logging (Serilog)

Every request logs:
- **Client IP** (from `HttpContext.Connection.RemoteIpAddress`)
- **Client ID** (from JWT `sub` claim)
- **HTTP Method & Endpoint**
- **Response Status Code**
- **Response Time** (milliseconds)

**Log Context Enrichment:**
```csharp
RequestLogContextMiddleware → Serilog.Context.LogContext
```

#### Correlation ID Flow

1. **Accept or Generate:** `X-Correlation-ID` header
2. **Store:** `ICorrelationIdAccessor` (AsyncLocal scoped)
3. **Response Header:** Return same ID to client
4. **Outbound Propagation:** Add to Frankfurter API calls
5. **Logging:** All logs include `CorrelationId` property

**Benefits:**
- Trace requests across API and external calls
- Debug production issues by correlation ID
- Simplifies distributed tracing setup

#### Request Logging Example

```json
{
  "@t": "2026-02-01T10:15:30.1234567Z",
  "@mt": "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms",
  "RequestMethod": "GET",
  "RequestPath": "/api/v1/convert",
  "StatusCode": 200,
  "Elapsed": 145.2341,
  "ClientIp": "192.168.1.100",
  "ClientId": "user-123",
  "CorrelationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

---

### API Versioning

**Strategy:** URL Segment versioning

```
/api/v1/rates/latest
/api/v1/convert
/api/v1/rates/historical
```

**Benefits:**
- Clear, visible version in URL
- Supports major version changes
- Easy client migration

**Configuration:**
```csharp
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
```

---

## Features & Requirements

### ✅ Functional Requirements

#### 1. Latest Exchange Rates
- **Endpoint:** `GET /api/v1/rates/latest?baseCurrency=EUR`
- **Source:** Frankfurter API
- **Caching:** 5 minutes TTL

#### 2. Currency Conversion
- **Endpoint:** `GET /api/v1/convert?amount=100&from=USD&to=EUR`
- **Validation:** Excludes TRY, PLN, THB, MXN (returns 400 with clear message)
- **Business Rule:** Enforced in Domain layer (`ConversionRequest.Create`)

#### 3. Historical Exchange Rates (Paginated)
- **Endpoint:** `GET /api/v1/rates/historical?baseCurrency=EUR&start=2024-01-01&end=2024-01-31&page=1&pageSize=10`
- **Pagination:** Server-side with `PagedResult<T>` response
- **Caching:** 60 minutes TTL

### ✅ Technical Requirements

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| **Resilience** | Retry + Circuit Breaker | ✅ |
| **Performance** | Memory caching (Redis-ready) | ✅ |
| **Extensibility** | Provider factory pattern | ✅ |
| **Security** | JWT + RBAC + Rate Limiting | ✅ |
| **Observability** | Serilog + Correlation IDs | ✅ |
| **Testing** | 97%+ coverage | ✅ |
| **Versioning** | URL segment (v1) | ✅ |
| **OpenAPI** | Scalar UI (dev mode) | ✅ |

---

## AI Usage & Collaboration

### How AI Was Used

#### Initial brainstorming and architecture validation
AI was used early in the process to explore architectural options and trade-offs. From the start, I explicitly insisted on Clean Architecture, clear layering, and strict separation of concerns. AI input was used to validate and challenge ideas, not to define them.

#### Structured, step-by-step implementation
After establishing the overall architecture, AI was used to help create a detailed, step-by-step coding plan for the implementation. The work was deliberately broken down into small, reviewable steps. Each step was implemented, reviewed, and adjusted before moving on to the next one.

#### Decision support, not decision-making
For non-trivial decisions, AI was used to present alternatives, explain trade-offs, and highlight potential risks. Final decisions were made manually, based on task requirements, maintainability, and scope. AI was used to support informed decision-making, not to make decisions independently.

#### Test strategy and test implementation
I defined and explained the full testing strategy myself, including:
- what should be unit-tested
- what should be covered by integration tests
- what should explicitly not be tested

AI was then used as a coding assistant to write the actual test implementations based on this strategy.

#### Documentation support
AI was also used to assist with documentation by helping structure sections, improve clarity, and ensure consistency. All documentation was reviewed and refined manually to accurately reflect design decisions and implementation details.

### What Was Changed or Rejected

Several AI suggestions were intentionally reviewed and adjusted to better align with the task requirements, scope, and maintainability goals.

#### Redis as a caching layer
AI suggested introducing Redis as a distributed caching solution. I decided not to include Redis in the current implementation, as it was not required by the task and would introduce additional operational complexity without clear benefit at this stage. Instead, in-memory caching was used to fully satisfy the requirements. Redis is explicitly listed as a potential future improvement once scalability demands justify it.

#### Additional mediation or orchestration layers
AI proposed adding extra mediation or orchestration patterns to structure application flow. I chose not to adopt these suggestions, as they increased complexity without providing meaningful value for the given scope. Simpler service interactions were preferred while still respecting Clean Architecture boundaries.

#### Frameworks and abstractions beyond requirements
AI occasionally suggested additional frameworks or abstractions that were not strictly necessary. These were deliberately rejected to keep the solution focused, readable, and aligned with the task’s expectations.

#### Generated code complexity
AI-generated code was frequently simplified or refactored when it introduced unnecessary indirection or verbosity. Preference was given to explicit, easy-to-follow implementations over clever or overly generic solutions.


---

## Configuration Management

### Environment Configuration

The application uses a two-tier configuration approach:

#### `appsettings.json` (Non-Secret Defaults)

Contains all non-sensitive configuration:
- **Provider Selection:** Active currency provider (`CurrencyProviders:ActiveProvider`)
- **Resilience Configuration:** Timeout, retry, circuit breaker settings (`FrankfurterResilience`)
- **JWT Metadata:** Issuer, Audience, TokenLifetime, ClockSkew (`Jwt`)
- **Rate Limiting:** PermitLimit, WindowSeconds, QueueLimit (`RateLimiting`)
- **Serilog Configuration:** Console sink, log levels, output template

#### `appsettings.Development.json` (Development-Only Secrets)

Contains development-specific secrets:
- **JWT Signing Key:** `Jwt:SigningKey` (not committed to source control)

**Security Note:** No secrets are committed to source control. The signing key must be configured per environment.

### Configuration Sections

```json
{
  "CurrencyProviders": {
    "ActiveProvider": "Frankfurter"
  },
  "Frankfurter": {
    "BaseUrl": "https://api.frankfurter.app/"
  },
  "FrankfurterResilience": {
    "TimeoutSeconds": 10,
    "RetryMaxAttempts": 3,
    "RetryBaseDelayMilliseconds": 500,
    "CircuitBreakerSamplingSeconds": 30,
    "CircuitBreakerMinimumThroughput": 5,
    "CircuitBreakerFailureRatio": 0.5,
    "CircuitBreakerBreakSeconds": 30
  },
  "Jwt": {
    "Issuer": "CurrencyConverter",
    "Audience": "CurrencyConverterClients",
    "TokenLifetimeMinutes": 60,
    "ClockSkewSeconds": 30
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 0
  }
}
```

---
## Assumptions & Trade-offs

The implementation makes a number of deliberate assumptions and trade-offs to stay focused on the task requirements while maintaining clean structure and extensibility.

### Assumptions

The solution assumes that the Frankfurter API is a reliable upstream data source and treats it as the single source of truth for exchange rates. Resilience mechanisms (timeouts, retries, circuit breaker) are used to mitigate temporary outages rather than attempting to replicate or persist external data.

Currency codes are assumed to follow ISO 4217 standards. The explicitly excluded currencies (TRY, PLN, THB, MXN) are enforced as business rules in the domain layer, as required by the task.

Rate limiting thresholds are assumed to be sufficient for a typical client usage pattern and are fully configurable. The exact values are not treated as fixed, but as defaults suitable for a demonstration environment.

The solution assumes a single active currency provider for now. The architecture supports adding additional providers later, but only Frankfurter is implemented to avoid unnecessary complexity.


### Trade-offs

#### In-memory caching instead of Redis

In-memory caching was chosen to satisfy performance and resilience requirements without introducing additional infrastructure dependencies. This keeps the solution simple and easy to run locally, while still demonstrating proper abstraction via `IExchangeRateCache`.

The trade-off is that cached data is not shared across instances and is lost on restart. This was considered acceptable for the scope of the task and can be addressed later by swapping in a distributed cache implementation.


#### Development-only authentication instead of a full auth system

A minimal development token endpoint was implemented to demonstrate JWT authentication, RBAC, and rate limiting without building a full authentication system.

This decision avoids spending time on out-of-scope concerns such as user registration, password management, or external identity providers, while still allowing security-related features to be exercised and tested.

The trade-off is that this endpoint is not suitable for production and exists only in development. A proper authentication system is explicitly listed as a future improvement.


#### Symmetric JWT signing

Symmetric JWT signing was chosen for simplicity and ease of configuration. This is sufficient for a single-service setup and avoids additional key management concerns.

The trade-off is reduced security flexibility compared to asymmetric keys, which would be preferable in a multi-service or externally integrated environment.


#### Integration tests against the real API

Integration tests were designed to call the real Frankfurter API to validate true end-to-end behavior.

This provides strong confidence in the integration but introduces external dependencies and slower test execution. For a production setup, these tests could be isolated using mocks or a tool like WireMock, but real calls were preferred here for correctness and realism.

---

## Future Improvements

The current implementation intentionally focuses on the task requirements and avoids unnecessary complexity. The following improvements represent realistic and clearly scoped next steps for a production-ready system.

### 1. Production Authentication

The current solution includes a development-only token endpoint to demonstrate JWT authentication and RBAC without implementing a full authentication system.

A production-ready version would replace this with a proper authentication flow, such as:
- OAuth 2.0 / OpenID Connect integration (e.g. Azure AD, Auth0, Keycloak), or
- A custom authentication system with secure password handling and refresh tokens

This would enable real user management while preserving the existing authorization model.


### 2. Distributed Caching

In-memory caching was intentionally chosen to satisfy performance requirements without introducing infrastructure dependencies.

For a horizontally scaled deployment, this could be replaced with a distributed cache (e.g. Redis) by implementing an alternative `IExchangeRateCache` without changing application or domain logic.


### 3. CI/CD Pipeline

While not implemented as part of this task, the solution is structured to support CI/CD workflows.

A next step would include:
- Automated build and test execution
- Coverage reporting as part of the pipeline
- Environment-specific configuration and deployments


### 4. Observability Expansion (Optional)

Basic structured logging and correlation IDs are already in place.

If needed, this could be extended with:
- OpenTelemetry exports
- Centralized tracing and metrics dashboards

This was intentionally kept minimal to stay within the scope of the assignment.


### Summary

These improvements build directly on the existing design and were deliberately deferred to keep the solution focused, readable, and aligned with the task scope.
