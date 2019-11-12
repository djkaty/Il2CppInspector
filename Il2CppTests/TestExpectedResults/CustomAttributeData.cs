/*
 * Generated code file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty
 */

using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Il2CppTests.TestSources;

// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x000A4D7C
[assembly: AssemblyCopyright] // 0x000A4D7C
[assembly: AssemblyDefaultAlias] // 0x000A4D7C
[assembly: AssemblyDelaySign] // 0x000A4D7C
[assembly: AssemblyDescription] // 0x000A4D7C
[assembly: AssemblyFileVersion] // 0x000A4D7C
[assembly: AssemblyInformationalVersion] // 0x000A4D7C
[assembly: AssemblyKeyFile] // 0x000A4D7C
[assembly: AssemblyProduct] // 0x000A4D7C
[assembly: AssemblyTitle] // 0x000A4D7C
[assembly: CLSCompliant] // 0x000A4D7C
[assembly: CompilationRelaxations] // 0x000A4D7C
[assembly: ComVisible] // 0x000A4D7C
[assembly: Debuggable] // 0x000A4D7C
[assembly: DefaultDependency] // 0x000A4D7C
[assembly: Guid] // 0x000A4D7C
[assembly: NeutralResourcesLanguage] // 0x000A4D7C
[assembly: RuntimeCompatibility] // 0x000A4D7C
[assembly: SatelliteContractVersion] // 0x000A4D7C
[assembly: StringFreezing] // 0x000A4D7C
[assembly: TypeLibVersion] // 0x000A4D7C

// Image 1: CustomAttributeData.dll - 1810
[assembly: CompilationRelaxations] // 0x000A526C
[assembly: Debuggable] // 0x000A526C
[assembly: Example] // 0x000A526C
[assembly: RuntimeCompatibility] // 0x000A526C

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

internal sealed class Locale // TypeDefIndex: 101
{
	// Constructors
	private Locale(); // 0x003EC700

	// Methods
	public static string GetText(string msg); // 0x003EC708
	public static string GetText(string fmt, params /* 0x000A35A8 */ object[] args); // 0x003EC70C
}

namespace Il2CppTests.TestSources
{
	public enum ExampleKind // TypeDefIndex: 1811
	{
		FirstKind = 0,
		SecondKind = 1,
		ThirdKind = 2,
		FourthKind = 3
	}

	[AttributeUsage] // 0x000A5070
	public class ExampleAttribute : Attribute // TypeDefIndex: 1812
	{
		// Fields
		private ExampleKind kindValue; // 0x08
		private string noteValue; // 0x0C
		private string[] arrayStrings; // 0x10
		private int[] arrayNumbers; // 0x14
	
		// Properties
		public ExampleKind Kind { get; } // 0x005612FC 
		public string[] Strings { get; } // 0x00561304 
		public string Note { get; set; } // 0x0056130C 0x00561314
		public int[] Numbers { get; set; } // 0x0056131C 0x00561324
	
		// Constructors
		public ExampleAttribute(ExampleKind initKind, string[] initStrings); // 0x00561288
		public ExampleAttribute(ExampleKind initKind); // 0x005612B0
		public ExampleAttribute(); // 0x005612D8
	}

	[Example] // 0x000A5084
	public class Test // TypeDefIndex: 1813
	{
		// Constructors
		public Test(); // 0x00561330
	
		// Methods
		[Example] // 0x000A5208
		public void TestMethod([Example] /* 0x000A525C */ object arg); // 0x0056132C
	}
}
