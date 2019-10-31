# Il2CppInspector
Extract types, methods, properties and fields from Unity IL2CPP binaries.

* **No manual reverse-engineering required; all data is calculated automatically!**
* Supports ELF (Android .so), PE (Windows .exe), Mach-O (Apple iOS/Mac) and Universal Binary (Fat Mach-O) file formats
* 32-bit and 64-bit support for all file formats
* Supports ARMv7, Thumb-2, ARMv8 (A64), x86 and x64 architectures regardless of file format
* Supports metadata versions 16, 21, 22, 23, 24, 24.1 (Unity 2018.3+) and 24.2 (Unity 2019+) (other versions may or may not work)
* Support for classes, methods, constructors, fields, properties, enumerations, events, delegates, interfaces, structs, nested types and default field values
* Static symbol table scanning for ELF and Mach-O binaries if present
* Dynamic symbol table scanning for ELF binaries if present
* Symbol relocation handling for ELF binaries
* **Il2CppInspector** re-usable class library for low-level access to IL2CPP binaries and metadata
* **Il2CppReflector** re-usable class library for high-level .NET Reflection-style access to IL2CPP types and data as a tree model
* Test chassis for automated integration testing of IL2CPP binaries

Class library targets .NET Standard 2.1. Application targets .NET Core 3.0. Built with Visual Studio 2019.

### Build instructions

```
git clone --recursive https://github.com/djkaty/Il2CppInspector
cd Il2CppInspector
dotnet publish -c Release
```

This will build Il2CppInspector for Windows 64-bit. For MacOS and Linux, add  `-r xxx` to the final command where `xxx` is a RID from https://docs.microsoft.com/en-us/dotnet/articles/core/rid-catalog

The output binary is placed in `Il2CppInspector/Il2CppDumper/bin/Release/netcoreapp3.0/win-x64/publish`.

### Usage

```
Il2CppDumper [--bin=<binary-file>] [--metadata=<metadata-file>] [--cs-out=<output-file>] [--exclude-namespaces=<ns1,ns2,...>|none]
```

Defaults if not specified:

- _binary-file_ - searches for `libil2cpp.so`
- _metadata-file_ - `global-metadata.dat`
- _output-file_ - `types.cs`

To exclude types from certain namespaces from being generated in the C¤ source file output, provide a comma-separated list of case-sensitive namespaces in `--exclude-namespaces`. The following namespaces will be excluded if no argument is specified:

```
System
Mono
UnityEngine
Microsoft.Win32
<the root (empty string) namespace>
```

Providing an argument to `--exclude-namespaces` will override the default list. To output all namespaces, use `--exclude-namespaces=none`.


File format and architecture are automatically detected.

For Apple Universal Binaries, multiple output files will be generated, with each filename besides the first suffixed by the index of the image in the Universal Binary. Unsupported images will be skipped.

### Running tests

Two Powershell scripts are provided to enable easy testing and debugging:

* `generate-binaries.ps1` compiles every C# source file in `TestSources` as a separate assembly and outputs them to `TestAssemblies`. It then takes every assembly in `TestAssemblies` and compiles each one as a separate IL2CPP project twice: one for Windows x86 standalone and one for Android into the `TestBinaries`folder. It then calls `generate-tests.ps1`.
* `generate-tests.ps1` generates a file called `Tests.cs` in the `Il2CppTests` project, containing one test per IL2CPP project in `TestBinaries`. This file will be compiled by the `Il2CppTests`project. You will then be able to see one test per IL2CPP project in Visual Studio's Test Explorer.

The auto-generated tests generate a file in the test IL2CPP binary's folder called `test-result.cs` and compares it (whitespace-insensitive) with the corresponding project name `cs` file in `TestExpectedResults`. In this way, you can check for files with known structure that the analysis is being performed correctly, or step through the analysis of specific binaries in the debugger without having to change the project's command-line arguments.

### Problems

If you have files that don't work or are in an unsupported format, please open a new issue on GitHub and attach a sample with details on the file format, and I'll try to add support.

### Acknowledgements

Thanks to the following individuals whose code and research helped me develop this tool:

- Perfare - https://github.com/Perfare/Il2CppDumper
- Jumboperson - https://github.com/Jumboperson/Il2CppDumper
- nevermoe - https://github.com/nevermoe/unity_metadata_loader
- branw - https://github.com/branw/pogo-proto-dumper
- fry - https://github.com/fry/d3
- ARMConverter - http://armconverter.com
- Defuse - https://defuse.ca/online-x86-assembler.htm

This tool uses Perfare's Il2CppDumper code as a base.

### License

All rights reserved. Unauthorized use, re-use or the creation of derivative works of this code for commercial purposes whether directly or indirectly is strictly prohibited. Use, re-use or the creation of derivative works for non-commercial purposes is expressly permitted.
