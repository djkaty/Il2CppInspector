/*
    Copyright 2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Il2CppTests.TestSources;

// This code is adapted from https://docs.microsoft.com/en-us/dotnet/api/system.reflection.customattributedata?view=netframework-4.8

// The example attribute is applied to the assembly.
[assembly: Example(ExampleKind.ThirdKind, Note = "This is a note on the assembly.")]

namespace Il2CppTests.TestSources
{
    // An enumeration used by the ExampleAttribute class.
    public enum ExampleKind
    {
        FirstKind,
        SecondKind,
        ThirdKind,
        FourthKind
    };

    // An example attribute. The attribute can be applied to all
    // targets, from assemblies to parameters.
    [AttributeUsage(AttributeTargets.All)]
    public class ExampleAttribute : Attribute
    {
        // Data for properties.
        private ExampleKind kindValue;
        private string noteValue;
        private string[] arrayStrings;
        private int[] arrayNumbers;

        // Constructors. The parameterless constructor (.ctor) calls
        // the constructor that specifies ExampleKind and an array of 
        // strings, and supplies the default values.
        public ExampleAttribute(ExampleKind initKind, string[] initStrings) {
            kindValue = initKind;
            arrayStrings = initStrings;
        }
        public ExampleAttribute(ExampleKind initKind) : this(initKind, null) { }
        public ExampleAttribute() : this(ExampleKind.FirstKind, null) { }

        // Properties. The Note and Numbers properties must be read/write, so they
        // can be used as named parameters.
        public ExampleKind Kind => kindValue;
        public string[] Strings => arrayStrings;

        public string Note {
            get { return noteValue; }
            set { noteValue = value; }
        }
        public int[] Numbers {
            get { return arrayNumbers; }
            set { arrayNumbers = value; }
        }
    }

    // The example attribute is applied to the test class.
    [Example(ExampleKind.SecondKind,
             new[] {   "String array argument, line 1",
                                "String array argument, line 2",
                                "String array argument, line 3" },
             Note = "This is a note on the class.",
             Numbers = new[] { 53, 57, 59 })]
    public class Test
    {
        // The example attribute is applied to a method, using the
        // parameterless constructor and supplying a named argument.
        // The attribute is also applied to the method parameter.
        [Example(Note = "This is a note on a method.")]
        public void TestMethod([Example] object arg) { }
    }
}
