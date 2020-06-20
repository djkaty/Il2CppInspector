/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System;
using System.Linq;

/* C# 3.0 feature test */
namespace Il2CppTests.TestSources
{
    public class FeatureTest
    {
        public string AutoProp { get; set; }
        public void AnonType() {
            var c = new { Value = 3, Message = "Nobody" };
            Console.WriteLine(c);
        }

        public int Linq() {
            var scores = new int[] { 1, 2, 3, 4 };
            var highScoresQuery =
                from score in scores
                where score > 80
                orderby score descending
                select score;
            return highScoresQuery.Count();
        }
    }
}
