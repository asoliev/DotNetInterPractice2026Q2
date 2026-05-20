# Unit And Integration Tests

This module contains automated tests for the Ticketing System:

- Unit tests for DAL and API controllers (with Moq where appropriate)
- Integration tests for end-to-end ticket ordering flows (book/complete/fail/release)

## Structure

- `sources/TicketingSystem.Tests.slnx` - test solution
- `sources/TicketingSystem.UnitTests/` - unit tests
- `sources/TicketingSystem.IntegrationTests/` - integration tests

## Prerequisites

- .NET SDK 10.0+

## Run tests

From repository root:

```bash
cd 05_unit_and_integration_tests/sources
dotnet test TicketingSystem.Tests.slnx
```

## Run tests with coverage

```bash
cd 05_unit_and_integration_tests/sources
dotnet test TicketingSystem.Tests.slnx --collect:"XPlat Code Coverage" --results-directory ./TestResultsCoverage
```

To generate a readable summary report (optional):

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
cd 05_unit_and_integration_tests/sources
reportgenerator -reports:"TestResultsCoverage/**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"TextSummary;JsonSummary;Html"
```
