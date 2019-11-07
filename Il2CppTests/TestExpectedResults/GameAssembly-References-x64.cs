// Image 0: mscorlib.dll - 0
// Image 1: References.dll - 1810

// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1811
{
	// Fields
	private float floatField; // 0x10

	// Constructors
	public Test(); // 0x00000001800E2000

	// Methods
	public void MethodWithRefParameters(int a, ref int b, int c, ref int d); // 0x00000001800EA7B0
	public void MethowWithInRefOut(in int a, ref int b, out int c); // 0x00000001803E0E60
	public ref float MethodWithRefReturnType(); // 0x00000001803E0E50
}

// Namespace: Il2CppTests.TestSources
[Obsolete] // 0x000000018000CB80
public struct RefStruct // TypeDefIndex: 1812
{
	// Fields
	private int structField1; // 0x10

}

