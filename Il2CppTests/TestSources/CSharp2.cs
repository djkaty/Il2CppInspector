/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System;

/* C# 2.0 feature test */
namespace Il2CppTests.TestSources
{
    public interface GenericInterface<T>
    {
        T func(T v);
        T genericMethod<T2>(T v, T2 w);
    }

    public class GenericClass<T> : GenericInterface<T>
    {
        public T x;

        public T func(T v) {
            return v;
        }

        public T genericMethod<T2>(T v, T2 w) {
            return x;
        }

        public static T myMethod(T x) {
            return x;
        }

        public virtual bool returnBool() {
            return true;
        }
    }

    public class DerivedGenericClass<T, T2> : GenericClass<T>, GenericInterface<T2>
    {
        public T2 t2;
        public T2 func(T2 v) {
            return t2;
        }

        public T2 genericMethod<T3>(T2 v, T3 w) {
            return v;
        }

        public T2 newGenericMethod() {
            return t2;
        }

        public static int Return42() {
            return 42;
        }
    }

    public struct GenericStruct<T> : GenericInterface<T> where T : struct
    {
        public T a;
        public T func(T v) {
            throw new NotImplementedException();
        }

        public T genericMethod<T2>(T v, T2 w) {
            throw new NotImplementedException();
        }

        public T? genericMethod2(T x) {
            return (new GenericClass<T>()).returnBool() ? null : (T?)x;
        }

        public static GenericStruct<T> ReturnStruct(T y) {
            var res = new GenericStruct<T>();
            res.a = y;
            return res;
        }
    }

    public interface IVariance<in T1, out T2>
    {
        T2 func(T1 v);
    }

    public class UseGenerics
    {
        public static void Main(string[] args) {
            GenericStruct<int>[] arr = new GenericStruct<int>[3];
            arr[0] = GenericStruct<int>.ReturnStruct(3);
            DerivedGenericClass<string, string> gc = new DerivedGenericClass<string, string>();
            string c = gc.func("oops");
            Console.WriteLine(c);
            gc.genericMethod("hello", arr[0].genericMethod2(3) ?? 0);
            GenericInterface<string> s = gc;
            s.genericMethod("goodbye", c);
            GenericClass<object>.myMethod(42);
            IVariance<string, int> q = null;
            q.func("nope");

            foreach (var v in arr) {
                v.genericMethod(64, "nope");
            }
        }
    }
}
