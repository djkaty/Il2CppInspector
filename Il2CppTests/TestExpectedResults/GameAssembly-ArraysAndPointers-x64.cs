// Image 0: mscorlib.dll - 0
// Image 1: ArraysAndPointers.dll - 1810


// Namespace: Il2CppTests.TestSources
internal class Test // TypeDefIndex: 1813
{
	// Fields
	private int[] foo; // 0x10
	private int[] bar; // 0x20
	private float[][] arrayOfArrays; // 0x30
	private float[,] twoDimensionalArray; // 0x40
	private float[,,] threeDimensionalArray; // 0x50
	private unsafe int*[] arrayOfPointer; // 0x10
	private unsafe int** pointerToPointer; // 0x19
	private unsafe float*[][,,][] confusedElephant; // 0x8047EDC0

	// Properties
	public unsafe int* PointerProperty { get; set; } // 0x00000001801513A0 0x00000001801140B0
	public unsafe int* this[int i] { get; } // 0x00000001800EA8C0 
	public unsafe int this[int* p] { get; } // 0x00000001800EA8C0 
	public unsafe float* this[float* fp] { get; } // 0x00000001800EA8C0 

	// Nested types
	private struct fixedSizeArrayStruct // TypeDefIndex: 1814
	{
		// Fields
		private unsafe fixed /* 0x000000018000C310 */ int fixedSizeArray[0]; // 0x10

		// Nested types

	}

	public unsafe delegate void OnUnsafe(int* ud); // TypeDefIndex: 1816; 0x00000001803E0E50

	public class NestedUnsafe<T> // TypeDefIndex: 1817
	{
		// Constructors
		public NestedUnsafe();

		// Methods
		private unsafe T* UnsafeGenericReturn();
		private unsafe void UnsafeGenericMethod(T* pt);
	}

	// Constructors
	public unsafe Test(int* u); // 0x00000001803E1130

	// Methods
	public int[] FooMethod(int[][] bar); // 0x00000001803E10F0
	public int[,] BarMethod(int[,,] baz); // 0x00000001803E10A0
	public unsafe void UnsafeMethod(int* unsafePointerArgument); // 0x00000001800EA7B0
	public unsafe int* UnsafeReturnMethod(); // 0x00000001800EA8C0
	public unsafe int* UnsafeMethod2(int* i); // 0x000000018012FC60
}

