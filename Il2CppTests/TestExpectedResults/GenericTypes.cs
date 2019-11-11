// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x000A5390
[assembly: AssemblyCopyright] // 0x000A5390
[assembly: AssemblyDefaultAlias] // 0x000A5390
[assembly: AssemblyDelaySign] // 0x000A5390
[assembly: AssemblyDescription] // 0x000A5390
[assembly: AssemblyFileVersion] // 0x000A5390
[assembly: AssemblyInformationalVersion] // 0x000A5390
[assembly: AssemblyKeyFile] // 0x000A5390
[assembly: AssemblyProduct] // 0x000A5390
[assembly: AssemblyTitle] // 0x000A5390
[assembly: CLSCompliant] // 0x000A5390
[assembly: CompilationRelaxations] // 0x000A5390
[assembly: ComVisible] // 0x000A5390
[assembly: Debuggable] // 0x000A5390
[assembly: DefaultDependency] // 0x000A5390
[assembly: Guid] // 0x000A5390
[assembly: NeutralResourcesLanguage] // 0x000A5390
[assembly: RuntimeCompatibility] // 0x000A5390
[assembly: SatelliteContractVersion] // 0x000A5390
[assembly: StringFreezing] // 0x000A5390
[assembly: TypeLibVersion] // 0x000A5390

// Image 1: GenericTypes.dll - 1810
[assembly: CompilationRelaxations] // 0x000A5684
[assembly: Debuggable] // 0x000A5684
[assembly: RuntimeCompatibility] // 0x000A5684


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
	private Locale(); // 0x003EC9C0

	// Methods
	public static string GetText(string msg); // 0x003EC9C8
	public static string GetText(string fmt, params /* 0x000A3BBC */ object[] args); // 0x003EC9CC
}


// Namespace: Il2CppTests.TestSources
public class Base<T, U> // TypeDefIndex: 1811
{
	// Constructors
	public Base();

}

// Namespace: Il2CppTests.TestSources
public class Derived<V> : Base<string, V> // TypeDefIndex: 1812
{
	// Fields
	public G<Derived<V>> F; // 0x00

	// Nested types
	public class Nested // TypeDefIndex: 1813
	{
		// Constructors
		public Nested();

	}

	// Constructors
	public Derived();

}

// Namespace: Il2CppTests.TestSources
public class G<T> // TypeDefIndex: 1814
{
	// Constructors
	public G();

}

// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1815
{
	// Constructors
	public Test(); // 0x00561704

	// Methods
	public void GenericTypesTest(); // 0x00561548
}

