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

<!-- Add new decisions above this line -->
