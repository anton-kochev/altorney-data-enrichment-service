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
- Field preservation with automatic whitespace trimming (US-004)
  - Date, currency, and price fields are preserved through enrichment
  - Leading/trailing whitespace is automatically trimmed from all fields
  - Original field values (after trimming) are maintained in output
