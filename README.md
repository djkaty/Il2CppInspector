# Il2CppInspector 2020.2

Il2CppInspector helps you to reverse engineer IL2CPP applications, providing the most complete analysis currently available.

### Main features

* Output IL2CPP type definitions, metadata and method pointers as **[C# stub code](#creating-c-prototypes)**

* Create **[C++ scaffolding](#creating-c-scaffolding-or-a-dll-injection-project)** for all types, methods, function pointers and API functions in an IL2CPP application for use in x64dbg, Cydia Substrate etc.

* Create **[IDA](#adding-metadata-to-your-ida-workflow) and [Ghidra](#adding-metadata-to-your-ghidra-workflow) Python scripts** to populate symbol, function and type information; includes API hooks to [implement scripts for other targets](#extending-il2cppinspectors-python-output-to-support-other-targets)

* Create Visual Studio **[C++ DLL injection projects](#dll-injection-workflow)** directly from IL2CPP files

* Create Visual Studio **[C# code stub solutions](#creating-a-visual-studio-c-code-stubs-solution)** directly from IL2CPP files

* Create **[JSON metadata](#generating-json-metadata)** with a complete address map directly from IL2CPP Files.

* Create **[IL2CPP binaries from arbitrary C# source code without a Unity project](#generating-il2cpp-binaries-without-a-unity-project)**

* **[Three major APIs](#using-the-apis-for-programmatic-analysis)** for use in your own custom static analysis projects for querying low level binary metadata, the .NET type model and the whole C++ application. These are also available as a [NuGet Package](https://www.nuget.org/packages/NoisyCowStudios.Il2CppInspector/).

* Supports **all major file formats and processor architectures**

* Defeats certain types of basic obfuscation

* Works on Windows, MacOS X and Linux. **Integrated GUI** for Windows users with drag & drop support

* Tested with [every release of IL2CPP](#version-support) since Unity 5.3.0


### Tutorials and Guides

You can read more about IL2CPP in my series [IL2CPP Reverse Engineering](https://katyscode.wordpress.com/category/reverse-engineering/il2cpp/).

[Using Il2CppInspector's type model to extract protobuf-net definitions (Case Study: Fall Guys)](https://katyscode.wordpress.com/2020/08/10/practical-il2cpp-protobuf/)


### Other features

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
* API function export processing for PE, ELF, Mach-O and SELF (PRX) binaries
* Symbol table processing and name demangling for ELF and Mach-O binaries 
* Relocation processing for ELF binaries
* Automatically defeats certain basic obfuscation methods
* Test chassis for automated integration testing of IL2CPP binaries

Class library targets .NET Standard 2.1. Application targets .NET Core 3.0. Built with Visual Studio 2019.

**NOTE**: Il2CppInspector is not a decompiler. It can provide you with the structure of an application and function addresses for every method so that you can easily jump straight to methods of interest in your disassembler. It does not attempt to recover the entire source code of the application.

![Il2CppInspector GUI](docs/GUI_Preview.png)

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

  -p, --py-out                (Default: il2cpp.py) Python script output file

  -h, --cpp-out               (Default: cpp) C++ scaffolding / DLL injection project output path

  -o, --json-out              (Default: metadata.json) JSON metadata output file

  -e, --exclude-namespaces    (Default: System Mono Microsoft.Reflection Microsoft.Win32 Internal.Runtime Unity UnityEditor UnityEngine UnityEngineInternal AOT JetBrains.Annotations) Comma-separated list of namespaces to suppress in C# output, or 'none' to include all namespaces

  -l, --layout                (Default: single) Partitioning of C# output ('single' = single file, 'namespace' = one file per namespace in folders, 'assembly' = one file per assembly, 'class' = one file per class in namespace folders, 'tree' = one file per class in assembly and
                              namespace folders)

  -s, --sort                  (Default: index) Sort order of type definitions in C# output ('index' = by type definition index, 'name' = by type name). No effect when using file-per-class or tree layout

  -f, --flatten               Flatten the namespace hierarchy into a single folder rather than using per-namespace subfolders. Only used when layout is per-namespace or per-class. Ignored for tree layout

  -n, --suppress-metadata     Diff tidying: suppress method pointers, field offsets and type indices from C# output. Useful for comparing two versions of a binary for changes with a diff tool

  -k, --must-compile          Compilation tidying: try really hard to make code that compiles. Suppress generation of code for items with CompilerGenerated attribute. Comment out attributes without parameterless constructors or all-optional constructor arguments. Don't emit
                              add/remove/raise on events. Specify AttributeTargets.All on classes with AttributeUsage attribute. Force auto-properties to have get accessors. Force regular properties to have bodies. Suppress global::Locale classes. Generate dummy parameterless
                              base constructors and ref return fields.

  --separate-attributes       Place assembly-level attributes in their own AssemblyInfo.cs files. Only used when layout is per-assembly or tree

  -j, --project               Create a Visual Studio solution and projects. Implies --layout tree, --must-compile and --separate-attributes

  --cpp-compiler              (Default: BinaryFormat) Compiler to target for C++ output (MSVC or GCC); selects based on binary executable type by default

  -t, --script-target         (Default: IDA) Application to target for Python script output (IDA or Ghidra) - case-sensitive

  --unity-path                (Default: C:\Program Files\Unity\Hub\Editor\*) Path to Unity editor (when using --project). Wildcards select last matching folder in alphanumeric order

  --unity-assemblies          (Default: C:\Program Files\Unity\Hub\Editor\*\Editor\Data\Resources\PackageManager\ProjectTemplates\libcache\com.unity.template.3d-*\ScriptAssemblies) Path to Unity script assemblies (when using --project). Wildcards select last matching folder in
                              alphanumeric order

  --unity-version             Version of Unity used to create the input files, if known. Used to enhance Python, C++ and JSON output. If not specified, a close match will be inferred automatically.

  --help                      Display this help screen.

  --version                   Display version information.
```

For Apple Universal Binaries and APKs with binaries for multiple architectures, multiple output files will be generated, with each filename besides the first suffixed by the index of the image in the binary. Unsupported images will be skipped.

For IPA packages, the executable must be decrypted first. Encrypted executable binaries are not supported.

### Creating C# prototypes

Il2CppInspector can create C# code stub files with pointer metadata to allow you to explore the IL2CPP binary in an easy-to-read format.

Specify the `-c` switch to choose the C# output file or path.

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

You can specify the output layout with the `--layout` switch:

* `single` - all types in a single file
* `namespace` - one namespace per file (each file will be given the namespace name)
* `assembly` - one assembly per file (each file will be given the assembly name)
* `class` - one class per file
* `tree` - one class per file in a tree-like folder structure with top-level assembly folders and second-level namespace folders

You can specify the intra-file type ordering with the `--sort` switch:

* `index` - sort by type definition index as found in the IL2CPP metadata
* `name` - sort alphabetically by type name

This switch has no effect when using `class` or `tree` layout.

The `--flatten` switch allows you to flatten a hierarchy of nested namespaces when `layout` is `class` or `namespace`, ie. a series of sub-folders `System/IO/Compression` will instead be a single top-level sub-folder `System.IO.Compression`.

The `--suppress-metadata` switch allows you to suppress the output of comments including method pointers, field offsets and type indices. This is useful when you want to diff the changes between two versions of an application to see only which types have changed.

The `--seperate-attributes` switch directs Il2CppInspector to put assembly-level attributes in their own `AssemblyInfo.cs` files rather than at the top of the first file generated for each assembly, when `layout` is `assembly` or `tree`.

### Adding metadata to your IDA workflow

Simply run Il2CppInspector with the `-p` switch to choose the IDA script output file. Load your binary file into IDA, press Alt+F7 and select the generated script. Observe the Output Window while IDA analyzes the file - this may take a long time.

Three files are generated by Il2CppInspector for IDAPython: the Python script itself, a JSON metadata file and a C++ type header file (this is `cpp/appdata/il2cpp-types.h` by default if you used the CLI, or `il2cpp.h` if you used the GUI). These files must be present for the script to run successfully.

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
- Names and types for some IL2CPP-specific data structures and functions
- Names and typed signatures for all IL2CPP API functions that could be identified

Example IDA C++ decompilation after applying Il2CppInspector (initialization code omitted for brevity):

![Il2CppInspector annotated IDA project](docs/IDA_Preview.png)

### Adding metadata to your Ghidra workflow

Run Il2CppInspector with the `-p` switch to choose the Ghidra script output file, and `-t Ghidra` (case-sensitive) to specify Ghidra as the script type you wish to produce.

Three files are generated by Il2CppInspector for Ghidra: the Python script itself, a JSON metadata file and a C++ type header file. These files must be present for the script to run successfully.

If you know which version of Unity the binary was compiled with, you can improve the output by specifying this with `--unity-version`, for example `--unity-version 2019.3.1f1`. Otherwise Il2CppInspector will make an educated guess based on the contents of the binary.

To import metadata into an existing Ghidra project:

1. From the _Code Browser_, choose _File -> Parse C Source..._
2. Create a new profile and add the generated C++ type header file. This is `cpp/appdata/il2cpp-types.h` by default if you used the CLI, or `il2cpp.h` if you used the GUI.
3. Ensure the _Parse Options_ are set exactly as follows:

   `-D_GHIDRA_`

4. Click Parse to Program and accept any warnings. This may take a long time to complete.
5. Open the _Script Manager_ and add the output folder you chose in Il2CppInspector as a script folder.
6. If you have used scripts from Il2CppInspector for other binaries, ensure the Python files are named differently, or disable use of the previous script folder(s).
7. Click Refresh to make the script appear in _Script Manager_.
8. Right-click the script and choose _Run_. This may take a while to complete.

   **NOTE:** If you receive a `Conflicting data exists at address` error when running the script, re-load the binary into the project but choose No when Ghidra asks if you would like to perform auto-analysis.

![Ghidra import help](docs/Ghidra_Guide.png)

Il2CppInspector generates identical data for Ghidra projects as it does for IDA - see the above section for details.

**NOTE**: For compatibility reasons, executing the Ghidra Python script on an ELF file will reset the file's image base to zero (`0x00000000`). Other file formats will retain the same image base.

Example Ghidra C++ decompilation after applying Il2CppInspector:

![Il2CppInspector annotated Ghidra project](docs/Ghidra_Preview.png)

### Creating C++ scaffolding or a DLL injection project

Il2CppInspector generates a series of C++ source files which you can use in a variety of ways, for example:

- with a tool like x64dbg to analyze the memory of the application at runtime
- with a tool like Cydia Substrate (MSHookFunction) for accessing types, methods and IL2CPP API functions
- to run custom code via DLL injection on Windows (see below)

Run Il2CppInspector with the `-h` switch to choose the C++ output folder.

If you know which version of Unity the binary was compiled with, you can improve the output by specifying this with `--unity-version`, for example `--unity-version 2019.3.1f1`. Otherwise Il2CppInspector will make an educated guess based on the contents of the binary.

You can target which C++ compiler you wish to use the output files with: specify `--cpp-compiler MSVC` for Visual Studio and `--cpp-compiler GCC` for gcc or clang.

Il2CppInspector performs automatic name conflict resolution to avoid the use of pre-defined symbols and keywords in C++, and to handle re-definition of same-named symbols in the application.

Some IL2CPP binary files contain only a partial set of API exports, or none at all. For these cases, Il2CppInspector will build scaffolding using only the available exports to ensure that the project compiles successfully.

![Il2CppInspector GUI](docs/Cpp_Preview.png)

The following files are generated:

- `appdata` - this folder contains project-agnostic binary-specific declarations:
    - `ilc2pp-types.h`:
      - Type declarations for all internal IL2CPP types (a minimal version of the Unity headers)
      - Type declarations for every type used in the application including all arrays, enums, concrete generic type instances and inferred usages from metadata.
      - Boxed versions for types where applicable
      - VTables for every type

    - `il2cpp-functions.h`:
      - The function pointer signature and offset from the image base address to every C#-equivalent method

    - `il2cpp-types-ptr.h`:
      - The offset from the image base address to every type information class (`Il2CppClass **`)

    - `il2cpp-api-functions.h`:
      - The function pointer signature to every IL2CPP API function (copied directly from Unity for the version used to compile the binary). Functions not found in the binary's export list will be elided

    - `il2cpp-api-functions-ptr.h`:
      - The offset from the image base address to every IL2CPP API function export (functions starting with `il2cpp_`)

The above files contain all the data needed for dynamic analysis in a debugger.

In addition, the following files are generated for DLL injection:

- `framework` - project-agnostic binary-agnostic boilerplate code and scaffolding:
    - `dllmain.cpp`:
      - Provides a DLL injection stub which calls `init_il2cpp()` (see below) and starts `Run()` (see below) in a new thread

    - `helpers.cpp` and `helpers.h`:
      - Provides basic logging and other helper functions. See the comments in `helpers.h` for details.

    - `il2cpp-init.cpp`, `il2cpp-init.h` and `il2cpp-appdata.h`:
      - Provides the `void init_il2cpp()` function which uses all of the above headers to generate usable function pointers and class pointers that are mapped to the correct places in the in-memory image at runtime

    - `pch-il2cpp.cpp` and `pch-il2cpp.h`:
      - Provides the necessary scaffolding to enable pre-compiled headers (PCH) for all of the headers in the `appdata` folder

- `user` - project-specific binary-agnostic user code which you can modify as desired:
    - `main.cpp` and `main.h`:
      - Contains a stub `Run()` function which denotes the entry point for your custom injected code. The function executes in a new thread and therefore does not block `DllMain`.

  **This is the only folder whose files you should edit**.

For Visual Studio users, the following files are also generated:

- `IL2CppDLL.vcxproj`, `Il2CppDLL.vcxproj.filters` and `Il2CppDLL.sln`:
  - The project and solution files for a DLL injection project. The first time you load the solution into Visual Studio, you will be asked to re-target the platform SDK and C++ toolchain. Accept the default suggestions. **WARNING: Compilation may fail if you don't do this.**

The default `main.cpp` includes commented code to allow you to write to a log or open a new console using the functions from `helpers.h`. To specify a log file target in your source code, use `extern const LPCWSTR LOG_FILE = L"my_log_file.txt"`.

#### DLL Injection workflow

1. Use Il2CppInspector to create C++ scaffolding output for the executable binary of interest
2. Load the generated solution (`Il2CppDLL.sln`) into Visual Studio
3. Add the code you wish to execute in the `Run()` function in `main.cpp`
4. Compile the project
5. Use a DLL injection tool such as [Cheat Engine](https://www.cheatengine.org/) or [RemoteDLL](https://securityxploded.com/remotedll.php) to inject the compiled DLL into the IL2CPP application at runtime

You have access to all of the C#-equivalent types and methods in the application, plus all of the available IL2CPP API functions. No additional function pointer or type declarations are required.

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

Use the `--project` switch to generate a solution workspace.

In order for Il2CppInspector to be able to create .csproj files which contain the correct Unity assembly references, you must provide the path to an installed Unity editor and a project template or `ScriptAssemblies` folder of an existing Unity project.

NOTE: The default settings will select the latest installed version of Unity and the latest installed version of the default 3D project template, if they have been installed in the default location.

Typical Unity editor location (specified with `--unity-path`): *C:\Program Files\Unity\Hub\Editor\20xx.y.z*

Typical Unity project template location (specified with `--unity-assemblies`): *C:\Program Files\Unity\Hub\Editor\20xx.y.z\Editor\Data\Resources\PackageManager\ProjectTemplates\libcache\\\<name-of-template>*

Typical Unity script assemblies location in existing project (specified with `--unity-aseemblies`): *X:\MyProject\Library\ScriptAssemblies*

Replace *x*, *y* and *z* with your Unity version number. Replace *\<name-of-template\>* with the desired template.

NOTE: You can use the asterisk wildcard (*) one or more times when specifying these paths. Il2CppInspector will select the last matching folder in alphanumeric order. This is useful if you have multiple side-by-side Unity installs and wish to always select the latest version or template.

In the event that the assembly references are not correctly resolved the first time you load a solution, simply close and re-open the solution to force them to be resolved.

![Il2CppInspector Auto-generated Visual Studio solution](docs/VisualStudio_Preview.png)

### Generating JSON metadata

Il2CppInspector can create a JSON file containing various metadata about the application.

Use the `-o` switch to specify the JSON output path.

The output schema is as follows:

- `addressMap` (object)

  An address map of all IL2CPP-related content in binary
    - `methodDefinitions` (array)
      The virtual address, binary symbol, C++ function signature and .NET method signature of every .NET-equivalent method in the binary

    - `constructedGenericMethods` (array)
      The virtual address, binary symbol, C++ function signature and .NET method signature of every .NET-equivalent concrete generic method in the binary

    - `customAttributesGenerators` (array)
      The virtual address, name and C++ function signature of every custom attributes generator function in the binary

    - `methodInvokers` (array)
      The virtual address, name and C++ function signature of every Method.Invoke thunk function in the binary

    - `stringLiterals` (array)
      For Unity versions lower than 5.3.2: the ordinal, name and text of every string literal in the binary
      For Unity version 5.3.2 and later: the virtual address, name and text of every string literal in the binary

    - `typeInfoPointers` (array)
      The virtual address, name, C++ derived type name and .NET type name equivalent of every class definition pointer (`Il2CppClass *`) in the binary

    - `typeRefPointers` (array)
      The virtual address, name and .NET type name equivalent of every type reference pointer (`Il2CppType *`) in the binary

    - `methodInfoPointers` (array)
      The virtual address, name and .NET method signature of every runtime method definition pointer (`MethodInfo *`) in the binary

    - `functionAddresses` (array)
      The virtual addresses of the start of every known function in the binary, including all of those above plus any others detected but not included in the above categories

    - `typeMetadata` (array)
      The virtual address, name and C++ type name of key metadata items in the binary. This includes `Il2CppCodeRegistration` and `Il2CppMetadataRegistration`, and depending on the binary version may also include `Il2CppCodeGenModule **` and a pointer to each `Il2CppCodeGenModule`

    - `functionMetadata` (array)
      The virtual address, name and C++ function signature of key metadata functions in the binary. Depending on how the binary was analyzed by Il2CppInspector, this may include `il2cpp_codegen_register`.

    - `apis` (array)
      The virtual address, name and C++ function signature of every IL2CPP API function identified in the binary

    - `exports` (array)
      The virtual address and name of every export in the binary

    - `symbols` (array)
      The virtual address, name and symbol type of every named (non-zero name length) and non-zero address function definition, type, field name and import (for ELF) defined in the binary. Not currently supported for PE files.

### Generating IL2CPP binaries without a Unity project

Two Powershell scripts are provided to enable easy testing and debugging:

* `generate-binaries.ps1` compiles every C# source file in `TestSources` as a separate assembly and outputs them to `TestAssemblies`. It then takes every assembly in `TestAssemblies` and compiles each one as a separate IL2CPP project for each of these architectures: Windows x86 standalone, Windows x64 standalone, Android ARMv7 (32-bit) and Android ARMv8-A (64-bit). These are placed into the `TestBinaries`folder. It then calls `generate-tests.ps1`.
* `generate-tests.ps1` generates a file called `Tests.cs` in the `Il2CppTests` project, containing one test per IL2CPP project in `TestBinaries`. This file will be compiled by the `Il2CppTests`project. You will then be able to see one test per IL2CPP project in Visual Studio's Test Explorer.

The auto-generated tests generate a file in the test IL2CPP binary's folder called `test-result.cs` and compares it (whitespace-insensitive) with the corresponding project name `cs` file in `TestExpectedResults`. In this way, you can check for files with known structure that the analysis is being performed correctly, or step through the analysis of specific binaries in the debugger without having to change the project's command-line arguments.

To learn more about this feature, see the section entitled **Using Il2CppInspector to generate IL2CPP code** in [IL2CPP Reverse Engineering Part 1](https://katyscode.wordpress.com/2020/06/24/il2cpp-part-1/).

### Using the APIs for programmatic analysis

**NOTE: _The Il2CppInspector APIs are also available as a [NuGet package](https://www.nuget.org/packages/NoisyCowStudios.Il2CppInspector/)! (search: NoisyCowStudios.Il2CppInspector)_**

Il2CppInspector offers the following re-usable class library APIs:

* **Il2CppInspector** - low-level access to the binary image and metadata
* **TypeModel** - high-level .NET Reflection-like query API for all of the .NET types in the source project as a tree model
* **AppModel** - access to all of the C++ types and methods, plus the IL2CPP API exports, with detailed address and offset data and mappings to their .NET equivalents

Use these APIs to easily query IL2CPP types, create new output modules and integrate Il2CppInspector with your own static analysis applications.

To utilize Il2CppInspector in your own projects, add a reference to `Il2CppInspector.Common.dll`.

Include the following `using` directives:

* `using Il2CppInspector` to use `Il2CppInspector`.
* `using Il2CppInspector.Reflection` to use `TypeModel`.
* `using Il2CppInspector.Model` to use `AppModel`.

See the source code for further details or the tutorials above.

### Extending Il2CppInspector's Python output to support other targets

The current version of Il2CppInspector can output Python scripts targeting the IDA and Ghidra disassemblers.

When Il2CppInspector generates such a script, it generates a concetenation of a shared block of code (`Outputs/ScriptResources/shared-main.py`) which parses the JSON metadata and dispatches it to a set of implementation-specific functions to be processed, and a block of code specific to the target application which implements these functions (a file from `Outputs/ScriptResources/Targets`).

If you would like to add support for a new target application, create a new Python script in `Outputs/ScriptResources/Targets` with the nane `<target-name-without-whitespace>.py` and implement the following functions:

- `CustomInitializer()` - perform any custom initialization required for the target before applying the metadata
- `DefineCode(code)` - parse and apply the specified C++ declaration text (this is not required for Unity 5.3.2 and later; if you don't need to support earlier versions, just specify `pass` as the implementation)
- `GetScriptDirectory()` - retrieve the directory that the Python script is running in. This will normally be `os.path.dirname(os.path.realpath(__file__))`
- `MakeFunction(start, name=None)` - define address `start` as the start of a function, optionally with name `name`
- `SetComment(addr, text)` - place the comment `text` at address `addr`
- `SetFunctionType(addr, sig)` - parse the C++ function signature in `sig` and apply it to the function at address `addr`
- `SetHeaderComment(addr, text)` - place the header/plate/pre-item comment `text` at address `addr`
- `SetName(addr, name)` - set the symbol (or label or name) of address `addr` to `name`
- `SetType(addr, type)` - define address `addr` to be an item of the C++ type `type`

Refer to the source code of `IDA.py` and `Ghidra.py` for examples.

When you add a new target and re-compile Il2CppInspector:

- the target will automatically be available for selection via the `-t` (`--script-target`) switch via the CLI
- the target will automatically appear in the drop-down selection box for script targets in the GUI

If you develop an API for a target that you think might be useful to others, please feel free to submit a PR with the code!

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
2020.2.0 (alpha) | 27 | Working

Please refer to the companion repository https://github.com/nneonneo/Il2CppVersions if you would like to track the changes between each IL2CPP release version.

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

This project uses:

- [MultiKeyDictionary](https://www.codeproject.com/Articles/32894/C-Multi-key-Generic-Dictionary) by Aron Weiler
- [CxxDemangler](https://github.com/southpolenator/CxxDemangler) by Vuk Jovanovic
- [CommandLineParser](https://github.com/commandlineparser/commandline) 
- [Ookii Dialogs](http://www.ookii.org/software/dialogs/) by Sven Groot
- [XamlAnimatedGif](https://github.com/XamlAnimatedGif/WpfAnimatedGif) by Thomas Levesque

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
- Bloog Bot by Drew Kestell - https://drewkestell.us/Article/6/Chapter/1

The following books and documents were also very helpful:

- [Practical Reverse Engineering](https://www.amazon.com/Practical-Reverse-Engineering-Reversing-Obfuscation/dp/1118787315/ref=sr_1_1?keywords=practical+reverse+engineering&qid=1580952619&sr=8-1) by Bruce Dang
- [Expert .NET 2.0 IL Assembler](https://www.amazon.com/Expert-NET-Assembler-Serge-Lidin/dp/1590596463/ref=sr_1_6?keywords=expert+il+2.0&qid=1580952700&sr=8-6) by Serge Lidin
- [The IDA Pro Book, 2nd Edition](https://www.amazon.com/IDA-Pro-Book-Unofficial-Disassembler/dp/1593272898/ref=sr_1_1?keywords=ida+pro+book&qid=1580952729&sr=8-1) by Chris Eagle
- [The Beginner's Guide to IDAPython](https://leanpub.com/IDAPython-Book) by Alexander Hanel
- [ARM Architecture Reference Manual ARMv8-A](https://developer.arm.com/docs/ddi0487/latest)
- [Intel 64 and IA-32 Architectures Software Developer's Manual](https://www.intel.com/content/dam/www/public/us/en/documents/manuals/64-ia-32-architectures-software-developer-instruction-set-reference-manual-325383.pdf)
- [Ghidra API documentation](https://ghidra.re/ghidra_docs/api/)

Pizza spinner animation in the GUI made by Chris Gannon - https://gannon.tv/

### License

This software is licensed under AGPLv3.
