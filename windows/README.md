# Dousha Windows

This directory contains the independent C#/.NET Windows Port. It is separate from the macOS Swift package at the repository root.

## Build and Test

```powershell
dotnet test .\Dousha.Windows.sln
```

## Portable Publish

```powershell
.\publish.ps1
```

The publish script writes the portable app files to `windows\artifacts\Dousha.Windows` by default. Manual acceptance should run `Dousha.Windows.App.exe` from that directory, not from `dotnet run`.
