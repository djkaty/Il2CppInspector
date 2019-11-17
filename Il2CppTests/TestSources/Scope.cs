/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

#pragma warning disable CS0169
namespace Il2CppTests.TestSources
{
    public class NestedOuter { // NestedOuter
        public NestedOuter Test1() => default;
        public NestedInner Test2() => default;
        public NestedInner.NestedSubInner Test3() => default;
        public NestedInner.NestedSubInner.NestedSubSubInner Test4() => default;

        public class NestedInner // NestedOuter.NestedInner
        {
            public NestedOuter Test1() => default;
            public NestedInner Test2() => default;
            public NestedSubInner Test3() => default;
            public NestedSubInner.NestedSubSubInner Test4() => default;

            public class NestedSubInner // NestedOuter.NestedInner.NestedSubInner
            {
                public NestedOuter Test1() => default;
                public NestedInner Test2() => default;
                public NestedSubInner Test3() => default;
                public NestedSubSubInner Test4() => default;

                public class NestedSubSubInner // NestedOuter.NestedInner.NestedSubInner.NestedSubSubInner
                {
                    public NestedOuter Test1() => default;
                    public NestedInner Test2() => default;
                    public NestedSubInner Test3() => default;
                    public NestedSubSubInner Test4() => default;
                }
            }

            public class NestedGeneric<T> {}

            public struct NestedStruct {}
        }

        // Generic nested scopes
        public NestedInner.NestedGeneric<NestedInner.NestedSubInner.NestedSubSubInner> GenericNestingScopes() => default;

        // Nullable type in nested scope
        public NestedInner.NestedStruct? NullableStruct() => default;

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
    }
}

// Same base namespace
namespace Il2CppTests.DifferentNamespace
{
    public class NestedOuter
    {
        public NestedOuter Test1() => default;
        public TestSources.NestedOuter Test2() => default;
        public TestSources.NestedOuter.NestedInner Test3() => default;
    }
}

// Different namespace with matching name at leaf level as previous namespace
namespace DifferentNamespace
{
    public class NestedOuter
    {
        public NestedOuter Test1() => default;
        public Il2CppTests.DifferentNamespace.NestedOuter Test2() => default;
        public Il2CppTests.TestSources.NestedOuter Test3() => default;
        public Il2CppTests.TestSources.NestedOuter.NestedInner Test4() => default;

        public class NestedIntermediate
        {
            public class NestedInner
            {
                public Il2CppTests.TestSources.NestedOuter Test1() => default;
                public Il2CppTests.TestSources.NestedOuter.NestedInner Test2() => default;
                public Il2CppTests.TestSources.NestedOuter.NestedInner.NestedSubInner Test3() => default;
            }
        }
    }

    public class TwoLevelConflictingParentScope
    {
        public class NestedOuter
        {
            public class NestedInner
            {
                public Il2CppTests.TestSources.NestedOuter.NestedInner.NestedSubInner Test1() => default;

                public class NestedSubInner
                {

                }
            }
        }
    }
}
#pragma warning restore CS0169
