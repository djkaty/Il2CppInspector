/*
 * Generated code file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Image 0: mscorlib.dll - 0
[assembly: AssemblyCompany] // 0x000000018000E050
[assembly: AssemblyCopyright] // 0x000000018000E050
[assembly: AssemblyDefaultAlias] // 0x000000018000E050
[assembly: AssemblyDelaySign] // 0x000000018000E050
[assembly: AssemblyDescription] // 0x000000018000E050
[assembly: AssemblyFileVersion] // 0x000000018000E050
[assembly: AssemblyInformationalVersion] // 0x000000018000E050
[assembly: AssemblyKeyFile] // 0x000000018000E050
[assembly: AssemblyProduct] // 0x000000018000E050
[assembly: AssemblyTitle] // 0x000000018000E050
[assembly: CLSCompliant] // 0x000000018000E050
[assembly: CompilationRelaxations] // 0x000000018000E050
[assembly: ComVisible] // 0x000000018000E050
[assembly: Debuggable] // 0x000000018000E050
[assembly: DefaultDependency] // 0x000000018000E050
[assembly: Guid] // 0x000000018000E050
[assembly: NeutralResourcesLanguage] // 0x000000018000E050
[assembly: RuntimeCompatibility] // 0x000000018000E050
[assembly: SatelliteContractVersion] // 0x000000018000E050
[assembly: StringFreezing] // 0x000000018000E050
[assembly: TypeLibVersion] // 0x000000018000E050

// Image 1: GenericTypes.dll - 1810
[assembly: CompilationRelaxations] // 0x000000018000C150
[assembly: Debuggable] // 0x000000018000C150
[assembly: RuntimeCompatibility] // 0x000000018000C150

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
	public static string GetText(string fmt, params /* 0x00000001800090D0 */ object[] args); // 0x00000001802C64F0
}

namespace Il2CppTests.TestSources
{
	public class Base<T, U> // TypeDefIndex: 1815
	{
		// Constructors
		public Base();
	}

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

	public class G<T> // TypeDefIndex: 1818
	{
		// Constructors
		public G();
	}

	internal class Test // TypeDefIndex: 1819
	{
		// Constructors
		public Test(); // 0x00000001800E2000
	
		// Methods
		public void GenericTypesTest(); // 0x00000001803E0ED0
	}

	internal class ConstrainedValueType<V> // TypeDefIndex: 1820
		where V : struct
	{
		// Constructors
		public ConstrainedValueType();
	}

	internal class ConstrainedRefType<R> // TypeDefIndex: 1821
		where R : class
	{
		// Constructors
		public ConstrainedRefType();
	
		// Methods
		[NullableContext] // 0x0000000180009190
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
}
