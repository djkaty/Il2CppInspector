using System;

/* Test for concretized and partially concretized generics */
namespace Il2CppTests.TestSources
{
    public class FunkyA<AT>
    {
        public AT at;
        public FunkyA() {
        }
        public FunkyA(AT at) {
            this.at = at;
        }
        public AT rt(AT x) {
            return x;
        }
    }

    public class FunkyB<BT1, BT2> : FunkyA<BT2>
    {
        public BT1 bt1;
        public BT2 bt2;
        public BT2 rt1(BT1 x) {
            return bt2;
        }
        public BT1 rt2(BT2 y) {
            return bt1;
        }
        public FunkyA<FunkyA<BT1>> rtx(FunkyA<FunkyA<BT2>> z) {
            return new FunkyA<FunkyA<BT1>>(new FunkyA<BT1>(bt1));
        }
    }

    public class FunkyC<CT1, CT2, CT3> : FunkyB<CT3, CT1>
    {
    }

    public class FunkyTest
    {
        public FunkyA<int> x;
        public FunkyB<string, int> y;
        public FunkyC<object, int, string> z;
        void test() {
            x.rt(0);
            y.rt1("");
            y.rt2(0);
            z.rtx(null);
        }
    }
}
