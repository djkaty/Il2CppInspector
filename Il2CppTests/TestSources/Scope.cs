/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using Il2CppTests.DifferentNamespace;
using Il2CppTests.TestSources;
using Some.Namespace;
using Some.Namespace.Again.SameLeafName;
using Some.Namespace.SameLeafName;

#pragma warning disable CS0169

// Type in global namespace should not overwrite selection of type with namespace
public class TestGlobal {}

namespace NotGlobalNamespace
{
    public class TestGlobal {}
}

namespace NotGlobalUsingNamespace
{
    public class TestUsingNonGlobalNamespaceType
    {
        public NotGlobalNamespace.TestGlobal Test() => default;
    }
}

// Namespace nesting and using directive tests
namespace Some.Namespace
{
    public class Test
    {
        // Namespace references can either be children...
        public Again.SameLeafName.Test foo;
    }
}

namespace Some.Namespace.Again.SameLeafName
{
    public class Test
    {
        // ..or from a parent
        public Namespace.Test foo;

        // Test to make sure this references the correct namespace
        public AClassFromUsingDirective Test1() => default;
    }
}

namespace Some.Namespace.SameLeafName
{
    public class AClassFromUsingDirective {}
}


// Different namespace with matching name at leaf level as Il2CppTests.DifferentNamespace
namespace DifferentNamespace
{
    public class NestedOuter
    {
        public class NestedIntermediate
        {
            public class NestedInner
            {
                public Il2CppTests.TestSources.NestedOuter Test1() => default;
                public Il2CppTests.TestSources.NestedOuter.NestedInner Test2() => default;
                public Il2CppTests.TestSources.NestedOuter.NestedInner.NestedSubInner Test3() => default;
            }
        }

        public NestedOuter Test1() => default;
        public Il2CppTests.DifferentNamespace.NestedOuter Test2() => default;
        public Il2CppTests.TestSources.NestedOuter Test3() => default;
        public Il2CppTests.TestSources.NestedOuter.NestedInner Test4() => default;
    }

    public class TwoLevelConflictingParentScope
    {
        public class NestedOuter
        {
            public class NestedInner
            {
                public class NestedSubInner
                {

                }

                public Il2CppTests.TestSources.NestedOuter.NestedInner.NestedSubInner Test1() => default;
            }
        }
    }
}

// Same base namespace as Il2CppTests.Sources
namespace Il2CppTests.DifferentNamespace
{
    public class NestedOuter
    {
        public NestedOuter Test1() => default;
        public TestSources.NestedOuter Test2() => default;
        public TestSources.NestedOuter.NestedInner Test3() => default;
    }
}

// Nested class tests
namespace Il2CppTests.TestSources
{
    public class NestedOuter { // NestedOuter
        public class NestedInner // NestedOuter.NestedInner
        {
            public class NestedSubInner // NestedOuter.NestedInner.NestedSubInner
            {
                public class NestedSubSubInner // NestedOuter.NestedInner.NestedSubInner.NestedSubSubInner
                {
                    public NestedOuter Test1() => default;
                    public NestedInner Test2() => default;
                    public NestedSubInner Test3() => default;
                    public NestedSubSubInner Test4() => default;
                }

                public NestedOuter Test1() => default;
                public NestedInner Test2() => default;
                public NestedSubInner Test3() => default;
                public NestedSubSubInner Test4() => default;
            }

            public NestedOuter Test1() => default;
            public NestedInner Test2() => default;
            public NestedSubInner Test3() => default;
            public NestedSubInner.NestedSubSubInner Test4() => default;

            public class NestedGeneric<T> {}

            public struct NestedStruct {}
        }

        // Nested siblings
        public class NestedInnerSibling
        {
            public class NestedSubInnerSibling
            {
                public NestedOuter Test1() => default;
                public NestedInner Test2() => default;
                public NestedInner.NestedSubInner Test3() => default;
            }
        }

        public NestedOuter Test1() => default;
        public NestedInner Test2() => default;
        public NestedInner.NestedSubInner Test3() => default;
        public NestedInner.NestedSubInner.NestedSubSubInner Test4() => default;

        // Generic nested scopes
        public NestedInner.NestedGeneric<NestedInner.NestedSubInner.NestedSubSubInner> GenericNestingScopes() => default;

        // Nullable type in nested scope
        public NestedInner.NestedStruct? NullableStruct() => default;
    }
}
#pragma warning restore CS0169
