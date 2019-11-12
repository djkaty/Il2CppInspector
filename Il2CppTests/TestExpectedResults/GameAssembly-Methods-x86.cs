using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x1000F760
[assembly: AssemblyCopyright] // 0x1000F760
[assembly: AssemblyDefaultAlias] // 0x1000F760
[assembly: AssemblyDelaySign] // 0x1000F760
[assembly: AssemblyDescription] // 0x1000F760
[assembly: AssemblyFileVersion] // 0x1000F760
[assembly: AssemblyInformationalVersion] // 0x1000F760
[assembly: AssemblyKeyFile] // 0x1000F760
[assembly: AssemblyProduct] // 0x1000F760
[assembly: AssemblyTitle] // 0x1000F760
[assembly: CLSCompliant] // 0x1000F760
[assembly: CompilationRelaxations] // 0x1000F760
[assembly: ComVisible] // 0x1000F760
[assembly: Debuggable] // 0x1000F760
[assembly: DefaultDependency] // 0x1000F760
[assembly: Guid] // 0x1000F760
[assembly: NeutralResourcesLanguage] // 0x1000F760
[assembly: RuntimeCompatibility] // 0x1000F760
[assembly: SatelliteContractVersion] // 0x1000F760
[assembly: StringFreezing] // 0x1000F760
[assembly: TypeLibVersion] // 0x1000F760

// Image 1: Methods.dll - 1810
[assembly: CompilationRelaxations] // 0x1000DC90
[assembly: Debuggable] // 0x1000DC90
[assembly: RuntimeCompatibility] // 0x1000DC90

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
	private Locale(); // 0x100BF000

	// Methods
	public static string GetText(string msg); // 0x100F7810
	public static string GetText(string fmt, params /* 0x1000A660 */ object[] args); // 0x10261460
}

// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1811
{
	// Constructors
	public Test(); // 0x100BF000

	// Methods
	public virtual void VirtualMethod(); // 0x100C5530
	public double ValueTypeReturnMethod(); // 0x1034DAD0
	public StringBuilder ReferenceTypeReturnMethod(); // 0x1034DA90
}

// Namespace: Il2CppTests.TestSources
public static class TestExtension // TypeDefIndex: 1812
{
	// Methods
	public static double DivideByXExtension(int a, float x); // 0x1034DA60
}

// Namespace: Il2CppTests.TestSources
internal abstract class TestAbstract // TypeDefIndex: 1813
{
	// Constructors
	protected TestAbstract(); // 0x100BF000

	// Methods
	public abstract void AbstractMethod();
}

// Namespace: Il2CppTests.TestSources
internal class TestOverride : Test // TypeDefIndex: 1814
{
	// Constructors
	public TestOverride(); // 0x100BF000

	// Methods
	public override void VirtualMethod(); // 0x100C5530
}

// Namespace: Il2CppTests.TestSources
internal class TestHideVirtual : Test // TypeDefIndex: 1815
{
	// Constructors
	public TestHideVirtual(); // 0x100BF000

	// Methods
	public new void VirtualMethod(); // 0x100C5530
}

// Namespace: Il2CppTests.TestSources
internal class TestHideOverride : TestOverride // TypeDefIndex: 1816
{
	// Constructors
	public TestHideOverride(); // 0x100BF000

	// Methods
	public new void VirtualMethod(); // 0x100C5530
}

// Namespace: Il2CppTests.TestSources
internal class TestOverrideAbstract : TestAbstract // TypeDefIndex: 1817
{
	// Constructors
	public TestOverrideAbstract(); // 0x100BF000

	// Methods
	public override void AbstractMethod(); // 0x100C5530
}

// Namespace: Il2CppTests.TestSources
internal class TestHideAbstractOverride : TestOverrideAbstract // TypeDefIndex: 1818
{
	// Constructors
	public TestHideAbstractOverride(); // 0x100BF000

	// Methods
	public new void AbstractMethod(); // 0x100C5530
}

// Namespace: Il2CppTests.TestSources
internal class TestHideVirtualAndNewVirtual : Test // TypeDefIndex: 1819
{
	// Constructors
	public TestHideVirtualAndNewVirtual(); // 0x100BF000

	// Methods
	public virtual new void VirtualMethod(); // 0x100C5530
}

// Namespace: Il2CppTests.TestSources
internal class TestHideOverrideAndNewVirtual : TestOverride // TypeDefIndex: 1820
{
	// Constructors
	public TestHideOverrideAndNewVirtual(); // 0x100BF000

	// Methods
	public virtual new void VirtualMethod(); // 0x100C5530
}

// Namespace: Il2CppTests.TestSources
internal abstract class TestAbstractNew : TestOverride // TypeDefIndex: 1821
{
	// Constructors
	protected TestAbstractNew(); // 0x100BF000

	// Methods
	public abstract new void VirtualMethod();
}

// Namespace: Il2CppTests.TestSources
internal class TestNewNonVirtualMethod : Test // TypeDefIndex: 1822
{
	// Constructors
	public TestNewNonVirtualMethod(); // 0x100BF000

	// Methods
	public int ValueTypeReturnMethod(); // 0x100EF660
}

