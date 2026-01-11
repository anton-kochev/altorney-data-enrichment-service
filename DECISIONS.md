# Technical Decisions

This document records significant technical decisions made during the development of this project.

## Format

Each decision follows this format:

- **Decision**: What was decided
- **Context**: Why this decision was needed
- **Alternatives Considered**: Other options that were evaluated
- **Rationale**: Why this option was chosen

---

## Decisions

### 001. Clean Architecture with Domain-Driven Design

**Decision**: Structure the solution using Clean Architecture with DDD layers (Api, Infrastructure, Application, Domain).

**Context**: Need a maintainable, testable architecture that separates concerns and allows independent evolution of components.

**Alternatives Considered**:

- Monolithic single-project structure
- Traditional N-tier architecture

**Rationale**: Clean Architecture provides clear dependency rules (outer layers depend on inner), making the codebase testable and maintainable. DDD patterns align well with the business domain of trade enrichment.

---

### 002. Separate Api Project

**Decision**: Create a dedicated Api project as the entry point, separate from Infrastructure.

**Context**: Needed to decide whether to use Infrastructure as the Web API host or create a separate project.

**Alternatives Considered**:

- Use Infrastructure project as the Web API host

**Rationale**: Separation provides clearer boundaries - Api handles HTTP concerns (controllers, middleware, DI configuration) while Infrastructure handles external integrations (database, file I/O). This also allows for potential alternative entry points in the future.

---

### 003. Controller-Based Web API

**Decision**: Use traditional controller-based ASP.NET Core Web API instead of Minimal APIs.

**Context**: Need to choose between controller-based and minimal API approaches for the REST endpoints.

**Alternatives Considered**:

- Minimal APIs

**Rationale**: Controller-based approach provides better organization for validation, error handling, and follows familiar MVC patterns. While we only have 2 endpoints initially, controllers scale better if the API grows.

---

### 004. Entity Framework Core for Data Access

**Decision**: Use Entity Framework Core directly instead of Repository Pattern abstraction.

**Context**: Need to define how data access will be handled across the application.

**Alternatives Considered**:

- Repository Pattern with IProductRepository interface

**Rationale**: EF Core already provides a unit of work and repository-like abstraction (DbContext, DbSet). Adding another repository layer introduces unnecessary abstraction without significant benefit for this project's scope.

---

### 005. Keep a Changelog Format

