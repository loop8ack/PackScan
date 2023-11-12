dotnet pack -o artifacts -c Release ./src/PackScan.Analyzer
dotnet pack -o artifacts -c Release ./src/PackScan.Defaults
dotnet pack -o artifacts -c Release ./src/PackScan.Tool
dotnet pack -o artifacts -c Release ./src/PackScan.PackagesProvider.Abstractions
