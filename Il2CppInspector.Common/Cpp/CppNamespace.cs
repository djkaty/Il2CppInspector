/*
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Il2CppInspector.Cpp
{
    /// <summary>
    /// A utility class for managing names in a common namespace.
    /// </summary>
    public class CppNamespace
    {
        // The central data structure that keeps track of which names have been generated
        // The value for any given key K is the number of unique objects originally named K, minus 1.
        // Each time we see a particular name assigned to a new, different object, we bump its rename count
        // and give it a suffix. For example, if we have three different objects all named X,
        // we'd name them X, X_1, and X_2, and renameCount["X"] would be 2.
        private readonly Dictionary<string, int> renameCount = new Dictionary<string, int>();

        // Mark a name as reserved without assigning an object to it (e.g. for keywords and built-in names)
        public void ReserveName(string name) {
            if (renameCount.ContainsKey(name)) {
                throw new Exception($"Can't reserve {name}: already taken!");
            }
            renameCount[name] = 0;
        }

        // Create a Namer object which will give names to objects of type T which are unique within this namespace
        public Namer<T> MakeNamer<T>(Namer<T>.KeyFunc keyFunc) {
            return new Namer<T>(this, keyFunc);
        }

        /// <summary>
        /// A class for managing objects of a common type within a namespace.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class Namer<T>
        {
            // Parent namespace
            private CppNamespace ns;

            // Names given out by this Namer.
            private readonly Dictionary<T, string> names = new Dictionary<T, string>();

            // The function which maps a T object to a suitably mangled name
            // That name might be further mangled by the Namer to make the name unique within the namespace
            public delegate string KeyFunc(T t);
            private readonly KeyFunc keyFunc;

            public Namer(CppNamespace ns, KeyFunc keyFunc) {
                this.ns = ns;
                this.keyFunc = keyFunc;
            }

            // Uniquely name an object within the parent namespace
            public string GetName(T t) {
                // If we've named this particular object before, just return that name
                string name;
                if (names.TryGetValue(t, out name))
                    return name;
                // Obtain the mangled name for the object
                name = keyFunc(t);
                // Check if the mangled name has been given to another object - if it has,
                // we need to give the object a new suffixed name (e.g. X_1).
                // We might need to repeat this process if the new suffixed name also exists.
                // Each iteration tacks on another suffix - so we normally expect this to only take
                // a single iteration. (It might take multiple iterations in rare cases, e.g.
                // another object had the mangled name X_1).
                if (ns.renameCount.ContainsKey(name)) {
                    int v = ns.renameCount[name] + 1;
                    while (ns.renameCount.ContainsKey(name + "_" + v))
                        v++;
                    ns.renameCount[name] = v;
                    name = name + "_" + v;
                }
                ns.renameCount[name] = 0;
                names[t] = name;
                return name;
            }
        }
    }
}
