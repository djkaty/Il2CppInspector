// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x000A534C
[assembly: AssemblyCopyright] // 0x000A534C
[assembly: AssemblyDefaultAlias] // 0x000A534C
[assembly: AssemblyDelaySign] // 0x000A534C
[assembly: AssemblyDescription] // 0x000A534C
[assembly: AssemblyFileVersion] // 0x000A534C
[assembly: AssemblyInformationalVersion] // 0x000A534C
[assembly: AssemblyKeyFile] // 0x000A534C
[assembly: AssemblyProduct] // 0x000A534C
[assembly: AssemblyTitle] // 0x000A534C
[assembly: CLSCompliant] // 0x000A534C
[assembly: CompilationRelaxations] // 0x000A534C
[assembly: ComVisible] // 0x000A534C
[assembly: Debuggable] // 0x000A534C
[assembly: DefaultDependency] // 0x000A534C
[assembly: Guid] // 0x000A534C
[assembly: NeutralResourcesLanguage] // 0x000A534C
[assembly: RuntimeCompatibility] // 0x000A534C
[assembly: SatelliteContractVersion] // 0x000A534C
[assembly: StringFreezing] // 0x000A534C
[assembly: TypeLibVersion] // 0x000A534C

// Image 1: GenericTypes.dll - 1810
[assembly: CompilationRelaxations] // 0x000A5754
[assembly: Debuggable] // 0x000A5754
[assembly: RuntimeCompatibility] // 0x000A5754


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
	private Locale(); // 0x003ECCE8

	// Methods
	public static string GetText(string msg); // 0x003ECCF0
	public static string GetText(string fmt, params /* 0x000A3B78 */ object[] args); // 0x003ECCF4

}



// Namespace: Il2CppTests.TestSources
public class Base<T, U> // TypeDefIndex: 1815
{
	// Constructors
	public Base();

}

// Namespace: Il2CppTests.TestSources
public class Derived<V> : Base<string, V> // TypeDefIndex: 1816
{
	// Fields
	public G<Derived<V>> F; // 0x00

	// Nested types
	public class Nested // TypeDefIndex: 1817
	{
		// Constructors
		public Nested();

	}

	// Constructors
	public Derived();

}

// Namespace: Il2CppTests.TestSources
public class G<T> // TypeDefIndex: 1818
{
	// Constructors
	public G();

}

// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1819
{
	// Constructors
	public Test(); // 0x00561A3C

	// Methods
	public void GenericTypesTest(); // 0x00561880

}

// Namespace: Il2CppTests.TestSources
internal class ConstrainedValueType<V> // TypeDefIndex: 1820
	where V : struct
{
	// Constructors
	public ConstrainedValueType();

}

// Namespace: Il2CppTests.TestSources
internal class ConstrainedRefType<R> // TypeDefIndex: 1821
	where R : class
{
	// Constructors
	public ConstrainedRefType();

	// Methods
	[NullableContext] // 0x000A5740
	public void ConstrainedMethodNotNull<N>(N notnullArgument, R bar);
	public void ConstrainedUnmanaged<U>(U unmanagedArgument)
		where U : struct;
	public void MultipleConstraintsMethod<C>(C constrained)
		where C : R, new();
	public void MultipleArgumentsMultipleConstraintsMethod<B, I>(B baseArgument, I interfaceArgument)
		where B : Derived<R>, new()
		where I : Test, IDisposable, IEnumerable<R>;
	public void DelegateConstraint<D>(D del)
		where D : Delegate;
	public void EnumConstraint<E>(E enumeration)
		where E : Enum;

}

