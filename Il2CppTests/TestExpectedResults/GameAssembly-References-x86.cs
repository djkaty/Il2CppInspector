// Image 0: mscorlib.dll - 0
// Image 1: References.dll - 1810

// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1811
{
	// Fields
	private float floatField; // 0x08

	// Constructors
	public Test(); // 0x100BF000

	// Methods
	public void MethodWithRefParameters(int a, ref int b, int c, ref int d); // 0x100C5530
	public void MethowWithInRefOut(in int a, ref int b, out int c); // 0x1034DA70
	public ref float MethodWithRefReturnType(); // 0x1034DA60
}

// Namespace: Il2CppTests.TestSources
[Obsolete] // 0x1000DD10
public struct RefStruct // TypeDefIndex: 1812
{
	// Fields
	private int structField1; // 0x08

}

