# Testing

The project can be tested simply by running

```sh
dotnet test
```

## Coverage
Some test projects also include instrumentation to compute code coverage. It can
be generated and trasformed into a report by invoking

```sh
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:TestResults/your-guid-goes-here/coverage.cobertura.xml -targetdir:coveragereport
```

This assumes that ReportGenerator is installed as a global tool, i.e.
```sh
dotnet tool install -g dotnet-reportgenerator-globaltool
```

## Public API compatibility
While no stable API is guaranteed right now, it is already possible to track the
public APIs exposed by a package and update the `PublicAPI.Unshipped.txt` file
as follows:

```sh
dotnet format --include-generated -a warn
```

or, in a .NET 6 environment, with:
```sh
dotnet format --include-generated --severity warn analyzers
```

This will eventually enable having a stable API and avoiding (undesired)
breaking changes.
