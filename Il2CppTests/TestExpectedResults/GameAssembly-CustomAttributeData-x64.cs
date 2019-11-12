using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Il2CppTests.TestSources;

// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x000000018000E4A0
[assembly: AssemblyCopyright] // 0x000000018000E4A0
[assembly: AssemblyDefaultAlias] // 0x000000018000E4A0
[assembly: AssemblyDelaySign] // 0x000000018000E4A0
[assembly: AssemblyDescription] // 0x000000018000E4A0
[assembly: AssemblyFileVersion] // 0x000000018000E4A0
[assembly: AssemblyInformationalVersion] // 0x000000018000E4A0
[assembly: AssemblyKeyFile] // 0x000000018000E4A0
[assembly: AssemblyProduct] // 0x000000018000E4A0
[assembly: AssemblyTitle] // 0x000000018000E4A0
[assembly: CLSCompliant] // 0x000000018000E4A0
[assembly: CompilationRelaxations] // 0x000000018000E4A0
[assembly: ComVisible] // 0x000000018000E4A0
[assembly: Debuggable] // 0x000000018000E4A0
[assembly: DefaultDependency] // 0x000000018000E4A0
[assembly: Guid] // 0x000000018000E4A0
[assembly: NeutralResourcesLanguage] // 0x000000018000E4A0
[assembly: RuntimeCompatibility] // 0x000000018000E4A0
[assembly: SatelliteContractVersion] // 0x000000018000E4A0
[assembly: StringFreezing] // 0x000000018000E4A0
[assembly: TypeLibVersion] // 0x000000018000E4A0

// Image 1: CustomAttributeData.dll - 1810
[assembly: CompilationRelaxations] // 0x000000018000C4B0
[assembly: Debuggable] // 0x000000018000C4B0
[assembly: Example] // 0x000000018000C4B0
[assembly: RuntimeCompatibility] // 0x000000018000C4B0

// Namespace: <global namespace>
internal static class Consts // TypeDefIndex: 100
{
	// Fields
	public const string MonoVersion = "2.6.5.0";
	public const string MonoCompany = "MONO development team";
	public const string MonoProduct = "MONO Common language infrastructure";
	public const string MonoCopyright = "(c) various MONO Authors";
	public const string FxVersion = "2.0.0.0";
	public const string VsVersion = "8.0.0.0";
	public const string FxFileVersion = "2.0.50727.1433";
	public const string VsFileVersion = "8.0.50727.1433";
	public const string AssemblyI18N = "I18N, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756";
	public const string AssemblyMicrosoft_VisualStudio = "Microsoft.VisualStudio, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
	public const string AssemblyMicrosoft_VisualStudio_Web = "Microsoft.VisualStudio.Web, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
	public const string AssemblyMicrosoft_VSDesigner = "Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
	public const string AssemblyMono_Http = "Mono.Http, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756";
	public const string AssemblyMono_Posix = "Mono.Posix, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756";
	public const string AssemblyMono_Security = "Mono.Security, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756";
	public const string AssemblyMono_Messaging_RabbitMQ = "Mono.Messaging.RabbitMQ, Version=2.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756";
	public const string AssemblyCorlib = "mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
	public const string AssemblySystem = "System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
	public const string AssemblySystem_Data = "System.Data, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
	public const string AssemblySystem_Design = "System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
	public const string AssemblySystem_DirectoryServices = "System.DirectoryServices, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
	public const string AssemblySystem_Drawing = "System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
	public const string AssemblySystem_Drawing_Design = "System.Drawing.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
	public const string AssemblySystem_Messaging = "System.Messaging, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
	public const string AssemblySystem_Security = "System.Security, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
	public const string AssemblySystem_ServiceProcess = "System.ServiceProcess, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
	public const string AssemblySystem_Web = "System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
	public const string AssemblySystem_Windows_Forms = "System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
	public const string AssemblySystem_Core = "System.Core, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
}

// Namespace: <global namespace>
internal sealed class Locale // TypeDefIndex: 101
{
	// Constructors
	private Locale(); // 0x00000001800E2000

	// Methods
	public static string GetText(string msg); // 0x0000000180123590
	public static string GetText(string fmt, params /* 0x0000000180009150 */ object[] args); // 0x00000001802C64F0
}

// Namespace: Il2CppTests.TestSources
public enum ExampleKind // TypeDefIndex: 1811
{
	FirstKind = 0,
	SecondKind = 1,
	ThirdKind = 2,
	FourthKind = 3
}

// Namespace: Il2CppTests.TestSources
[AttributeUsage] // 0x000000018000C5B0
public class ExampleAttribute : Attribute // TypeDefIndex: 1812
{
	// Fields
	private ExampleKind kindValue; // 0x10
	private string noteValue; // 0x20
	private string[] arrayStrings; // 0x29
	private int[] arrayNumbers; // 0x8047D628

	// Properties
	public ExampleKind Kind { get; } // 0x00000001800ECD10 
	public string[] Strings { get; } // 0x00000001800EAEB0 
	public string Note { get; set; } // 0x00000001803E0EE0 0x00000001800EAEE0
	public int[] Numbers { get; set; } // 0x00000001803E0EF0 0x000000018015B760

	// Constructors
	public ExampleAttribute(ExampleKind initKind, string[] initStrings); // 0x00000001803E0E70
	public ExampleAttribute(ExampleKind initKind); // 0x00000001803E0EB0
	public ExampleAttribute(); // 0x00000001803E0E50
}

// Namespace: Il2CppTests.TestSources
[Example] // 0x000000018000C5D0
public class Test // TypeDefIndex: 1813
{
	// Constructors
	public Test(); // 0x00000001800E2000

	// Methods
	[Example] // 0x000000018000C710
	public void TestMethod([Example] /* 0x000000018000C750 */ object arg); // 0x00000001800EA7B0
}

