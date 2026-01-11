# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
    - X-Enrichment-Missing-Product-Ids: comma-separated list of missing IDs
  - Returns HTTP 200 for successful processing (even with some discarded rows)
  - Returns HTTP 400 for malformed CSV or empty request body
  - Returns HTTP 415 for unsupported content types
  - Configurable request size limit (default: 100MB) via appsettings.json
  - Custom CSV formatters using Sep library for high-performance parsing
