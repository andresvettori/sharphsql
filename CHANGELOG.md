# Changelog

All notable changes to SharpHSQL will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Automated build script with GitVersion support
- Comprehensive documentation suite
- NuGet package configuration
- Quick Start guide
- Technical Reference documentation

## [2.0.0] - 2026-03-27

### Added
- Migrated to .NET Standard 2.0 for broader compatibility
- Enhanced logging with Serilog integration
- Comprehensive test suite with 100+ unit tests
- Support for DECLARE variables
- LIMIT clause for SELECT queries
- CASEWHEN function for conditional expressions
- Extended mathematical functions (ACOS, ASIN, ATAN, ATAN2, etc.)
- DATEDIFF function for date calculations
- EXISTS function to check table existence
- Reliability tests for database lifecycle management

### Changed
- Modernized codebase to use C# latest features
- Improved ADO.NET provider compatibility
- Enhanced error handling and reporting
- Updated dependencies to latest stable versions
- Improved transaction handling
- Better NULL value handling

### Fixed
- Memory leaks in long-running operations
- Transaction rollback edge cases
- Date/time conversion issues
- String function encoding issues
- Index corruption on concurrent access
- PRIMARY KEY constraint validation

### Performance
- Optimized SELECT query execution
- Improved index traversal algorithms
- Reduced memory footprint for large result sets
- Faster transaction commit/rollback
- Enhanced caching mechanisms

## [1.0.0] - Initial Release

### Added
- Core database engine ported from HSQLDB
- In-memory and file-based storage modes
- Full SQL support (SELECT, INSERT, UPDATE, DELETE)
- DDL commands (CREATE TABLE, DROP TABLE, ALTER TABLE, CREATE INDEX)
- Transaction support (BEGIN, COMMIT, ROLLBACK)
- INNER JOIN and LEFT OUTER JOIN
- Subqueries in SELECT, FROM, and WHERE clauses
- Set operations (UNION, INTERSECT, EXCEPT)
- Aggregate functions (COUNT, SUM, AVG, MIN, MAX)
- 80+ built-in functions (math, string, date/time)
- ADO.NET provider implementation
- Native API for direct database access
- Primary key and index support
- Multiple data types (numeric, string, binary, date/time, boolean)

### Supported SQL Features
- Data Definition Language (DDL)
- Data Manipulation Language (DML)
- Transaction Control
- Advanced SELECT with GROUP BY, HAVING, ORDER BY
- Complex joins and subqueries
- Parameterized queries
- Batch operations

---

## Version History Summary

| Version | Release Date | Major Changes |
|---------|--------------|---------------|
| 2.0.0   | 2026-03-27   | .NET Standard 2.0, Enhanced features, Comprehensive tests |
| 1.0.0   | -            | Initial release, Core functionality |

---

## Upgrade Guide

### Upgrading from 1.x to 2.0

**Breaking Changes:**
- .NET Standard 2.0 now required (minimum .NET Framework 4.6.1 or .NET Core 2.0)
- Some internal APIs have changed (affects custom function implementations)

**Migration Steps:**
1. Update your project to target .NET Standard 2.0 or higher
2. Update NuGet package reference to 2.0.0
3. Test your application thoroughly, especially:
   - Transaction handling
   - Custom functions (if any)
   - Date/time operations
   - NULL value handling

**New Features You Can Use:**
- DECLARE variables for more complex SQL scripts
- LIMIT clause for pagination
- CASEWHEN for inline conditionals
- Extended mathematical functions
- DATEDIFF for date calculations

**Example Migration:**
```csharp
// Old (1.x)
var result = database.Execute("SELECT * FROM Users", channel);

// New (2.0) - Still compatible, but you can now use:
var result = database.Execute("SELECT LIMIT 0, 10 * FROM Users", channel);

// Or with variables:
database.Execute("DECLARE @userId INT", channel);
database.Execute("SET @userId = 1", channel);
var result = database.Execute("SELECT * FROM Orders WHERE UserId = @userId", channel);
```

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for information on how to contribute to this project.

---

## Support

- **Issues**: Report bugs and request features on [GitHub Issues](https://github.com/andresvettori/sharphsql/issues)
- **Discussions**: Ask questions on [GitHub Discussions](https://github.com/andresvettori/sharphsql/discussions)
- **Documentation**: Read the full docs at [docs/](docs/)

---

[Unreleased]: https://github.com/andresvettori/sharphsql/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/andresvettori/sharphsql/releases/tag/v2.0.0
[1.0.0]: https://github.com/andresvettori/sharphsql/releases/tag/v1.0.0
