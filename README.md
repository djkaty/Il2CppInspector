# Il2CppInspector
Easily extract types and metadata from IL2CPP binaries.

* **No manual reverse-engineering required; all data is calculated automatically!**
* Supports ELF (Android .so), PE (Windows .exe), Mach-O (Apple iOS/Mac) and Universal Binary (Fat Mach-O) file formats
* 32-bit and 64-bit support for all file formats
* Supports ARMv7, Thumb-2, ARMv8 (A64), x86 and x64 architectures regardless of file format
* Supports applications created with Unity 5.3.0 onwards (full IL2CPP version table below)
* Support for assemblies, classes, methods, constructors, fields, properties, enumerations, events, interfaces, structs, pointers, references, attributes, nested types, generic types, generic methods, generic constraints, default field values and default method parameter values
* C# syntactic sugar for CTS value types, compiler-generated types, delegates, extension methods, operator overloading, indexers, user-defined conversion operators, explicit interface instantiations, finalizers, nullable types, unsafe contexts, fixed-size arrays, variable length argument lists, method hiding and escaped strings
* Partition C# code output by namespace, assembly, class or single file; sort by index or type name; output flat or nested folder hierarchy. Each file includes the necessary `using` directives. Scope and type name conflicts are resolved automatically to produce code that compiles.
* Static symbol table scanning for ELF and Mach-O binaries if present
* Dynamic symbol table scanning for ELF binaries if present
* Symbol relocation handling for ELF binaries
* **Il2CppInspector** re-usable class library for low-level access to IL2CPP binaries and metadata
* **Il2CppModel** re-usable class library for high-level .NET Reflection-style access to IL2CPP types and data as a tree model
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

Run `Il2CppDumper.exe` at the command prompt.

File format and architecture are automatically detected.

```
  -i, --bin                   Required. (Default: libil2cpp.so) IL2CPP binary file input
  -m, --metadata              Required. (Default: global-metadata.dat) IL2CPP metadata file input
  -c, --cs-out                (Default: types.cs) C# output file (when using single-file layout) or path (when using per namespace, assembly or class layout)
  -e, --exclude-namespaces    (Default: System Unity UnityEngine UnityEngineInternal Mono Microsoft.Win32) Comma-separated list of namespaces to suppress in C# output, or 'none' to include all namespaces
  -l, --layout                (Default: single) Partitioning of C# output ('single' = single file, 'namespace' = one file per namespace, 'assembly' = one file per assembly, 'class' = one file per class)
  -s, --sort                  (Default: index) Sort order of type definitions in C# output ('index' = by type definition index, 'name' = by type name). No effect when using file-per-class layout
  -f, --flatten               Flatten the namespace hierarchy into a single folder rather than using per-namespace subfolders. Only used when layout is per-namespace or per-class
  -n, --suppress-metadata     Diff tidying: suppress method pointers, field offsets and type indices from C# output. Useful for comparing two versions of a binary for changes with a diff tool
  -k, --must-compile          Compilation tidying: try really hard to make code that compiles. Suppress generation of code for items with CompilerGenerated attribute. Comment out attributes without parameterless constructors or all-optional constructor arguments. Don't emit add/remove/raise on events. Specify AttributeTargets.All on classes with AttributeUsage attribute. Force auto-properties to have get accessors. Force regular properties to have bodies.
```

Defaults if not specified:

- _bin_ - `libil2cpp.so`
- _metadata_ - `global-metadata.dat`
- _cs-out_ - `types.cs`

To exclude types from certain namespaces from being generated in the C# source file output, provide a comma-separated list of case-sensitive namespaces in `--exclude-namespaces`. The following namespaces will be excluded if no argument is specified:

```
System
Mono
Unity
UnityEngine
UnityEngineInternal
Microsoft.Win32
```

Providing an argument to `--exclude-namespaces` will override the default list. To output all namespaces, use `--exclude-namespaces=none`.

By default, types and fields declared with the `System.Runtime.CompilerServices.CompilerGeneratedAttribute` attribute will be suppresssed from the C# code output. The attribute itself will be suppressed from property getters and setters. This is useful if you would like to be able to compile the output code. To include these constructs in the output, use `--no-suppress-cg`.

For Apple Universal Binaries, multiple output files will be generated, with each filename besides the first suffixed by the index of the image in the Universal Binary. Unsupported images will be skipped.

### Running tests

Two Powershell scripts are provided to enable easy testing and debugging:

* `generate-binaries.ps1` compiles every C# source file in `TestSources` as a separate assembly and outputs them to `TestAssemblies`. It then takes every assembly in `TestAssemblies` and compiles each one as a separate IL2CPP project twice: one for Windows x86 standalone and one for Android into the `TestBinaries`folder. It then calls `generate-tests.ps1`.
* `generate-tests.ps1` generates a file called `Tests.cs` in the `Il2CppTests` project, containing one test per IL2CPP project in `TestBinaries`. This file will be compiled by the `Il2CppTests`project. You will then be able to see one test per IL2CPP project in Visual Studio's Test Explorer.

The auto-generated tests generate a file in the test IL2CPP binary's folder called `test-result.cs` and compares it (whitespace-insensitive) with the corresponding project name `cs` file in `TestExpectedResults`. In this way, you can check for files with known structure that the analysis is being performed correctly, or step through the analysis of specific binaries in the debugger without having to change the project's command-line arguments.

### Version support

Unity version | IL2CPP version | Support
--- | --- | ---
4.6.1+ | First release | Unsupported
5.2.x | 15 | Unsupported
5.3.0-5.3.1 | 16 | Working
5.3.2 | 19 | Untested
5.3.3-5.3.4 | 20 | Untested
5.3.5-5.4.x | 21 | Working
5.5.x | 22 | Working
5.6.x | 23 | Working
2017.x-2018.2 | 24.0 | Working
2018.3-2019.x | 24.1 | Working
2019.x+ | 24.2 | Working

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
