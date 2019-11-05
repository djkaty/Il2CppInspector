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
[AttributeUsage] // 0x1000D430
public class ExampleAttribute : Attribute // TypeDefIndex: 1812
{
	// Fields
	private ExampleKind kindValue; // 0x08
	private string noteValue; // 0x0C
	private string[] arrayStrings; // 0x10
	private int[] arrayNumbers; // 0x14

	// Properties
	public ExampleKind Kind { get; } // 0x100BF0C0 
	public string[] Strings { get; } // 0x100BFC20 
	public string Note { get; set; } // 0x100C5B30 0x100C5B50
	public int[] Numbers { get; set; } // 0x100EF0C0 0x10127B00

	// Constructors
	public ExampleAttribute(ExampleKind initKind, string[] initStrings); // 0x1034DA90
	public ExampleAttribute(ExampleKind initKind); // 0x1034DAC0
	public ExampleAttribute(); // 0x1034DA60

}

// Namespace: Il2CppTests.TestSources
[Example] // 0x1000D450
public class Test // TypeDefIndex: 1813
{
	// Constructors
	public Test(); // 0x100BF000

	// Methods
	[Example] // 0x1000D550
	public void TestMethod([Example] /* 0x1000D590 */ object arg); // 0x100C5530
}

