// Image 0: mscorlib.dll - 0
// Image 1: ArraysAndPointers.dll - 1810

// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1811
{
	// Fields
	private int[] foo; // 0x10
	private int[] bar; // 0x20
	private float[][] arrayOfArrays; // 0x30
	private float[,] twoDimensionalArray; // 0x40
	private float[,,] threeDimensionalArray; // 0x10
	private int*[] arrayOfPointer; // 0x29
	private int** pointerToPointer; // 0x8047EC30
	private float*[][,,][] confusedElephant; // 0x00

	// Nested types
	private struct fixedSizeArrayStruct // TypeDefIndex: 1812
	{
		// Fields
		private fixed /* 0x000000018000CC70 */ int fixedSizeArray[0]; // 0x10

		// Nested types

	}

	// Constructors
	public Test(); // 0x00000001803E0EE0

	// Methods
	public int[] FooMethod(int[][] bar); // 0x00000001803E0EA0
	public int[,] BarMethod(int[,,] baz); // 0x00000001803E0E50
}

