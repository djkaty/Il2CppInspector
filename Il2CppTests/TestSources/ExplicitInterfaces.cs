/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;

#pragma warning disable CS0169
namespace Il2CppTests.TestSources
{
    // See: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation

    interface IControl
    {
        void Paint();
    }

    interface ISurface
    {
        void Paint();
    }

    internal class Test : IControl, ISurface
    {
        // No access modifiers on explicit interface implementation
        void IControl.Paint() {}
        void ISurface.Paint() {}
    }

    interface ILeft
    {
        int P { get; }
    }

    interface IRight
    {
        int P();
    }

    internal class Middle : ILeft, IRight
    {
        // One or the other has to be explicitly implemented to resolve naming conflicts
        public int P() => default;
        int ILeft.P => default;
    }

    // Generic implementation
    interface IGeneric<in T>
    {
        void GenericMethod(T t);
    }

    internal class ImplementsGenericInterface : IGeneric<KeyValuePair<int, double>>
    {
        void IGeneric<KeyValuePair<int, double>>.GenericMethod(KeyValuePair<int, double> t) {}
    }

    // Explicit implementation of an indexer
    interface IIndexer
    {
        bool this[int i] { get; }
    }

    internal class ImplementsIndexer : IIndexer
    {
        // Normal indexer
        public bool this[int i] => default;

        // Explicit interface indexer
        bool IIndexer.this[int i] => default;
    }
}
#pragma warning restore CS0169
