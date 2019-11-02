/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;

namespace Il2CppTests.TestSources
{
    public class Base<T, U> { }

    public class Derived<V> : Base<string, V>
    {
        public G<Derived<V>> F;

        public class Nested { }
    }

    public class G<T> { }

    internal class Test
    {
        public void GenericTypesTest() {
            // Get the generic type definition for Derived, and the base
            // type for Derived.
            //

            Type tDerived = typeof(Derived<>);
            Type tDerivedBase = tDerived.BaseType;

            // Declare an array of Derived<int>, and get its type.
            //
            Derived<int>[] d = new Derived<int>[0];
            Type tDerivedArray = d.GetType();

            // Get a generic type parameter, the type of a field, and a
            // type that is nested in Derived. Notice that in order to
            // get the nested type it is necessary to either (1) specify
            // the generic type definition Derived<>, as shown here,
            // or (2) specify a type parameter for Derived.
            //
            Type tT = typeof(Base<,>).GetGenericArguments()[0];
            Type tF = tDerived.GetField("F").FieldType;
            Type tNested = typeof(Derived<>.Nested);
        }
    }
}