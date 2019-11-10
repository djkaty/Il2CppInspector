// Image 0: mscorlib.dll - 0
// Image 1: ArraysAndPointers.dll - 1810


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

		// Nested types

	}

	public unsafe delegate void OnUnsafe(int* ud); // TypeDefIndex: 1816; 0x00562F3C

	public class NestedUnsafe<T> // TypeDefIndex: 1817
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

