// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x1000EDA0
[assembly: AssemblyCopyright] // 0x1000EDA0
[assembly: AssemblyDefaultAlias] // 0x1000EDA0
[assembly: AssemblyDelaySign] // 0x1000EDA0
[assembly: AssemblyDescription] // 0x1000EDA0
[assembly: AssemblyFileVersion] // 0x1000EDA0
[assembly: AssemblyInformationalVersion] // 0x1000EDA0
[assembly: AssemblyKeyFile] // 0x1000EDA0
[assembly: AssemblyProduct] // 0x1000EDA0
[assembly: AssemblyTitle] // 0x1000EDA0
[assembly: CLSCompliant] // 0x1000EDA0
[assembly: CompilationRelaxations] // 0x1000EDA0
[assembly: ComVisible] // 0x1000EDA0
[assembly: Debuggable] // 0x1000EDA0
[assembly: DefaultDependency] // 0x1000EDA0
[assembly: Guid] // 0x1000EDA0
[assembly: NeutralResourcesLanguage] // 0x1000EDA0
[assembly: RuntimeCompatibility] // 0x1000EDA0
[assembly: SatelliteContractVersion] // 0x1000EDA0
[assembly: StringFreezing] // 0x1000EDA0
[assembly: TypeLibVersion] // 0x1000EDA0

// Image 1: ArraysAndPointers.dll - 1810
[assembly: CompilationRelaxations] // 0x1000D250
[assembly: Debuggable] // 0x1000D250
[assembly: RuntimeCompatibility] // 0x1000D250


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
	public static string GetText(string fmt, params /* 0x10009AA0 */ object[] args); // 0x10261460
}



// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1813
{
	// Fields
	private int[] foo; // 0x08
	private int[] bar; // 0x0C
	private float[][] arrayOfArrays; // 0x10
	private float[,] twoDimensionalArray; // 0x14
	private float[,,] threeDimensionalArray; // 0x18
	private unsafe int*[] arrayOfPointer; // 0x1C
	private unsafe int** pointerToPointer; // 0x20
	private unsafe float*[][,,][] confusedElephant; // 0x24

	// Properties
	public unsafe int* PointerProperty { get; set; } // 0x100EB040 0x100EB250
	public unsafe int* this[int i] { get; } // 0x100C5600 
	public unsafe int this[int* p] { get; } // 0x100C5600 
	public unsafe float* this[float* fp] { get; } // 0x100C5600 

	// Nested types
	private struct fixedSizeArrayStruct // TypeDefIndex: 1814
	{
		// Fields
		private unsafe fixed /* 0x1000D370 */ int fixedSizeArray[0]; // 0x08

		// Nested types

	}

	public unsafe delegate void OnUnsafe(int* ud); // TypeDefIndex: 1816; 0x1034DA60

	public class NestedUnsafe<T> // TypeDefIndex: 1817
	{
		// Constructors
		public NestedUnsafe();

		// Methods
		private unsafe T* UnsafeGenericReturn();
		private unsafe void UnsafeGenericMethod(T* pt);
	}

	// Constructors
	public unsafe Test(int* u); // 0x1034DD10

	// Methods
	public int[] FooMethod(int[][] bar); // 0x1034DCE0
	public int[,] BarMethod(int[,,] baz); // 0x1034DC90
	public unsafe void UnsafeMethod(int* unsafePointerArgument); // 0x100C5530
	public unsafe int* UnsafeReturnMethod(); // 0x100C5600
	public unsafe int* UnsafeMethod2(int* i); // 0x10102390
}

