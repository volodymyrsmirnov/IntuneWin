# IntuneWin File

IntuneWin is the package format for Windows 10 deployments helping to solve any roadblocker by offering modern packaging, CDN, delivery optimization.

For Intune aka Microsoft Endpoint Manager you can upload only a single file as an installation source. Typically, that's not the case when we talk about old-school thick apps with many files lying near the Setup.exe. 

So, Microsoft made a simple container format, which is in fact a ZIP archive that contains XML with metadata and another encrypted and checksumed ZIP archive with installation content in it.

**Important**: the file format is not publicly documented and the library is based on the information found on the internet, so some of the features might be missing or misbehave.

## Installation 

### Prerequisites

* .NET Framework 4.6.1+
* .NET Core 3.0+
* .NET Standard 2.0+

### Install Package

```cmd
dotnet add package IntuneWinFile
```

## Documentation

Please see documentation string in [IntuneWinFile.cs](IntuneWin/IntuneWinFile.cs), [IntuneWin.Data](IntuneWin/Data) and [IntuneWin.exceptions](IntuneWin/Exceptions) namespaces.

You can also check [tests](IntuneWin.Tests/IntuneWinTests.cs) for basic usage examples.