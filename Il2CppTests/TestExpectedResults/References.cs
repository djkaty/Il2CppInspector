using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x000A4F30
[assembly: AssemblyCopyright] // 0x000A4F30
[assembly: AssemblyDefaultAlias] // 0x000A4F30
[assembly: AssemblyDelaySign] // 0x000A4F30
[assembly: AssemblyDescription] // 0x000A4F30
[assembly: AssemblyFileVersion] // 0x000A4F30
[assembly: AssemblyInformationalVersion] // 0x000A4F30
[assembly: AssemblyKeyFile] // 0x000A4F30
[assembly: AssemblyProduct] // 0x000A4F30
[assembly: AssemblyTitle] // 0x000A4F30
[assembly: CLSCompliant] // 0x000A4F30
[assembly: CompilationRelaxations] // 0x000A4F30
[assembly: ComVisible] // 0x000A4F30
[assembly: Debuggable] // 0x000A4F30
[assembly: DefaultDependency] // 0x000A4F30
[assembly: Guid] // 0x000A4F30
[assembly: NeutralResourcesLanguage] // 0x000A4F30
[assembly: RuntimeCompatibility] // 0x000A4F30
[assembly: SatelliteContractVersion] // 0x000A4F30
[assembly: StringFreezing] // 0x000A4F30
[assembly: TypeLibVersion] // 0x000A4F30

// Image 1: References.dll - 1810
[assembly: CompilationRelaxations] // 0x000A525C
[assembly: Debuggable] // 0x000A525C
[assembly: RuntimeCompatibility] // 0x000A525C

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
	private Locale(); // 0x003EC4F8

	// Methods
	public static string GetText(string msg); // 0x003EC500
	public static string GetText(string fmt, params /* 0x000A375C */ object[] args); // 0x003EC504
}

// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1811
{
	// Fields
	private float floatField; // 0x08

	// Constructors
	public Test(); // 0x00561098

	// Methods
	public void MethodWithRefParameters(int a, ref int b, int c, ref int d); // 0x00561080
	public void MethodWithInRefOut(in int a, ref int b, out int c); // 0x00561084
	public ref float MethodWithRefReturnType(); // 0x00561090
}

// Namespace: Il2CppTests.TestSources
[Obsolete] // 0x000A5224
public struct RefStruct // TypeDefIndex: 1812
{
	// Fields
	private int structField1; // 0x08
}

