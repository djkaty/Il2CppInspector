// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x1000F7A0
[assembly: AssemblyCopyright] // 0x1000F7A0
[assembly: AssemblyDefaultAlias] // 0x1000F7A0
[assembly: AssemblyDelaySign] // 0x1000F7A0
[assembly: AssemblyDescription] // 0x1000F7A0
[assembly: AssemblyFileVersion] // 0x1000F7A0
[assembly: AssemblyInformationalVersion] // 0x1000F7A0
[assembly: AssemblyKeyFile] // 0x1000F7A0
[assembly: AssemblyProduct] // 0x1000F7A0
[assembly: AssemblyTitle] // 0x1000F7A0
[assembly: CLSCompliant] // 0x1000F7A0
[assembly: CompilationRelaxations] // 0x1000F7A0
[assembly: ComVisible] // 0x1000F7A0
[assembly: Debuggable] // 0x1000F7A0
[assembly: DefaultDependency] // 0x1000F7A0
[assembly: Guid] // 0x1000F7A0
[assembly: NeutralResourcesLanguage] // 0x1000F7A0
[assembly: RuntimeCompatibility] // 0x1000F7A0
[assembly: SatelliteContractVersion] // 0x1000F7A0
[assembly: StringFreezing] // 0x1000F7A0
[assembly: TypeLibVersion] // 0x1000F7A0

// Image 1: References.dll - 1810
[assembly: CompilationRelaxations] // 0x1000DC90
[assembly: Debuggable] // 0x1000DC90
[assembly: RuntimeCompatibility] // 0x1000DC90


// Namespace: <default namespace>
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

// Namespace: <default namespace>
internal sealed class Locale // TypeDefIndex: 101
{
	// Constructors
	private Locale(); // 0x100BF000

	// Methods
	public static string GetText(string msg); // 0x100F7810
	public static string GetText(string fmt, params /* 0x1000A660 */ object[] args); // 0x10261460

}


// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1811
{
	// Fields
	private float floatField; // 0x08

	// Constructors
	public Test(); // 0x100BF000

	// Methods
	public void MethodWithRefParameters(int a, ref int b, int c, ref int d); // 0x100C5530
	public void MethodWithInRefOut(in int a, ref int b, out int c); // 0x1034DA60
	public ref float MethodWithRefReturnType(); // 0x1034DA70

}

// Namespace: Il2CppTests.TestSources
[Obsolete] // 0x1000DD10
public struct RefStruct // TypeDefIndex: 1812
{
	// Fields
	private int structField1; // 0x08

}

