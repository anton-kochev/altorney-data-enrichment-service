# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial project structure for Trade Data Enrichment Service
- Product reference data loading at startup from configurable CSV file
  - Enables mapping of product IDs to product names for trade enrichment
  - Invalid rows are skipped and logged without blocking the service
  - Service fails fast if product file is missing or unreadable
