# Il2CppInspector 2020.2 beta

Il2CppInspector helps you to reverse engineer IL2CPP applications, providing the most complete analysis currently available.

Main features:

* Output IL2CPP type definitions, metadata and method pointers as C# stub code

* Create C++ scaffolding for all types, methods and API functions in an IL2CPP application

* Create IDA Python scripts to populate symbol, function and type information

* Create Visual Studio C++ DLL injection projects directly from IL2CPP files

* Create Visual Studio C# code stub solutions directly from IL2CPP files

* Create IL2CPP binaries from arbitrary C# source code without a Unity project

* Three major APIs for use in custom static analysis projects

* Supports all major file formats and processor architectures

* Defeats certain types of basic obfuscation

* Works on Windows, MacOS X and Linux. Integrated GUI for Windows users

* Tested with every release of IL2CPP since Unity 5.3.0

You can read more about IL2CPP in my series [IL2CPP Reverse Engineering](https://katyscode.wordpress.com/category/reverse-engineering/il2cpp/).

**NOTE**: Il2CppInspector is not a decompiler. It can provide you with the structure of an application and function addresses for every method so that you can easily jump straight to methods of interest in your disassembler. It does not attempt to recover the entire source code of the application.

![Il2CppInspector GUI](docs/GUI_Preview.png)


File format and architecture support:

* Supports ELF (Android .so), PE (Windows .exe), Mach-O (Apple iOS/Mac), Universal Binary (Fat Mach-O) and FSELF (PlayStation 4 .prx/.sprx) file formats
* Also supports APK (Android) and decrypted IPA (iOS) application package files as input
* 32-bit and 64-bit support for all file formats
* Supports ARMv7, Thumb-2, ARMv8 (A64), x86 and x64 architectures regardless of file format
* Supports applications created with Unity 5.3.0 onwards (full IL2CPP version table below)

Nice to have:

* Support for assemblies, classes, methods, constructors, fields, properties, enumerations, events, interfaces, structs, pointers, references, attributes, nested types, generic types, generic methods, generic constraints, default field values and default method parameter values
* C# syntactic sugar for CTS value types, compiler-generated types, delegates, extension methods, operator overloading, indexers, user-defined conversion operators, explicit interface instantiations, finalizers, nullable types, unsafe contexts, fixed-size arrays, variable length argument lists, method hiding and escaped strings
* Partition C# code output by namespace, assembly, class, full tree or single file; sort by index or type name; output flat or nested folder hierarchy. Each file includes the necessary `using` directives. Scope and type name conflicts are resolved automatically to produce code that compiles.
* API function export processing for PE, ELF and Mach-O binaries
* Static and dynamic symbol table scanning and relocation processing for ELF binaries 
* Static symbol table scanning for Mach-O binaries
* Automatically defeats certain basic obfuscation methods

Reusable class library APIs:

* **Il2CppInspector** - low-level access to the binary image and metadata
* **TypeModel** - high-level .NET Reflection-like query API for all of the .NET types in the source project as a tree model
* **ApplicationModel** - access to all of the C++ types and methods, plus the IL2CPP API exports, with detailed address and offset data and mappings to their .NET equivalents

  Use these APIs to easily query IL2CPP types, create new output modules and integrate Il2CppInspector with your own static analysis applications.

* Test chassis for automated integration testing of IL2CPP binaries

Class library targets .NET Standard 2.1. Application targets .NET Core 3.0. Built with Visual Studio 2019.

### Build instructions

```
git clone --recursive https://github.com/djkaty/Il2CppInspector
cd Il2CppInspector
```

##### Windows

Build the CLI and Windows GUI versions:

```
dotnet publish -c Release
```

##### Mac OS X

Build the CLI version:

```
cd Il2CppInspector.CLI
dotnet publish -r osx-x64 -c Release
```

##### Linux

Build the CLI version:

```
cd Il2CppInspector.CLI
dotnet publish -r linux-x64 -c Release
```

For other operating systems supporting .NET Core, add  `-r xxx` to the final command where `xxx` is a RID from https://docs.microsoft.com/en-us/dotnet/articles/core/rid-catalog

The output binary for command-line usage is placed in `Il2CppInspector/Il2CppInspector.CLI/bin/Release/netcoreapp3.0/[win|osx|linux]-x64/publish/Il2CppInspector.exe`.

The output binary for Windows GUI is placed in `Il2CppInspector/Il2CppInspector.GUI/bin/Release/netcoreapp3.1/[win|osx|linux]-x64/publish/Il2CppInspector.exe`.

### Command-line Usage

Run `Il2CppInspector.exe` at the command prompt.

File format and architecture are automatically detected.

```
  -i, --bin                   (Default: libil2cpp.so) IL2CPP binary, APK or IPA input file
  -m, --metadata              (Default: global-metadata.dat) IL2CPP metadata file input (ignored for APK/IPA)
  -c, --cs-out                (Default: types.cs) C# output file (when using single-file layout) or path (when using per namespace, assembly or class layout)
  -p, --py-out                (Default: ida.py) IDA Python script output file
  -h, --cpp-out               (Default: cpp) C++ scaffolding / DLL injection project output path
  -e, --exclude-namespaces    (Default: System Mono Microsoft.Reflection Microsoft.Win32 Internal.Runtime Unity UnityEditor UnityEngine UnityEngineInternal AOT JetBrains.Annotations) Comma-separated list of
                              namespaces to suppress in C# output, or 'none' to include all namespaces
  -l, --layout                (Default: single) Partitioning of C# output ('single' = single file, 'namespace' = one file per namespace in folders, 'assembly' = one file per assembly, 'class' = one file per
                              class in namespace folders, 'tree' = one file per class in assembly and namespace folders)
  -s, --sort                  (Default: index) Sort order of type definitions in C# output ('index' = by type definition index, 'name' = by type name). No effect when using file-per-class or tree layout
  -f, --flatten               Flatten the namespace hierarchy into a single folder rather than using per-namespace subfolders. Only used when layout is per-namespace or per-class. Ignored for tree layout
  -n, --suppress-metadata     Diff tidying: suppress method pointers, field offsets and type indices from C# output. Useful for comparing two versions of a binary for changes with a diff tool
  -k, --must-compile          Compilation tidying: try really hard to make code that compiles. Suppress generation of code for items with CompilerGenerated attribute. Comment out attributes without
                              parameterless constructors or all-optional constructor arguments. Don't emit add/remove/raise on events. Specify AttributeTargets.All on classes with AttributeUsage attribute.
                              Force auto-properties to have get accessors. Force regular properties to have bodies. Suppress global::Locale classes. Generate dummy parameterless base constructors and ref
                              return fields.
  --separate-attributes       Place assembly-level attributes in their own AssemblyInfo.cs files. Only used when layout is per-assembly or tree
  -j, --project               Create a Visual Studio solution and projects. Implies --layout tree, --must-compile and --separate-attributes
  --cpp-compiler              (Default: BinaryFormat) Compiler to make C++ output compatible with (MSVC or GCC); selects based on binary executable type by default
  --unity-path                (Default: C:\Program Files\Unity\Hub\Editor\*) Path to Unity editor (when using --project). Wildcards select last matching folder in alphanumeric order
  --unity-assemblies          (Default: C:\Program Files\Unity\Hub\Editor\*\Editor\Data\Resources\PackageManager\ProjectTemplates\libcache\com.unity.template.3d-*\ScriptAssemblies) Path to Unity script
                              assemblies (when using --project). Wildcards select last matching folder in alphanumeric order
  --unity-version             Version of Unity used to create the input files, if known. Used to enhance IDAPython and C++ output. If not specified, a close match will be inferred automatically.
  --help                      Display this help screen.
  --version                   Display version information.
```

For Apple Universal Binaries, multiple output files will be generated, with each filename besides the first suffixed by the index of the image in the Universal Binary. Unsupported images will be skipped.

For IPA packages, the executable must be decrypted first. Encrypted executable binaries are not supported.

### Creating C# prototypes

To exclude types from certain namespaces from being generated in the C# source file output, provide a comma-separated list of case-sensitive namespaces in `--exclude-namespaces`. The following namespaces will be excluded if no argument is specified:

```
System
Mono
Microsoft.Reflection
Microsoft.Win32
Internal.Runtime
Unity
UnityEditor
UnityEngine
UnityEngineInternal
AOT
JetBrains.Annotations
```

Providing an argument to `--exclude-namespaces` will override the default list. To output all namespaces, use `--exclude-namespaces=none`. This only applies to C# prototypes output.

### Adding metadata to your IDA workflow

Simply run Il2CppInspector with the `-p` switch to choose the IDA script output file. Load your binary file into IDA, press Alt+F7 and select the generated script. Observe the Output Window while IDA analyzes the file - this may take a long time.

If you know which version of Unity the binary was compiled with, you can improve the output by specifying this with `--unity-version`, for example `--unity-version 2019.3.1f1`. Otherwise Il2CppInspector will make an educated guess based on the contents of the binary.

Il2CppInspector generates the following data for IDA projects:

- Type declarations for all IL2CPP internal types
- Type declarations for every type in the IL2CPP application including generic instances
- Addresses for every known type
- Names for all regular .NET methods
- Names for all constructed generic methods
- Names and typed signatures for all IL2CPP custom attributes generator functions
- Names, .NET argument type lists and typed signatures for all IL2CPP runtime invoker functions for both regular and constructed generic methods (per-signature Method.Invoke endpoints)
- Function boundaries for all of the above
- Comments at each function entry point with .NET method signatures for all of the above
- Names and type declarations for all of the following IL metadata references: Type, TypeInfo, MethodDef, FieldInfo, StringLiteral, MethodRef (this includes all generic class and method instantiation metadata)
- Comments for all IL string literal metadata pointers containing the value of the string
- Names for some IL2CPP-specific data structures and functions

Example IDA C++ decompilation after applying Il2CppInspector (initialization code omitted for brevity):

![Il2CppInspector annotated IDA project](docs/IDA_Preview.png)

### Creating C++ scaffolding or a DLL injection project

Il2CppInspector generates a series of C++ source files which you can use with a tool like `x64dbg` to analyze the memory of the application, or for accessing types, methods and IL2CPP API functions via DLL injection, among other uses.

If you know which version of Unity the binary was compiled with, you can improve the output by specifying this with `--unity-version`, for example `--unity-version 2019.3.1f1`. Otherwise Il2CppInspector will make an educated guess based on the contents of the binary.

You can target which C++ compiler you wish to use the output files with: specify `--cpp-compiler MSVC` for Visual Studio and `--cpp-compiler GCC` for gcc or clang.

Il2CppInspector performs automatic name conflict resolution to avoid the use of pre-defined symbols and keywords in C++, and to handle re-definition of same-named symbols in the application.

Some IL2CPP binary files contain only a partial set of API exports, or none at all. For these cases, Il2CppInspector will build scaffolding using only the available exports to ensure that the project compiles successfully.

![Il2CppInspector GUI](docs/Cpp_Preview.png)

The following files are generated:

- `ilc2pp-types.h`:
  - Type declarations for all internal IL2CPP types (a minimal version of the Unity headers)
  - Type declarations for every type used in the application including all arrays, enums, concrete generic type instances and inferred usages from metadata.
  - Boxed versions for types where applicable
  - VTables for every type

- `il2cpp-functions.h`:
  - The function pointer signature and offset from the image base address to every C#-equivalent method

- `il2cpp-type-ptr.h`:
  - The offset from the image base address to every type information class (`Il2CppClass **`)

- `il2cpp-function-ptr.h`:
  - The offset from the image base address to every IL2CPP API function export (functions starting with `il2cpp_`)

- `il2cpp-api-functions.h`:
  - The function pointer signature to every IL2CPP API function (copied directly from Unity for the version used to compile the binary). Functions not found in the binary's export list will be elided

The above files contain all the data needed for dynamic analysis in a debugger.

In addition, the following files are generated for DLL injection:

- `il2cpp-init.h`:
  - Provides the `void init_il2cpp()` function which uses all of the above headers to generate usable function pointers and class pointers that are mapped to the correct places in the in-memory image at runtime

- `dllmain.cpp` and `dllmain.h`:
  - Provides a DLL injection stub which calls `init_il2cpp()` and starts `Run()` (see below) in a new thread

- _`helpers.cpp` and `helpers.h`:
  - Provides basic logging (`LogWrite(std::string text)`) and other helper functions. See the comments in `helpers.h` for details. To specify a log file target in your source code, use `extern const LPCWSTR LOG_FILE = L"my_log_file.txt"`

- `main.cpp`:
  - Contains a stub `Run()` function where you can enter your custom injected code. The function executes in a new thread and therefore does not block `DllMain`. **This is the only file that you should modify**.

For Visual Studio users, the following files are also generated:

- `IL2CppDLL.vxcproj` and `Il2CppDLL.sln`:
  - The project and solution files for a DLL injection project. The first time you load the solution into Visual Studio, you will be asked to re-target the platform SDK and C++ toolchain. Accept the default suggestions. **WARNING: Compilation may fail if you don't do this.**

#### DLL Injection workflow

1. Use Il2CppInspector to create C++ scaffolding output for the executable binary of interest
2. Load the generated solution (`Il2CppDLL.sln`) into Visual Studio
3. Add the code you wish to execute in the `Run()` function in `main.cpp`
4. Compile the project
5. Use a DLL injection tool such as [Cheat Engine](https://www.cheatengine.org/) or [RemoteDLL](https://securityxploded.com/remotedll.php) to inject the compiled DLL into the IL2CPP application at runtime

You have access to all of the C#-equivalent types and methods in the application, plus all of the available IL2CPP API functions.

Example (create a `Vector3` and log its y co-ordinate to a file):

```cpp
// in main.cpp
void Run()
{
    // Vector3 example

    // (Call an IL2CPP API function)
    Vector3__Boxed* myVector3 = (Vector3__Boxed*) il2cpp_object_new(Vector3__TypeInfo);

    // (Call an instance constructor)
    Vector3__ctor(myVector3, 1.0f, 2.0f, 3.0f, NULL);

    // (Access an instance field)
    LogWrite(to_string(myVector3->fields.y));
}
```

### Creating a Visual Studio C# code stubs solution

Il2CppInspector can create a complete Visual Studio workspace with a solution (.sln) file, project (.csproj) files and assembly-namespace-class tree-like folder structure. Each project creates a single assembly.

Use the `--project` flag to generate a solution workspace.

In order for Il2CppInspector to be able to create .csproj files which contain the correct Unity assembly references, you must provide the path to an installed Unity editor and a project template or `ScriptAssemblies` folder of an existing Unity project.

NOTE: The default settings will select the latest installed version of Unity and the latest installed version of the default 3D project template, if they have been installed in the default location.

Typical Unity editor location (specified with `--unity-path`): *C:\Program Files\Unity\Hub\Editor\20xx.y.z*

Typical Unity project template location (specified with `--unity-assemblies`): *C:\Program Files\Unity\Hub\Editor\20xx.y.z\Editor\Data\Resources\PackageManager\ProjectTemplates\libcache\\\<name-of-template>*

Typical Unity script assemblies location in existing project (specified with `--unity-aseemblies`): *X:\MyProject\Library\ScriptAssemblies*

Replace *x*, *y* and *z* with your Unity version number. Replace *\<name-of-template\>* with the desired template.

NOTE: You can use the asterisk wildcard (*) one or more times when specifying these paths. Il2CppInspector will select the last matching folder in alphanumeric order. This is useful if you have multiple side-by-side Unity installs and wish to always select the latest version or template.

In the event that the assembly references are not correctly resolved the first time you load a solution, simply close and re-open the solution to force them to be resolved.

![Il2CppInspector Auto-generated Visual Studio solution](docs/VisualStudio_Preview.png)

### Generating IL2CPP binaries without a Unity project

Two Powershell scripts are provided to enable easy testing and debugging:

* `generate-binaries.ps1` compiles every C# source file in `TestSources` as a separate assembly and outputs them to `TestAssemblies`. It then takes every assembly in `TestAssemblies` and compiles each one as a separate IL2CPP project for each of these architectures: Windows x86 standalone, Windows x64 standalone, Android ARMv7 (32-bit) and Android ARMv8-A (64-bit). These are placed into the `TestBinaries`folder. It then calls `generate-tests.ps1`.
* `generate-tests.ps1` generates a file called `Tests.cs` in the `Il2CppTests` project, containing one test per IL2CPP project in `TestBinaries`. This file will be compiled by the `Il2CppTests`project. You will then be able to see one test per IL2CPP project in Visual Studio's Test Explorer.

The auto-generated tests generate a file in the test IL2CPP binary's folder called `test-result.cs` and compares it (whitespace-insensitive) with the corresponding project name `cs` file in `TestExpectedResults`. In this way, you can check for files with known structure that the analysis is being performed correctly, or step through the analysis of specific binaries in the debugger without having to change the project's command-line arguments.

To learn more about this feature, see the section entitled **Using Il2CppInspector to generate IL2CPP code** in [IL2CPP Reverse Engineering Part 1](https://katyscode.wordpress.com/2020/06/24/il2cpp-part-1/).

### Using the APIs for programmatic analysis

To utilize Il2CppInspector in your own projects, add a reference to `Il2CppInspector.Common.dll`.

Include the following `using` directives:

* `using Il2CppInspector` to use `Il2CppInspector`.
* `using Il2CppInspector.Reflection` to use `TypeModel`.
* `using Il2CppInspector.Model` to use `ApplicationModel`.

See the source code for further details.

### Version support

Unity version | IL2CPP version | Support
--- | --- | ---
4.6.1+ | First release | Unsupported
5.2.x | 15 | Unsupported
5.3.0-5.3.1 | 16 | Working
5.3.2 | 19 | Working
5.3.3-5.3.4 | 20 | Working
5.3.5-5.4.x | 21 | Working
5.5.x | 22 | Working
5.6.x | 23 | Working
2017.1.x-2018.2.x | 24.0 | Working
2018.3.x-2018.4.x | 24.1 | Working
2019.1.x-2019.3.6 | 24.2 | Working
2019.3.7-2020.1.x | 24.3 | Working

Please refer to the separate repository https://github.com/nneonneo/Il2CppVersions if you would like to track the changes between each IL2CPP release version.

### Problems

If you have files that don't work or are in an unsupported format, please open a new issue on GitHub and attach a sample with details on the file format, and I'll try to add support. Include both the IL2CPP binary and `global-metadata.dat` in your submission.

Please check the binary file in a disassembler to ensure that it is a plain IL2CPP binary before filing an issue. Il2CppInspector is not intended to handle packed, encrypted or obfuscated IL2CPP files.

### Support

If you found Il2CppInspector useful, you can really help support the project by making a small donation at http://paypal.me/djkaty!

You can also donate with bitcoin: 3FoRUqUXgYj8NY8sMQfhX6vv9LqR3e2kzz

Much love! - Katy

### Acknowledgements

Thanks to the following major contributors!

- nneonneo - https://github.com/nneonneo (huge overhaul of generics and script generation)
- carterbush - https://github.com/carterbush (IDA script generation)

This project uses [MultiKeyDictionary](https://www.codeproject.com/Articles/32894/C-Multi-key-Generic-Dictionary) by Aron Weiler.

Thanks to the following individuals whose code and research helped me develop this tool:

- Perfare - https://github.com/Perfare/Il2CppDumper
- Jumboperson - https://github.com/Jumboperson/Il2CppDumper
- nevermoe - https://github.com/nevermoe/unity_metadata_loader
- branw - https://github.com/branw/pogo-proto-dumper
- fry - https://github.com/fry/d3
- ARMConverter - http://armconverter.com
- Defuse - https://defuse.ca/online-x86-assembler.htm
- Jackson Dunstan has an awesome series of articles on IL2CPP - https://jacksondunstan.com/articles/tag/il2cpp
- Josh Grunzweig's IDAPython primer series - https://unit42.paloaltonetworks.com/using-idapython-to-make-your-life-easier-part-1/

The following books and documents were also very helpful:

- [Practical Reverse Engineering](https://www.amazon.com/Practical-Reverse-Engineering-Reversing-Obfuscation/dp/1118787315/ref=sr_1_1?keywords=practical+reverse+engineering&qid=1580952619&sr=8-1) by Bruce Dang
- [Expert .NET 2.0 IL Assembler](https://www.amazon.com/Expert-NET-Assembler-Serge-Lidin/dp/1590596463/ref=sr_1_6?keywords=expert+il+2.0&qid=1580952700&sr=8-6) by Serge Lidin
- [The IDA Pro Book, 2nd Edition](https://www.amazon.com/IDA-Pro-Book-Unofficial-Disassembler/dp/1593272898/ref=sr_1_1?keywords=ida+pro+book&qid=1580952729&sr=8-1) by Chris Eagle
- [The Beginner's Guide to IDAPython](https://leanpub.com/IDAPython-Book) by Alexander Hanel
- [ARM Architecture Reference Manual ARMv8-A](https://developer.arm.com/docs/ddi0487/latest)
- [Intel 64 and IA-32 Architectures Software Developer's Manual](https://www.intel.com/content/dam/www/public/us/en/documents/manuals/64-ia-32-architectures-software-developer-instruction-set-reference-manual-325383.pdf)

Pizza spinner animation in the GUI made by Chris Gannon - https://gannon.tv/

### License

This software is licensed under AGPLv3.
