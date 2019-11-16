/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;

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

    // Constraints on type definitions
    internal class ConstrainedValueType<V> where V : struct {} // Value type constraint
    internal class ConstrainedRefType<R> where R : class { // Reference type constraint

        // Constraints on method definitions
#nullable enable
        public void ConstrainedMethodNotNull<N>(N notnullArgument, R bar) where N : notnull {} // Non-nullable reference type constraint (suppressed if not in nullable enable context)
#nullable restore
        public unsafe void ConstrainedUnmanaged<U>(U unmanagedArgument) where U : unmanaged {} // Unmanaged type constraint (added in C# 7.3; suppressed if not in unsafe context)

        // Multiple constraints
        public void MultipleConstraintsMethod<C>(C constrained) where C : R, new() {} // Derived type argument constraint + public parameterless constructor constraint

        // Multiple type arguments with multiple constraints
        public void MultipleArgumentsMultipleConstraintsMethod<B, I>(B baseArgument, I interfaceArgument)
            where B : Derived<R>, new() // Base type constraint + public parameterless constructor constraint
            where I : Test, IDisposable, IEnumerable<R> // Base type constraint + Interface implementation constraint x2
        { }

        // Special type constraints (these must be specified as their full type names and not the C# shorthand versions
        // Added in C# 7.3
        public void DelegateConstraint<D>(D del) where D : Delegate {}
        public void EnumConstraint<E>(E enumeration) where E : Enum {}

        // Nested types inherit parent constraints but should not be output
        private class NestedWithAutomaticConstraints {}

        // Generates a compiler warning
        //private class NestedWithDeclaringTypeGenericParameter<R> {}

        private class NestedWithNewGenericParameter<T> {}

        private class NestedWithNewGenericParameterAndConstraint<T> where T : new() {}

        private class NestedWithNewGenericParameterAndDependentConstraint<T> where T : G<R>, new() {}

        private enum NestedEnumWithAutomaticConstraints {}

        private delegate void NestedDelegateWithAutomaticConstraints();
    }
}