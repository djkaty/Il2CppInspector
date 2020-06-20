/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System;

/* Test virtual method calls (VTables) and class layout */
namespace Il2CppTests.TestSources
{
    public struct Vector3
    {
        public float x, y, z;
    }

    public class TestVTable
    {
        public interface TestInterface
        {
            int overrideme();
        }

        public interface TestInterface2
        {
            int overrideme2();
        }

        public interface TestInterface3
        {
            int overrideme3();
        }

        public interface IT1
        {
            void f1();
            void f2();
        }

        public interface IT2
        {
            void f1();
            void f3();
        }

        public interface IT3 : IT2, IT1
        {
            new void f2();
            new void f1();
        }

        public class WeirdLayout1
        {
            public ulong x;
            public char y;
        }

        public class WeirdLayout2 : WeirdLayout1
        {
            public char z;
            public short f;
        }

        public struct TestStruct : TestInterface, TestInterface2, IT1, IT2
        {
            public int x;
            public int overrideme() {
                return 42 + x;
            }

            public int overrideme2() {
                return 42000 * x;
            }

            public void f1() {

            }

            public void f2() {

            }

            public void f3() {

            }
        }

        public class TestClass : TestInterface, TestInterface3, IT3
        {
            public int x;
            public virtual int overrideme() {
                return 64 - x;
            }
            public void normal1() {

            }
            public virtual int overrideme3() {
                return -1 + x;
            }
            public void normal2() {

            }
            public void f1() { }
            public void f2() { }
            public void f3() { }
        }

        public class TestClass2 : TestClass
        {
            public int y;
            public override int overrideme() {
                return 1 + y;
            }
            public new void normal2() {

            }
        }

        public interface ITestGeneric<T>
        {
            void genericFunc(T t);
        }

        public class TestGeneric<T> : ITestGeneric<T>
        {
            public T m_t;
            public T[] arr;
            public int x;
            public TestGeneric(T[] arr) {
                this.arr = arr;
                this.x = 0;
                for (int i = 0; i < arr.Length; i++) {
                    x += arr[i].GetHashCode();
                }
            }
            public void genericFunc(T t) {
                x += arr[0].Equals(t) ? 1 : 0;
            }
        }

        public class TestGeneric2<T1, T2> : TestGeneric<T2>
        {
            public T1 m_t1;
            public T2 m_t2;
            public TestGeneric2(T2[] t2) : base(t2) {

            }
            public void secondGenericFunc(T1 t1, T2 t2) {
                genericFunc(t2);
                TestGeneric<T1> tg1 = new TestGeneric<T1>(null);
                tg1.genericFunc(t1);
            }
        }

        public void overrideme(int x) {
            Console.WriteLine(x);
        }
        public float takestruct(Vector3 a, Vector3 b, Vector3 c) {
            return a.x + b.y + c.z;
        }
        public delegate void callit(int x);
        public callit monkey;

        public int doit(TestInterface ti, TestInterface2 ti2, TestInterface3 ti3) {
            return ti.overrideme() * 2 + ti2.overrideme2() + ti3.overrideme3();
        }

        public void calltypes(ref Vector3 vin, ref Vector3 vout, ref Vector3 vref, params Vector3[] vparams) {
            vout = vin;
        }

        public void callgeneric<T>(ITestGeneric<T> it, T t) {
            it.genericFunc(t);
        }

        public void callints(IT1 it1, IT2 it2, IT3 it3) {
            it1.f1();
            it1.f2();
            it2.f1();
            it2.f3();
            it3.f1();
            it3.f2();
            it3.f3();
        }

        public int[] intProperty { get; set; }
        public int[] intProp2 { get { return intArray; } set { intArray = value; } }
        private int[] intArray;
        public object[] objArray;

        void Start() {
            if (monkey != null)
                monkey(0);
            TestClass i1 = new TestClass2();
            i1.overrideme();
            i1.overrideme3();
            i1.normal2();
            TestStruct i2 = new TestStruct();
            i2.overrideme();
            i2.overrideme2();
            doit(i1, i2, i1);
            doit(i2, i2, i1);
            callints(i2, i2, i1);
            int res = 0;
            for (int i = 0; i < intArray.Length; i++) {
                res += intArray[i];
            }
            for (int i = 0; i < objArray.Length; i++) {
                res += (int)objArray[i];
            }
            ITestGeneric<int> x1 = new TestGeneric<int>(new int[] { 1, 2, 3 });
            ITestGeneric<object> x2 = new TestGeneric<object>(new object[] { new object() });
            callgeneric(x1, 0);
            callgeneric(x2, "foo");
            ITestGeneric<string> x3 = new TestGeneric2<int, string>(new string[] { "as", "de" });
            new WeirdLayout2().f = 3;
            x3.genericFunc("bar");
        }

        void Update() {
            overrideme(0);
        }
    }
}
