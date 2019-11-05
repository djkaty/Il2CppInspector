// Image 0: mscorlib.dll - 0
// Image 1: ArraysAndPointers.dll - 1810

// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1811
{
	// Fields
	private int[] foo; // 0x08
	private int[] bar; // 0x0C
	private float[][] arrayOfArrays; // 0x10
	private float[,] twoDimensionalArray; // 0x14
	private float[,,] threeDimensionalArray; // 0x18
	private int*[] arrayOfPointer; // 0x1C
	private int** pointerToPointer; // 0x20
	private float*[][,,][] confusedElephant; // 0x24

	// Nested types
	private struct fixedSizeArrayStruct // TypeDefIndex: 1812
	{
		// Fields
		private fixed /* 0x1000DE40 */ int fixedSizeArray[0]; // 0x08

		// Nested types

	}

	// Constructors
	public Test(); // 0x1034DAE0

	// Methods
	public int[] FooMethod(int[][] bar); // 0x1034DAB0
	public int[,] BarMethod(int[,,] baz); // 0x1034DA60
}

