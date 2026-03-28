# SharpHSQL Versioning Guide

This document explains how versioning works in SharpHSQL using GitVersion.

## Current Version

The project is currently at version **2.0.0** and uses **GitVersion** for automatic semantic versioning based on Git history.

## How It Works

GitVersion calculates the version number based on:
1. Git tags (e.g., `2.0.0`, `2.1.0`, `3.0.0`)
2. Branch names
3. Commit history since the last tag

## Version Format

SharpHSQL follows **Semantic Versioning (SemVer)**: `MAJOR.MINOR.PATCH`

- **MAJOR** (2): Incremented for breaking changes (e.g., 2.x.x → 3.0.0)
- **MINOR** (0): Incremented for new features (e.g., 2.0.x → 2.1.0)
- **PATCH** (0): Incremented for bug fixes (e.g., 2.0.0 → 2.0.1)

## Creating New Releases

### Patch Release (Bug Fixes) - 2.0.0 → 2.0.1

For bug fixes and small improvements:

```bash
# Make your bug fix commits
git commit -m "fix: Resolve transaction rollback issue"

# Tag the release
git tag -a 2.0.1 -m "Release 2.0.1 - Bug fixes"

# Push the tag
git push origin 2.0.1

# Build and publish
./build.ps1 -Configuration Release -CreatePackage -Publish -ApiKey "your-key"
```

### Minor Release (New Features) - 2.0.x → 2.1.0

For new features (backward compatible):

```bash
# Make your feature commits
git commit -m "feat: Add support for CASE statements"

# Tag the release
git tag -a 2.1.0 -m "Release 2.1.0 - New features"

# Push the tag
git push origin 2.1.0

# Build and publish
./build.ps1 -Configuration Release -CreatePackage -Publish -ApiKey "your-key"
```

### Major Release (Breaking Changes) - 2.x.x → 3.0.0

For breaking changes:

```bash
# Make your breaking change commits
git commit -m "feat!: Change ADO.NET provider API"

# Tag the release
git tag -a 3.0.0 -m "Release 3.0.0 - Breaking changes"

# Update GitVersion.yml next-version to 3.0.0
# Update CHANGELOG.md with breaking changes

# Push the tag
git push origin 3.0.0

# Build and publish
./build.ps1 -Configuration Release -CreatePackage -Publish -ApiKey "your-key"
```

## Pre-Release Versions

### Alpha (Development Branch)

```bash
git checkout -b develop
git commit -m "feat: Work in progress feature"
# Version will be: 2.1.0-alpha.1
```

### Beta (Feature Branches)

```bash
git checkout -b feature/new-feature
git commit -m "feat: New feature"
# Version will be: 2.1.0-new-feature.1
```

### Release Candidate

```bash
git checkout -b release/2.1.0
# Version will be: 2.1.0-rc.1
```

## Version in Different Scenarios

| Scenario | Example Version | Description |
|----------|----------------|-------------|
| Tagged main branch | `2.0.0` | Stable release |
| Main branch after tag | `2.0.1-1` | 1 commit after 2.0.0 tag |
| Develop branch | `2.1.0-alpha.5` | 5 commits on develop |
| Feature branch | `2.1.0-my-feature.3` | 3 commits on feature |
| Release branch | `2.1.0-rc.2` | Release candidate 2 |
| Hotfix branch | `2.0.1-beta.1` | Hotfix in progress |

## Checking Current Version

```bash
# View version as JSON
dotnet gitversion /output json

# View version in buildserver format
dotnet gitversion /output buildserver

# Show specific version component
dotnet gitversion /showvariable SemVer
```

## Build Script Versioning

The `build.ps1` script automatically:
1. Calculates version using GitVersion
2. Updates `AssemblyInfo.cs` with:
   - `AssemblyVersion`: e.g., `2.0.0.0`
   - `AssemblyFileVersion`: e.g., `2.0.0.0`
   - `AssemblyInformationalVersion`: e.g., `2.0.0+Branch.main.Sha.60810d2`
3. Sets the NuGet package version

## Best Practices

### 1. Always Tag Releases
```bash
git tag -a 2.0.1 -m "Release 2.0.1"
git push origin 2.0.1
```

### 2. Use Conventional Commits
```bash
feat: Add new feature
fix: Fix bug
docs: Update documentation
chore: Update dependencies
```

### 3. Update CHANGELOG.md
Always update the CHANGELOG.md before tagging a release:
```markdown
## [2.1.0] - 2026-04-01
### Added
- New CASE statement support
- Additional string functions
```

### 4. Test Before Tagging
```bash
# Run full build and tests
./build.ps1 -Configuration Release

# Only tag if all tests pass
git tag -a 2.1.0 -m "Release 2.1.0"
```

## GitVersion Configuration

The `GitVersion.yml` file controls versioning behavior:

```yaml
mode: ContinuousDelivery
next-version: 2.0.0  # Base version if no tags exist
branches:
  main:
    increment: Patch  # Auto-increment patch version
    is-mainline: true
  develop:
    increment: Minor  # Auto-increment minor version
    tag: alpha
```

## Troubleshooting

### Version Shows 0.0.1
**Problem**: No tags in repository

**Solution**:
```bash
git tag -a 2.0.0 -m "Initial release 2.0.0"
git push origin 2.0.0
```

### Version Not Updating
**Problem**: GitVersion cache

**Solution**:
```bash
# Clear cache
rm -rf .git/gitversion_cache
dotnet gitversion /nocache
```

### Wrong Version Number
**Problem**: Incorrect tag or branch configuration

**Solution**:
```bash
# Check current tags
git tag -l

# Check GitVersion calculation
dotnet gitversion /showconfig
```

## Examples

### Release Workflow

```bash
# 1. Finish your feature work
git checkout main
git pull origin main

# 2. Update version in CHANGELOG.md
# Edit CHANGELOG.md to document changes

# 3. Commit changes
git add CHANGELOG.md
git commit -m "chore: Prepare for 2.1.0 release"

# 4. Tag the release
git tag -a 2.1.0 -m "Release 2.1.0"

# 5. Push tag
git push origin main
git push origin 2.1.0

# 6. Build and publish
./build.ps1 -Configuration Release -CreatePackage -Publish -ApiKey "your-key"
```

### Hotfix Workflow

```bash
# 1. Create hotfix branch from tagged version
git checkout -b hotfix/2.0.1 2.0.0

# 2. Fix the bug
git commit -m "fix: Critical bug in transaction handling"

# 3. Tag the hotfix
git tag -a 2.0.1 -m "Release 2.0.1 - Hotfix"

# 4. Merge back to main
git checkout main
git merge hotfix/2.0.1

# 5. Push
git push origin main
git push origin 2.0.1

# 6. Build and publish
./build.ps1 -Configuration Release -CreatePackage -Publish -ApiKey "your-key"
```

## Additional Resources

- [GitVersion Documentation](https://gitversion.net/docs/)
- [Semantic Versioning Spec](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)

---

**Current Status**: Version 2.0.0 tagged and ready for development!
