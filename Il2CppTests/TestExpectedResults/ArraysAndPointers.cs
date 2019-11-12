using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x000A68AC
[assembly: AssemblyCopyright] // 0x000A68AC
[assembly: AssemblyDefaultAlias] // 0x000A68AC
[assembly: AssemblyDelaySign] // 0x000A68AC
[assembly: AssemblyDescription] // 0x000A68AC
[assembly: AssemblyFileVersion] // 0x000A68AC
[assembly: AssemblyInformationalVersion] // 0x000A68AC
[assembly: AssemblyKeyFile] // 0x000A68AC
[assembly: AssemblyProduct] // 0x000A68AC
[assembly: AssemblyTitle] // 0x000A68AC
[assembly: CLSCompliant] // 0x000A68AC
[assembly: CompilationRelaxations] // 0x000A68AC
[assembly: ComVisible] // 0x000A68AC
[assembly: Debuggable] // 0x000A68AC
[assembly: DefaultDependency] // 0x000A68AC
[assembly: Guid] // 0x000A68AC
[assembly: NeutralResourcesLanguage] // 0x000A68AC
[assembly: RuntimeCompatibility] // 0x000A68AC
[assembly: SatelliteContractVersion] // 0x000A68AC
[assembly: StringFreezing] // 0x000A68AC
[assembly: TypeLibVersion] // 0x000A68AC

// Image 1: ArraysAndPointers.dll - 1810
[assembly: CompilationRelaxations] // 0x000A6D34
[assembly: Debuggable] // 0x000A6D34
[assembly: RuntimeCompatibility] // 0x000A6D34

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
	private Locale(); // 0x003EE218

	// Methods
	public static string GetText(string msg); // 0x003EE220
	public static string GetText(string fmt, params /* 0x000A50D8 */ object[] args); // 0x003EE224
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
	public unsafe int* PointerProperty { get; set; } // 0x00562EEC 0x00562EF4
	public unsafe int* this[int i] { get; } // 0x00562F10 
	public unsafe int this[int* p] { get; } // 0x00562F18 
	public unsafe float* this[float* fp] { get; } // 0x00562F20 

	// Nested types
	private struct fixedSizeArrayStruct // TypeDefIndex: 1814
	{
		// Fields
		private unsafe fixed /* 0x000A6C98 */ int fixedSizeArray[0]; // 0x08
	}

	public unsafe delegate void OnUnsafe(int* ud); // TypeDefIndex: 1816; 0x00562F3C

	public class NestedUnsafe<T> // TypeDefIndex: 1817
		where T : struct
	{
		// Constructors
		public NestedUnsafe();

		// Methods
		private unsafe T* UnsafeGenericReturn();
		private unsafe void UnsafeGenericMethod(T* pt);
	}

	// Constructors
	public unsafe Test(int* u); // 0x00562E78

	// Methods
	public int[] FooMethod(int[][] bar); // 0x00562DA0
	public int[,] BarMethod(int[,,] baz); // 0x00562E00
	public unsafe void UnsafeMethod(int* unsafePointerArgument); // 0x00562EFC
	public unsafe int* UnsafeReturnMethod(); // 0x00562F00
	public unsafe int* UnsafeMethod2(int* i); // 0x00562F08
}

