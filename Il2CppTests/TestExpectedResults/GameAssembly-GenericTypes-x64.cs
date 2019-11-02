// Image 0: mscorlib.dll - 0
// Image 1: GenericTypes.dll - 1810

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
	public class Nested<V> // TypeDefIndex: 1813
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
	public Test(); // 0x00000001800E2000

	// Methods
	public void GenericTypesTest(); // 0x00000001803E0E50
}

