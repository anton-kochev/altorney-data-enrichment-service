# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-01-12

### Added

- Initial project structure for Trade Data Enrichment Service
- Product reference data loading at startup from configurable CSV file (US-001)
  - Enables mapping of product IDs to product names for trade enrichment
  - Invalid rows are skipped and logged without blocking the service
  - Service fails fast if product file is missing or unreadable
- Trade enrichment capability to translate product IDs to human-readable names (US-002)
  - Enriches trade data by replacing numeric product IDs with product names
  - Handles missing products gracefully with placeholder value
  - Supports concurrent request processing for high throughput
  - Provides enrichment summary with processing statistics
- Enhanced missing product handling with detailed logging (US-003)
  - Warning logs now include trade context (date, currency, price) for easier troubleshooting
  - Duplicate warnings aggregated (each missing productId logged only once)
  - Missing product count included in enrichment summary for reporting
- Field preservation with automatic whitespace trimming (US-004)
  - Date, currency, and price fields are preserved through enrichment
  - Leading/trailing whitespace is automatically trimmed from all fields
  - Original field values (after trimming) are maintained in output
- CSV enrichment endpoint at POST /api/v1/enrich (US-009)
  - Accepts trade data in CSV format with Content-Type: text/csv
  - Returns enriched CSV with product names replacing product IDs
  - Response includes X-Enrichment-* headers with processing statistics:
    - X-Enrichment-Total-Rows: total rows processed
    - X-Enrichment-Enriched-Rows: successfully enriched rows
    - X-Enrichment-Discarded-Rows: rows discarded due to validation
    - X-Enrichment-Missing-Products: rows with missing product mappings
    - X-Enrichment-Unique-Missing-Product-Ids: count of unique missing product IDs
  - Returns HTTP 200 for successful processing (even with some discarded rows)
  - Returns HTTP 400 for malformed CSV, empty request body, or invalid operations
  - Returns HTTP 408 for request timeout or cancellation
  - Returns HTTP 415 for unsupported content types
  - Configurable request size limit (default: 100MB)
  - Configurable request timeout (default: 5 minutes)
  - Custom CSV formatters using CsvHelper library for streaming CSV processing
- Health check endpoint at GET /health (US-011)
  - Returns HTTP 200 when service is healthy (product data loaded)
  - Returns HTTP 503 when service is unhealthy (product data not loaded)
  - JSON response includes: status, productDataLoaded, productCount, timestamp
  - Source-generated logging at Information level (healthy) or Warning level (unhealthy)
  - No authentication required
- Code coverage reporting in GitHub Actions CI pipeline
  - Coverlet collects coverage during test runs
  - ReportGenerator produces markdown summary
  - Coverage displayed in workflow job summary
  - Full report available as downloadable artifact
  - Dynamic coverage badge using shields.io endpoint
