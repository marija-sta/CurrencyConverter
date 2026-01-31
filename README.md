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

This project was developed **in collaboration with AI** (GitHub Copilot) as a **collaborative technical assistant**, not as an autonomous code generator. Below is a transparent account of how AI was used and where human judgment was applied.

### How AI Was Used

#### 1. **Architecture & Design Discussions**

**AI Role:**
- Proposed multiple architectural options and trade-offs for evaluation
- Suggested Clean Architecture structure with clear boundaries
- Recommended resilience patterns (retry, circuit breaker, timeout)
- Proposed provider factory for extensibility
- Helped identify where to place business rules (Domain vs Application vs Infrastructure)
- Reviewed design decisions for Clean Architecture alignment

**Human Decision:**
- ✅ Accepted: Clean Architecture separation with Domain at the core
- ✅ Accepted: Provider factory pattern for extensibility
- ⚠️ Modified: Selected keyed services over alternative provider factory implementations (simpler DI)
- ⚠️ Modified: Chose major-only API versioning strategy (no minor versions)
- ❌ Rejected: AI initially suggested complex multi-provider strategy; simplified to single active provider
- ❌ Rejected: MediatR and other overengineering patterns (kept solution focused on requirements)

#### 2. **Code Generation & Problem Solving**

**AI Role:**
- Generated initial controller endpoints
- Created middleware boilerplate
- Generated test class structures
- Wrote repetitive DTO mappings
- Helped identify and fix dependency injection issues
- Assisted with configuration structure design
- Helped debug JSON serialization issues (e.g., Frankfurter's `start_date`/`end_date` mapping)
- Supported API versioning and OpenAPI setup
- Assisted with error handling strategy design

**Human Validation:**
- ✅ Verified all generated code for correctness
- ✅ Refactored to match team coding standards
- ✅ Added XML documentation where AI omitted it
- ⚠️ Fixed: DI wiring issues (typed HttpClient BaseAddress with keyed services)
- ⚠️ Fixed: JSON property mapping for Frankfurter DTOs
- ⚠️ Fixed: AI sometimes used `OnStarting` callbacks unnecessarily
- ⚠️ Adjusted: Error handling behavior for invalid client input (400 vs 500)
- ❌ Rejected: Over-engineered service abstractions (kept it simple)

#### 3. **Test Generation & Strategy**

**AI Role:**
- Generated 60+ unit test method signatures
- Provided FluentAssertions syntax
- Created integration test setup with `WebApplicationFactory`
- Assisted with testing strategy design (unit vs integration)
- Helped draft test case scenarios

**Human Decisions:**
- ✅ Reviewed every test for correctness
- ⚠️ Fixed: Middleware tests initially didn't trigger `OnStarting` callbacks (removed callbacks entirely)
- ⚠️ Modified: Simplified correlation ID middleware tests
- ⚠️ Fixed: Test setup issues identified during manual testing
- ✅ Added: Edge case tests AI didn't generate (e.g., empty correlation ID header, null handling)
- ✅ Defined: 90% coverage threshold with pragmatic exclusions (DI classes)

#### 4. **Resilience Configuration**

**AI Role:**
- Provided initial Polly retry/circuit breaker setup
- Suggested transient error classification logic

**Human Validation:**
- ⚠️ Modified: Transient error classification (408 and 429 are transient, other 4xx are not)
- ✅ Tuned: Circuit breaker thresholds based on Frankfurter SLA
- ✅ Validated: Retry backoff strategy with exponential + jitter

#### 5. **Documentation**

**AI Role:**
- Generated initial README structure
- Provided XML doc comments for public APIs
- Created test documentation

**Human Oversight:**
- ✅ Rewrote: Made documentation more concise
- ✅ Added: Real-world examples and troubleshooting tips
- ✅ Corrected: AI sometimes confused project structure

---

### What AI Did NOT Control

#### Design Decisions (Human-Driven)

1. **Excluded Currencies Rule Placement**
   - AI suggested middleware validation
   - **Human:** Moved to Domain layer (`ConversionRequest`) for proper business rule encapsulation

2. **Correlation ID Middleware**
   - AI used `OnStarting` callback
   - **Human:** Simplified to set header directly (no callback needed)

3. **JWT Storage Strategy (Frontend)**
   - AI suggested cookies
   - **Human:** Chose in-memory storage for SPA security

4. **Test Coverage Strategy**
   - AI suggested 100% coverage
   - **Human:** Defined 90% threshold, excluded DI classes pragmatically

#### Code Quality Standards

- **Naming:** Human enforced consistent naming (no abbreviations)
- **Comments:** Human removed AI's over-commenting (code should be self-explanatory)
- **Error Messages:** Human reviewed all user-facing messages for clarity
- **Edge Cases:** Human identified missing test scenarios AI overlooked

---

### Lessons Learned (AI Collaboration)

#### ✅ AI Strengths

- **Boilerplate:** Excellent at repetitive code (DTOs, test setup, middleware)
- **Patterns:** Good at suggesting standard patterns (factory, repository)
- **Speed:** 3-5x faster initial implementation
- **Documentation:** Helpful for XML comments and README structure

#### ⚠️ AI Limitations

- **Context:** Sometimes forgets earlier decisions in long sessions
- **Pragmatism:** Tends to over-engineer (gold-plating)
- **Domain Logic:** Cannot validate business rules without human input
- **Testing Edge Cases:** Misses subtle edge cases (e.g., empty string vs null)
- **Middleware Order:** Doesn't always understand ASP.NET Core pipeline order

#### 🎯 Best Practices for AI Collaboration

1. **Review Everything:** Never blindly accept generated code
2. **Iterate:** Use AI for first draft, human refines
3. **Test AI Code:** Write tests for AI-generated code
4. **Clarify Context:** Re-state requirements when AI drifts
5. **Challenge Suggestions:** Ask "why" for AI recommendations

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

### Assumptions

1. **Frankfurter API Availability**
   - Assumption: >99% uptime
   - Mitigation: Circuit breaker prevents cascading failures

2. **Currency Code Stability**
   - Assumption: ISO 4217 codes don't change frequently
   - Trade-off: Hardcoded excluded currencies (TRY, PLN, THB, MXN)

3. **Historical Data Range**
   - Assumption: Frankfurter supports date ranges up to 1 year
   - Validation: None (client can request any range)

4. **Rate Limiting Sufficiency**
   - Assumption: 100 requests/minute per client is adequate
   - Tunable via configuration

5. **Single Currency Provider**
   - Assumption: Frankfurter is sufficient for MVP
   - Extensibility: Factory pattern allows adding providers later

### Trade-offs

#### 1. **In-Memory Caching vs Redis**

**Decision:** In-memory caching (`IMemoryCache`)

**Pros:**
- Zero infrastructure dependency
- Simpler deployment
- Lower latency

**Cons:**
- Not shared across API instances
- Lost on restart
- Not suitable for large-scale production

**Future:** Swap to Redis via `IExchangeRateCache` interface (no code change)

#### 2. **Symmetric JWT vs Asymmetric**

**Decision:** Symmetric key (HMAC-SHA256)

**Pros:**
- Simpler configuration
- Faster token generation
- Adequate for single-tenant API

**Cons:**
- Requires secret key sharing
- Less secure than RSA/ECDSA

**Future:** Move to asymmetric keys for production

#### 3. **Development Token Endpoint (Not Full Authentication)**

**Decision:** Minimal `/api/dev/token` endpoint instead of full authentication flow

**Rationale:**
The task requirements did not specify implementing a complete authentication system (login, registration, password management, OAuth, etc.). To avoid spending time on out-of-scope features while still demonstrating:
- JWT authentication
- RBAC enforcement
- Rate limiting by client ID

A **development-only** token issuance endpoint was implemented as a **minimal API endpoint** (not a controller) that:
- Exists **only in Development environment**
- Accepts a `clientId` and `roles[]` to generate a test JWT
- Enables testing and demonstration of security features
- Is explicitly documented as **not a production authentication flow**

**Pros:**
- Focused on task requirements (JWT + RBAC)
- Easy testing and development
- No external OAuth dependency
- Saved significant development time
- Clear separation (minimal API, not part of controller surface)

**Cons:**
- Not a real authentication flow
- Cannot be used in production
- Requires developers to manually request tokens

**Mitigation:** 
- Only mapped in Development environment (`app.MapDevAuthEndpoints()` wrapped in `if (app.Environment.IsDevelopment())`)
- Clearly documented as development-only in code and README
- Would be replaced with proper authentication in production (see Future Improvements)

#### 4. **Integration Tests Call Real API**

**Decision:** Integration tests call actual Frankfurter API

**Pros:**
- True end-to-end validation
- Catches real integration issues

**Cons:**
- Network dependency
- Slower tests (~5-8 seconds)
- Potential rate limiting

**Alternative:** Could mock with WireMock (documented in tests)

#### 5. **No Refresh Token**

**Decision:** JWT only, no refresh token

**Pros:**
- Simpler implementation
- Stateless

**Cons:**
- User must re-authenticate after token expiry

**Future:** Add refresh token for production UX

---

## Future Improvements

### Short-Term (Production Readiness)

1. **Production Authentication System** ⭐ **PRIORITY**
   - Replace development token endpoint with proper authentication
   - Implement one of:
     - OAuth 2.0 / OpenID Connect integration (Azure AD, Auth0, Keycloak)
     - Custom login/registration endpoints with password hashing
     - API key management system
   - Add user management (registration, password reset, email verification)
   - Implement refresh tokens for better UX
   - Add rate limiting per authenticated user
   - **Current State:** Dev-only token endpoint exists solely for testing JWT/RBAC features

2. **Distributed Caching**
   - Implement `RedisExchangeRateCache`
   - Update DI registration
   - No service code changes (abstraction FTW!)

3. **Asymmetric JWT Keys**
   - Generate RSA key pair
   - Update `JwtTokenService`
   - Store private key in Azure Key Vault

4. **Health Checks**
   - Add `/health` endpoint
   - Check Frankfurter API connectivity
   - Integrate with Kubernetes liveness/readiness probes

5. **Advanced Observability**
   - Export to OpenTelemetry
   - Integrate with Application Insights / Grafana
   - Distributed tracing across services

6. **HTTPS Everywhere**
   - Enforce HTTPS in production
   - Add HSTS headers

### Medium-Term (Scalability)

7. **Multiple Currency Providers**
   - Add Fixer.io, ExchangeRate-API
   - Implement fallback strategy
   - Provider health scoring

8. **Response Caching Headers**
   - Add `Cache-Control`, `ETag`
   - Enable client-side caching

9. **GraphQL Support**
   - Add HotChocolate
   - Allow clients to query only needed fields

10. **Background Jobs**
    - Pre-warm cache for popular currency pairs
    - Daily data refresh job

11. **Database for Historical Data**
    - Cache historical rates in SQL/NoSQL
    - Reduce Frankfurter API dependency

12. **CI/CD Pipeline**
    - Automated test execution
    - Coverage reporting
    - Deployment automation

### Long-Term (Enterprise Features)

13. **Multi-Tenancy**
    - Tenant-specific rate limits
    - Custom currency provider per tenant

14. **Webhooks**
    - Notify clients on rate changes
    - Event-driven architecture

15. **Analytics Dashboard**
    - Track most requested currencies
    - API usage metrics

16. **Mobile SDK**
    - Native SDKs for iOS/Android
    - Offline-first with sync

17. **AI-Powered Rate Prediction**
    - ML model for rate forecasting
    - Trend analysis

---

## Project Statistics

- **Lines of Code:** ~3,500 (excluding tests)
- **Test Coverage:** 97.1% line coverage, 86.4% branch coverage
- **Test Count:** 161 tests (106 unit, 55 integration)
- **Build Time:** ~5 seconds
- **Test Execution:** ~10 seconds (with real API calls)

---

## License

This project is a take-home assignment demonstrating technical proficiency.

---

## Contact

For questions or feedback, please open an issue in the repository.

---

**Last Updated:** February 1, 2026
