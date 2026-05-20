# Unit And Integration Tests

This module contains automated tests for the Ticketing System:

- Unit tests for DAL and API controllers (with Moq where appropriate)
- Integration tests for end-to-end ticket ordering flows (book/complete/fail/release)

## Structure

- `src/TicketingSystem.slnx` - workspace solution
- `src/TicketingSystem.UnitTests/` - unit tests
- `src/TicketingSystem.IntegrationTests/` - integration tests

## Prerequisites

- .NET SDK 10.0+

## Run tests

From repository root:

```bash
dotnet test src/TicketingSystem.slnx
```

## Run tests with coverage

```bash
dotnet test src/TicketingSystem.slnx --collect:"XPlat Code Coverage" --results-directory ./TestResultsCoverage
```

To generate a readable summary report (optional):

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet test src/TicketingSystem.slnx --collect:"XPlat Code Coverage" --results-directory ./TestResultsCoverage
reportgenerator -reports:"TestResultsCoverage/**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"TextSummary;JsonSummary;Html"
```
