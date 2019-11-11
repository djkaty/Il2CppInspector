// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x000A5098
[assembly: AssemblyCopyright] // 0x000A5098
[assembly: AssemblyDefaultAlias] // 0x000A5098
[assembly: AssemblyDelaySign] // 0x000A5098
[assembly: AssemblyDescription] // 0x000A5098
[assembly: AssemblyFileVersion] // 0x000A5098
[assembly: AssemblyInformationalVersion] // 0x000A5098
[assembly: AssemblyKeyFile] // 0x000A5098
[assembly: AssemblyProduct] // 0x000A5098
[assembly: AssemblyTitle] // 0x000A5098
[assembly: CLSCompliant] // 0x000A5098
[assembly: CompilationRelaxations] // 0x000A5098
[assembly: ComVisible] // 0x000A5098
[assembly: Debuggable] // 0x000A5098
[assembly: DefaultDependency] // 0x000A5098
[assembly: Guid] // 0x000A5098
[assembly: NeutralResourcesLanguage] // 0x000A5098
[assembly: RuntimeCompatibility] // 0x000A5098
[assembly: SatelliteContractVersion] // 0x000A5098
[assembly: StringFreezing] // 0x000A5098
[assembly: TypeLibVersion] // 0x000A5098

// Image 1: Methods.dll - 1810
[assembly: CompilationRelaxations] // 0x000A538C
[assembly: Debuggable] // 0x000A538C
[assembly: RuntimeCompatibility] // 0x000A538C


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
	private Locale(); // 0x003EC600

	// Methods
	public static string GetText(string msg); // 0x003EC608
	public static string GetText(string fmt, params /* 0x000A38C4 */ object[] args); // 0x003EC60C

}


// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1811
{
	// Constructors
	public Test(); // 0x00561220

	// Methods
	public virtual void VirtualMethod(); // 0x00561188
	public double ValueTypeReturnMethod(); // 0x0056118C
	public StringBuilder ReferenceTypeReturnMethod(); // 0x005611B4

}

// Namespace: Il2CppTests.TestSources
public static class TestExtension // TypeDefIndex: 1812
{
	// Methods
	public static double DivideByXExtension(int a, float x); // 0x00561198

}

// Namespace: Il2CppTests.TestSources
internal abstract class TestAbstract // TypeDefIndex: 1813
{
	// Constructors
	protected TestAbstract(); // 0x00561228

	// Methods
	public abstract void AbstractMethod();

}

// Namespace: Il2CppTests.TestSources
internal class TestOverride : Test // TypeDefIndex: 1814
{
	// Constructors
	public TestOverride(); // 0x00561238

	// Methods
	public override void VirtualMethod(); // 0x00561294

}

// Namespace: Il2CppTests.TestSources
internal class TestHideVirtual : Test // TypeDefIndex: 1815
{
	// Constructors
	public TestHideVirtual(); // 0x00561270

	// Methods
	public new void VirtualMethod(); // 0x0056126C

}

// Namespace: Il2CppTests.TestSources
internal class TestHideOverride : TestOverride // TypeDefIndex: 1816
{
	// Constructors
	public TestHideOverride(); // 0x00561258

	// Methods
	public new void VirtualMethod(); // 0x00561254

}

// Namespace: Il2CppTests.TestSources
internal class TestOverrideAbstract : TestAbstract // TypeDefIndex: 1817
{
	// Constructors
	public TestOverrideAbstract(); // 0x0056124C

	// Methods
	public override void AbstractMethod(); // 0x00561298

}

// Namespace: Il2CppTests.TestSources
internal class TestHideAbstractOverride : TestOverrideAbstract // TypeDefIndex: 1818
{
	// Constructors
	public TestHideAbstractOverride(); // 0x00561244

	// Methods
	public new void AbstractMethod(); // 0x00561240

}

// Namespace: Il2CppTests.TestSources
internal class TestHideVirtualAndNewVirtual : Test // TypeDefIndex: 1819
{
	// Constructors
	public TestHideVirtualAndNewVirtual(); // 0x0056127C

	// Methods
	public virtual new void VirtualMethod(); // 0x00561278

}

// Namespace: Il2CppTests.TestSources
internal class TestHideOverrideAndNewVirtual : TestOverride // TypeDefIndex: 1820
{
	// Constructors
	public TestHideOverrideAndNewVirtual(); // 0x00561264

	// Methods
	public virtual new void VirtualMethod(); // 0x00561260

}

// Namespace: Il2CppTests.TestSources
internal abstract class TestAbstractNew : TestOverride // TypeDefIndex: 1821
{
	// Constructors
	protected TestAbstractNew(); // 0x00561230

	// Methods
	public abstract new void VirtualMethod();

}

// Namespace: Il2CppTests.TestSources
internal class TestNewNonVirtualMethod : Test // TypeDefIndex: 1822
{
	// Constructors
	public TestNewNonVirtualMethod(); // 0x0056128C

	// Methods
	public int ValueTypeReturnMethod(); // 0x00561284

}

