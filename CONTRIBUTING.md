# Contributing

## Commit Message Guidelines

We follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification for commit messages.

### Commit Types Cheat Sheet

| Type       | Description                                  | Example                                       |
| ---------- | -------------------------------------------- | --------------------------------------------- |
| `feat`     | A new feature                                | `feat: add file upload functionality`         |
| `fix`      | A bug fix                                    | `fix: resolve memory leak in download method` |
| `docs`     | Documentation changes                        | `docs: update API usage examples`             |
| `style`    | Code style changes (formatting, whitespace)  | `style: fix indentation in GoogleDriveApi.cs` |
| `refactor` | Code restructuring without changing behavior | `refactor: simplify folder hierarchy logic`   |
| `perf`     | Performance improvements                     | `perf: optimize token refresh mechanism`      |
| `test`     | Adding or updating tests                     | `test: add unit tests for file download`      |
| `chore`    | Build/tooling changes, dependency updates    | `chore: update .NET SDK version`              |
| `build`    | Build system or dependency changes           | `build: update NuGet packages`                |
| `ci`       | CI/CD configuration changes                  | `ci: add GitHub Actions workflow`             |

### Breaking Changes

For breaking changes, append `!` after the type/scope:

- `feat!: remove deprecated method`
- `fix(api)!: change authentication flow`

### Format

```
<type>[optional scope]: <description>

[optional body]

[optional footer]
```

### Examples

```
feat: add support for folder deletion
fix(download): handle Google Docs export correctly
docs: update README with new authentication steps
chore: update dependencies to latest versions
```

For more details, see the [Conventional Commits specification](https://www.conventionalcommits.org/en/v1.0.0/).
