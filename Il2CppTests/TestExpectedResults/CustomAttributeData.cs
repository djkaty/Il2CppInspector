// Image 0: mscorlib.dll - 0
// Image 1: CustomAttributeData.dll - 1810

// Namespace: Il2CppTests.TestSources
public enum ExampleKind // TypeDefIndex: 1811
{
	FirstKind = 0,
	SecondKind = 1,
	ThirdKind = 2,
	FourthKind = 3
}

// Namespace: Il2CppTests.TestSources
[AttributeUsage] // 0x000A5070
public class ExampleAttribute : Attribute // TypeDefIndex: 1812
{
	// Fields
	private ExampleKind kindValue; // 0x08
	private string noteValue; // 0x0C
	private string[] arrayStrings; // 0x10
	private int[] arrayNumbers; // 0x14

	// Properties
	public ExampleKind Kind { get; } // 0x005612FC 
	public string[] Strings { get; } // 0x00561304 
	public string Note { get; set; } // 0x0056130C 0x00561314
	public int[] Numbers { get; set; } // 0x0056131C 0x00561324

	// Constructors
	public ExampleAttribute(ExampleKind initKind, string[] initStrings); // 0x00561288
	public ExampleAttribute(ExampleKind initKind); // 0x005612B0
	public ExampleAttribute(); // 0x005612D8

}

// Namespace: Il2CppTests.TestSources
[Example] // 0x000A5084
public class Test // TypeDefIndex: 1813
{
	// Constructors
	public Test(); // 0x00561330

	// Methods
	[Example] // 0x000A5208
	public void TestMethod([Example] /* 0x000A525C */ object arg); // 0x0056132C
}

