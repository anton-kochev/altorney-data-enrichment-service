# Trade Data Enrichment Service

[![.NET](https://github.com/anton-kochev/altorney-data-enrichment-service/actions/workflows/dotnet.yml/badge.svg)](https://github.com/anton-kochev/altorney-data-enrichment-service/actions/workflows/dotnet.yml)

A .NET 10.0 ASP.NET Core API that enriches trade transaction data by mapping product IDs to human-readable product names.

## Highlights

- **Automatic Enrichment**: Maps cryptic product IDs (e.g., 75301) to readable names (e.g., "Widget Pro")
- **Data Validation**: Validates dates (yyyyMMdd format), required fields, and product ID integrity
- **Thread-Safe**: Uses `ConcurrentDictionary` for lock-free concurrent request handling
- **Clean Architecture**: 4-layer DDD structure with Domain, Application, Infrastructure, and Api layers
- **Comprehensive Testing**: Unit tests for domain logic and infrastructure services

## Built With

- [.NET 10.0](https://dotnet.microsoft.com/) - Runtime and SDK
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core) - Web API framework
- [CsvHelper](https://joshclose.github.io/CsvHelper/) - CSV parsing library
- [xUnit](https://xunit.net/) - Testing framework
- [FluentAssertions](https://fluentassertions.com/) - Test assertion library

## Table of Contents

- [Getting Started](#getting-started)
- [Usage](#usage)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [Validation Rules](#validation-rules)
- [Implementation Status](#implementation-status)
- [Documentation](#documentation)
- [CI Pipeline](#ci-pipeline)
- [Contributing](#contributing)
- [License](#license)

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

### Installation

1. Clone the repository

   ```bash
   git clone https://github.com/anton-kochev/altorney-data-enrichment-service.git
   cd altorney-data-enrichment-service
   ```

2. Build the solution

   ```bash
   dotnet build altorney-data-enrichment-service.sln
   ```

3. Run the application

   ```bash
   dotnet run --project src/Api/Api.csproj
   ```

The service loads product reference data from `src/Api/Data/product.csv` on startup.

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific tests
dotnet test --filter "FullyQualifiedName~TradeDateTests"
```

## Usage

### Input Format (Trade CSV)

```csv
date,productId,currency,price
20250605,12345,USD,150.25
20250606,67890,EUR,200.00
```

### Output Format (Enriched CSV)

```csv
date,productName,currency,price
20250605,Widget Pro,USD,150.25
20250606,Missing Product Name,EUR,200.00
```

### Product Reference File

```csv
productId,productName
12345,Widget Pro
54321,Super Gadget
```

## Project Structure

```
src/
├── Domain/                 # Core business logic (zero dependencies)
│   ├── Entities/           # Trade, EnrichedTrade, Product
│   ├── ValueObjects/       # TradeDate, ProductIdentifier, Currency, Price
│   └── Exceptions/         # Domain-specific exceptions
├── Application/            # Use cases and interfaces
│   ├── DTOs/               # Data transfer objects
│   └── Services/           # Service interfaces
├── Infrastructure/         # External concerns implementation
│   ├── Services/           # ProductDataLoader, CsvProductLookupService
│   └── Extensions/         # DI registration
└── Api/                    # ASP.NET Core entry point
    └── Data/               # product.csv reference file

tests/
├── Domain.Tests/           # Value object and entity tests
└── Infrastructure.Tests/   # Service implementation tests
```

## Configuration

Edit `src/Api/appsettings.json`:

```json
{
  "ProductData": {
    "FilePath": "Data/product.csv"
  }
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| `ProductData:FilePath` | Path to product reference CSV | `Data/product.csv` |

## Validation Rules

| Field | Rule | Failure Behavior |
|-------|------|------------------|
| `date` | yyyyMMdd format, valid calendar date | Row discarded, error logged |
| `productId` | Positive integer (> 0) | Row discarded |
| `currency` | Non-empty string | Row discarded, error logged |
| `price` | Non-negative decimal | Row discarded |

Trades with valid fields but unmapped product IDs are included with "Missing Product Name" placeholder.

## Implementation Status

### Completed

- [x] Product reference data loading at startup (US-001)
- [x] Product ID to name mapping with O(1) lookup (US-002)
- [x] Missing product handling with placeholder (US-003)
- [x] Field preservation with whitespace trimming (US-004)
- [x] Date format validation (US-005)
- [x] Required fields validation (US-006)
- [x] ProductId format validation (US-007)
- [x] CSV enrichment endpoint `POST /api/v1/enrich` (US-009)
- [x] Streaming CSV processing with CsvHelper (US-012)
- [x] Clean Architecture with DDD patterns
- [x] Unit tests for Domain, Infrastructure, and Api

### Pending

- [ ] Health check endpoint `GET /health` (US-011)
- [ ] Performance tests

See [epics-and-stories.md](epics-and-stories.md) for the complete roadmap.

## Documentation

| Document | Description |
|----------|-------------|
| [CHANGELOG.md](CHANGELOG.md) | Version history following [Keep a Changelog](https://keepachangelog.com/) format |
| [DECISIONS.md](DECISIONS.md) | Technical decision records with context and rationale |
| [epics-and-stories.md](epics-and-stories.md) | Product roadmap with epics and user stories |

### User Story Documentation

Detailed implementation documentation for completed user stories:

| Story | Description |
|-------|-------------|
| [US-001](docs/US-001.md) | Product reference data loading |
| [US-002](docs/US-002.md) | Trade enrichment service |
| [US-003](docs/US-003.md) | Missing product handling |
| [US-004](docs/US-004.md) | Field preservation with whitespace trimming |
| [US-005](docs/US-005.md) | Date format validation |
| [US-006](docs/US-006.md) | Required fields validation |
| [US-009](docs/US-009.md) | CSV enrichment endpoint |

## CI Pipeline

This project uses GitHub Actions for continuous integration.

**Workflow:** [`.github/workflows/dotnet.yml`](.github/workflows/dotnet.yml)

| Trigger | Action |
|---------|--------|
| Push to `main` | Build and test |
| Pull request to `main` | Build and test |

## Contributing

1. Follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) for commit messages
2. Maintain test coverage >80%
3. Record technical decisions in `DECISIONS.md`

## License

This project is provided for evaluation purposes.
