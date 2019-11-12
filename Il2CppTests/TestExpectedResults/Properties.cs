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
[assembly: AssemblyCompany] // 0x000A4DEC
[assembly: AssemblyCopyright] // 0x000A4DEC
[assembly: AssemblyDefaultAlias] // 0x000A4DEC
[assembly: AssemblyDelaySign] // 0x000A4DEC
[assembly: AssemblyDescription] // 0x000A4DEC
[assembly: AssemblyFileVersion] // 0x000A4DEC
[assembly: AssemblyInformationalVersion] // 0x000A4DEC
[assembly: AssemblyKeyFile] // 0x000A4DEC
[assembly: AssemblyProduct] // 0x000A4DEC
[assembly: AssemblyTitle] // 0x000A4DEC
[assembly: CLSCompliant] // 0x000A4DEC
[assembly: CompilationRelaxations] // 0x000A4DEC
[assembly: ComVisible] // 0x000A4DEC
[assembly: Debuggable] // 0x000A4DEC
[assembly: DefaultDependency] // 0x000A4DEC
[assembly: Guid] // 0x000A4DEC
[assembly: NeutralResourcesLanguage] // 0x000A4DEC
[assembly: RuntimeCompatibility] // 0x000A4DEC
[assembly: SatelliteContractVersion] // 0x000A4DEC
[assembly: StringFreezing] // 0x000A4DEC
[assembly: TypeLibVersion] // 0x000A4DEC

// Image 1: Properties.dll - 1810
[assembly: CompilationRelaxations] // 0x000A5264
[assembly: Debuggable] // 0x000A5264
[assembly: RuntimeCompatibility] // 0x000A5264

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
	private Locale(); // 0x003EC6A8

	// Methods
	public static string GetText(string msg); // 0x003EC6B0
	public static string GetText(string fmt, params /* 0x000A3618 */ object[] args); // 0x003EC6B4
}

namespace Il2CppTests.TestSources
{
	internal class Test // TypeDefIndex: 1811
	{
		// Properties
		private int prop1 { get; set; } // 0x00561230 0x00561238
		protected int prop2 { get; private set; } // 0x00561240 0x00561248
		protected int prop3 { private get; set; } // 0x00561250 0x00561258
		public static int prop4 { private get; set; } // 0x00561260 0x005612C4
		public string this[int i] { get; } // 0x00561328 
		public string this[double d] { get; } // 0x00561384 
		public string this[long l] { set; } // 0x005613DC
		public string this[float f] { get; set; } // 0x005613E0 0x0056143C
		public bool this[int i, int j] { get; } // 0x00561440 
	
		// Constructors
		public Test(); // 0x00561448
	}
}
