# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET 10 CLI tool (`cf-deploy`) distributed as a global NuGet package for deploying AWS CloudFormation stacks. It reads a YAML project file that defines stacks, variables, tags, and S3 buckets, then orchestrates deployment/deletion with macro-based parameter resolution.

## Build & Test Commands

```bash
dotnet build                    # Build the solution
dotnet test                     # Run all xUnit tests
dotnet test --filter "FullyQualifiedName~MacroPatternTests"  # Run a single test class
dotnet pack -c Release          # Pack NuGet package
```

Solution file: `MbUtils.CloudFormationStackDeployer.slnx`

## Architecture

**CLI layer** (`Commands/`): Spectre.Console commands — `DeployCommand`, `DeleteCommand`, `EmptyBucketCommand`. Each command reads project config, prompts for stack/bucket selection, then delegates to a process class.

**Process layer** (`DeploymentProcess.cs`, `StackDeletionProcess.cs`): Orchestrates AWS operations. Deploy checks existence → creates/updates → polls status → displays outputs. Delete checks existence → initiates delete → polls.

**Context** (`DeploymentContext.cs`): Resolves macro expressions in parameters and tags. Two macro types:
- `${variables.KEY}` — resolved from project-level variables
- `${outputs.STACK.OUTPUT}` — resolved from CloudFormation stack outputs (lazy-cached)

Pattern regexes live in `Patterns.cs`.

**AWS extensions** (`CloudFormation/`, `S3/`, `SecurityToken/`): Extension methods on AWS SDK clients wrapping CF, S3, and STS operations.

**Configuration** (`Configuration/`): YAML deserialization via YamlDotNet. `ProjectConfiguration` → `StackConfiguration` models.

## Key Patterns

- **Discriminated unions** via the `Dunet` library for type-safe result handling (`DeployResult`, `StackDeletionResult`, `WorkflowErrors`). Uses exhaustive pattern matching.
- **Extension methods** for AWS client operations (declared with Dunet's `extension` keyword pattern).
- **DI** via `Microsoft.Extensions.DependencyInjection` configured in `Program.cs`.

## CI/CD

- GitHub Actions: tests on push/PR to `main` (`.github/workflows/main.yml`)
- NuGet publish on version tags `v*` (`.github/workflows/pack_and_publish.yml`)