**Decision**: Follow [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format for documenting changes.

**Context**: Need a consistent format for tracking project changes across versions.

**Alternatives Considered**:

- Conventional Commits only
- Auto-generated changelog from commits

**Rationale**: Keep a Changelog provides human-readable, curated change documentation. It categorizes changes (Added, Changed, Deprecated, Removed, Fixed, Security) making it easy for users to understand what changed between versions.

---

### 006. EditorConfig for Code Style

**Decision**: Use [EditorConfig](https://editorconfig.org/) for code style enforcement.

**Context**: Need consistent code formatting across different editors and team members.

**Alternatives Considered**:

- IDE-specific settings only
- StyleCop/Roslyn analyzers only

**Rationale**: EditorConfig is IDE-agnostic and supported by most editors natively. It handles basic formatting (indentation, line endings) while more complex C# rules can be added via .editorconfig's C# extensions.

---

### 007. Centralized Build Configuration

**Decision**: Use global.json, Directory.Build.props, and .gitattributes for consistent builds.

**Context**: Need to ensure consistent SDK versions, project settings, and line endings across environments.

**Alternatives Considered**:

- Individual project settings only
- CI/CD enforcement only

**Rationale**:

- global.json pins SDK version preventing "works on my machine" issues
- Directory.Build.props eliminates duplication and ensures all projects use same settings
- .gitattributes ensures consistent line endings across Windows/Mac/Linux

---

### 008. Conventional Commits

**Decision**: Follow [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/) specification for all commit messages.

**Context**: Need a consistent commit message format that enables automated changelog generation and semantic versioning.

**Alternatives Considered**:

- Freeform commit messages
- Custom commit format

**Rationale**: Conventional Commits provide a standardized format that works with semantic versioning, enables automated tooling (changelog generation, version bumping), and makes commit history more readable. Types like `feat`, `fix`, `docs` clearly communicate the nature of changes.

---

### 009. FrozenDictionary with Volatile for Thread-Safe Read-Only Data

**Decision**: Use `FrozenDictionary<int, string>` with `volatile` keyword for storing product reference data.

**Context**: Product data is loaded once at startup and accessed concurrently by multiple request threads. Need an efficient, thread-safe data structure for O(1) lookups.

**Alternatives Considered**:

- `ConcurrentDictionary<int, string>` - thread-safe but optimized for concurrent writes
- `ImmutableDictionary<int, string>` - immutable but slower lookups
- `Dictionary<int, string>` with locks - manual synchronization overhead

**Rationale**: `FrozenDictionary` (.NET 8+) is specifically optimized for read-only scenarios with lower memory overhead and faster lookups than alternatives. The `volatile` keyword ensures visibility across threads during potential hot-reload scenarios, preventing stale reads from CPU cache.

---

### 010. CsvHelper Library for CSV Parsing

**Decision**: Use [CsvHelper](https://joshclose.github.io/CsvHelper/) for CSV parsing.

**Context**: Need CSV parsing for loading product reference data at startup and processing trade files. Must support streaming from non-seekable HTTP request bodies.

**Alternatives Considered**:

- Sep (nietras.Sep) - fastest .NET CSV parser, but requires seekable streams
- Sylvan.Data.Csv - good performance, more complex API
- Manual parsing with `string.Split` - error-prone, no RFC 4180 compliance

**Rationale**: CsvHelper is the most popular .NET CSV library with excellent RFC 4180 compliance. Critically, it supports forward-only streams, enabling true streaming directly from `request.Body` without buffering the entire request into memory. While Sep offers better raw performance, CsvHelper's streaming capability reduces memory usage from ~100MB (full buffer) to minimal row-by-row processing for large CSV uploads. The API is well-documented and handles edge cases (quoting, escaping, multiline fields) correctly.

---

### 011. Test-Driven Development

**Decision**: Follow Test-Driven Development (TDD) methodology - write tests before implementation.

**Context**: Need a development approach that ensures high test coverage and drives clean, testable design.

**Alternatives Considered**:

- Test-after development - write tests after implementation
- Behavior-Driven Development (BDD) - higher-level specification tests

**Rationale**: TDD ensures every feature has test coverage from the start, catches design issues early, and produces more modular code. Writing tests first clarifies requirements and acceptance criteria before coding begins. The project uses xUnit with FluentAssertions and FakeLogger for clean, readable tests.

---

### 012. Source-Generated Logging

**Decision**: Use source-generated logging with `[LoggerMessage]` attribute instead of traditional `ILogger` extension methods.

**Context**: Need efficient, structured logging throughout the application with minimal runtime overhead.

**Alternatives Considered**:

- Traditional `ILogger.LogInformation()` extension methods - allocates strings and objects
- Serilog with message templates - third-party dependency
- Manual `ILogger.Log()` calls - verbose and error-prone

**Rationale**: Source-generated logging (introduced in .NET 6) provides zero-allocation logging by generating optimal code at compile time. Benefits include: compile-time validation of message templates, consistent EventIds, better performance in high-throughput scenarios. Requires `partial` class and methods but eliminates runtime string formatting overhead.

---

### 013. Value Objects for Domain Validation

**Decision**: Encapsulate validation rules in value objects (TradeDate, ProductIdentifier, Currency, Price) rather than using external validation frameworks.

**Context**: Trade data requires validation of multiple fields (date format, positive product ID, non-empty currency, non-negative price). Need to decide where validation logic lives.

**Alternatives Considered**:

- FluentValidation library - powerful but adds external dependency
- Inline validation in services - scatters validation logic
- Data Annotations - limited to simple rules, couples domain to framework

**Rationale**: Value objects are a DDD pattern that keeps validation close to the domain model. Invalid states become unrepresentable at compile time. Validation logic is reusable across the codebase and testable in isolation. Each value object is self-documenting about what constitutes valid data.

---

### 014. Static Factory Methods for Domain Objects

**Decision**: Use static `Create()` factory methods instead of public constructors for domain value objects and entities.

**Context**: Need a consistent pattern for creating domain objects that ensures validation always runs.

**Alternatives Considered**:

- Public constructors with validation in body - allows `new` keyword but less explicit
- Builder pattern - more verbose, better for objects with many optional parameters
- Private constructors only - requires factory classes

**Rationale**: Static factory methods make object creation explicit and self-documenting. They ensure validation cannot be bypassed, allow descriptive method names (e.g., `Create`, `FromString`), and enable future evolution to return Result types for error handling without breaking changes. The pattern is lightweight compared to builders while providing the same guarantees as constructor validation.

---

### 015. Response Headers for Enrichment Summary

**Decision**: Return `EnrichmentSummary` statistics via `X-Enrichment-*` response headers instead of wrapping the CSV response in a JSON envelope.

**Context**: The CSV enrichment endpoint needs to provide processing statistics (total rows, enriched rows, discarded rows, missing products) alongside the enriched CSV data. Need to decide how to deliver this metadata.

**Alternatives Considered**:

- JSON wrapper object containing both CSV data and summary - breaks streaming, changes Content-Type
- Multipart response with summary and CSV parts - complex client parsing
- Trailing summary row in CSV output - breaks standard CSV format
- Separate endpoint to query processing results - requires correlation, adds complexity

**Rationale**: HTTP response headers are the idiomatic way to provide metadata about a response. Benefits include:

- Maintains pure CSV response body with `text/csv` Content-Type
- Preserves streaming capability (no need to buffer entire response)
- Machine-readable and easy to parse from any HTTP client
- Headers are set before streaming begins, providing immediate visibility
- Standard HTTP semantics, no custom format to document

Headers used: `X-Enrichment-Total-Rows`, `X-Enrichment-Enriched-Rows`, `X-Enrichment-Discarded-Rows`, `X-Enrichment-Missing-Products`, `X-Enrichment-Missing-Product-Ids`.

---

### 016. Request Timeout Policy for Enrichment Endpoint

**Decision**: Use ASP.NET Core's `RequestTimeouts` middleware with a named policy (`EnrichmentPolicy`) and the `[RequestTimeout]` attribute.

**Context**: Large CSV files (up to 100MB) may take significant time to process. Need to prevent runaway requests while allowing sufficient time for legitimate large file processing.

**Alternatives Considered**:

- `CancellationToken` with manual timeout - requires custom implementation
- Kestrel's `RequestHeadersTimeout` - applies globally, not per-endpoint
- Custom middleware with `Task.Delay` - more complex, error-prone

**Rationale**: ASP.NET Core's built-in `RequestTimeouts` middleware (introduced in .NET 7) provides:

- Per-endpoint timeout configuration via named policies
- Automatic `CancellationToken` propagation
- Clean HTTP 408 response on timeout
- Configurable timeout response handler
- Integration with the `[RequestTimeout("PolicyName")]` attribute

Configuration via `EnrichmentEndpointOptions.TimeoutSeconds` (default: 5 minutes) allows runtime adjustment without code changes.

---

### 017. Direct Streaming for CSV Input Parsing

**Decision**: Read CSV input directly from `request.Body` using CsvHelper without buffering.

**Context**: ASP.NET Core's request body is a forward-only, non-seekable stream. Need to parse CSV data efficiently without loading the entire request into memory.

**Alternatives Considered**:

- Buffer into `MemoryStream` first - requires ~100MB memory for large files
- Read entire body as string with `ReadToEndAsync()` - doubles memory (bytes + string)
- Enable request body buffering middleware - global impact, less control

**Rationale**: CsvHelper supports forward-only streams, enabling true streaming directly from `request.Body`. Benefits include:

- Minimal memory footprint - only one row in memory at a time
- No buffering overhead for large file uploads
- Immediate processing - no delay waiting for full upload
- Better scalability under concurrent load

This approach processes CSV data row-by-row as it arrives from the network, making memory usage independent of file size.

---

<!-- Add new decisions above this line -->
