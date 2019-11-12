/*
 * Generated code file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty
 */

using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x000000018000DF60
[assembly: AssemblyCopyright] // 0x000000018000DF60
[assembly: AssemblyDefaultAlias] // 0x000000018000DF60
[assembly: AssemblyDelaySign] // 0x000000018000DF60
[assembly: AssemblyDescription] // 0x000000018000DF60
[assembly: AssemblyFileVersion] // 0x000000018000DF60
[assembly: AssemblyInformationalVersion] // 0x000000018000DF60
[assembly: AssemblyKeyFile] // 0x000000018000DF60
[assembly: AssemblyProduct] // 0x000000018000DF60
[assembly: AssemblyTitle] // 0x000000018000DF60
[assembly: CLSCompliant] // 0x000000018000DF60
[assembly: CompilationRelaxations] // 0x000000018000DF60
[assembly: ComVisible] // 0x000000018000DF60
[assembly: Debuggable] // 0x000000018000DF60
[assembly: DefaultDependency] // 0x000000018000DF60
[assembly: Guid] // 0x000000018000DF60
[assembly: NeutralResourcesLanguage] // 0x000000018000DF60
[assembly: RuntimeCompatibility] // 0x000000018000DF60
[assembly: SatelliteContractVersion] // 0x000000018000DF60
[assembly: StringFreezing] // 0x000000018000DF60
[assembly: TypeLibVersion] // 0x000000018000DF60

// Image 1: Properties.dll - 1810
[assembly: CompilationRelaxations] // 0x000000018000C0D0
[assembly: Debuggable] // 0x000000018000C0D0
[assembly: RuntimeCompatibility] // 0x000000018000C0D0

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
	private Locale(); // 0x00000001800E2000

	// Methods
	public static string GetText(string msg); // 0x0000000180123590
	public static string GetText(string fmt, params /* 0x00000001800091F0 */ object[] args); // 0x00000001802C64F0
}

namespace Il2CppTests.TestSources
{
	internal class Test // TypeDefIndex: 1811
	{
		// Properties
		private int prop1 { get; set; } // 0x00000001800ECD10 0x0000000180143AD0
		protected int prop2 { get; private set; } // 0x0000000180156360 0x00000001803E0F20
		protected int prop3 { private get; set; } // 0x00000001800ED060 0x000000018019DD90
		public static int prop4 { private get; set; } // 0x00000001803E0EE0 0x00000001803E0F30
		public string this[int i] { get; } // 0x00000001803E0E80 
		public string this[double d] { get; } // 0x00000001803E0E50 
		public string this[long l] { set; } // 0x00000001800EA7B0
		public string this[float f] { get; set; } // 0x00000001803E0EB0 0x00000001800EA7B0
		public bool this[int i, int j] { get; } // 0x000000018010E420 
	
		// Constructors
		public Test(); // 0x00000001800E2000
	}
}
