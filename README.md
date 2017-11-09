# Il2CppInspector
Extract types, methods, properties and fields from Unity IL2CPP binaries.

* Supports ELF (Android .so), PE (Windows .exe), Mach-O (Apple iOS/Mac) and Universal Binary (Fat Mach-O) file formats
* Supports ARMv7, ARMv7 Thumb T1 and x86 architectures regardless of file format
* Supports metadata versions 21, 22, 23 and 24
* No manual reverse-engineering required; all data is calculated automatically
* **Il2CppInspector** re-usable class library for low-level access to IL2CPP binaries and metadata
* **Il2CppReflector** re-usable class library for high-level .NET Reflection-style access to IL2CPP types and data as a tree model

Class library targets .NET Standard 1.5. Application targets .NET Core 2.0. Built with Visual Studio 2017.

### Build instructions

```
git clone --recursive https://github.com/djkaty/Il2CppInspector
cd Il2CppInspector
dotnet restore -r win7-x86
dotnet publish -c Release -r win7-x86
```

This will build Il2CppInspector for Windows 7 and later. For MacOS and Linux, replace `win7-x86` with a RID from https://docs.microsoft.com/en-us/dotnet/articles/core/rid-catalog

The output binary is placed in `Il2CppInspector/Il2CppDumper/bin/Release/netstandard1.6/win7-x86/publish`.

### Usage

```
Il2CppDumper [<binary-file> [<metadata-file> [<output-file>]]]
```

Defaults if not specified:

- _binary-file_ - searches for `libil2cpp.so` and `GameAssembly.dll`
- _metadata-file_ - `global-metadata.dat`
- _output-file_ - `types.cs`

File format and architecture are automatically detected.

For Apple Universal Binaries, multiple output files will be generated, with each filename suffixed by the index of the image in the Universal Binary. Unsupported images will be skipped.

### 64-bit binaries

Il2CppInspector does not currently support 64-bit IL2CPP binaries. 64-bit Mach-O files will be parsed without crashing but there is currently no support for 64-bit CPU architectures so automatic inspection will fail.

### Problems

If you have files that don't work or are in an unsupported format, please open a new issue on GitHub and attach a sample with details on the file format, and I'll try to add support.

### Acknowledgements

Thanks to the following individuals whose code and research helped me develop this tool:

- Perfare - https://github.com/Perfare/Il2CppDumper
- Jumboperson - https://github.com/Jumboperson/Il2CppDumper
- nevermoe - https://github.com/nevermoe/unity_metadata_loader
- branw - https://github.com/branw/pogo-proto-dumper
- fry - https://github.com/fry/d3
- ARMConvertor - http://armconverter.com

This tool uses Perfare's Il2CppDumper code as a base.

### License

All rights reserved. Unauthorized use, re-use or the creation of derivative works of this code for commercial purposes whether directly or indirectly is strictly prohibited. Use, re-use or the creation of derivative works for non-commercial purposes is expressly permitted.
