using System;
using System.Collections.Generic;
using System.Text;

namespace Il2CppInspector.Outputs
{
    /// <summary>
    /// A utility class for managing names in a common namespace.
    /// </summary>
    public class Namespace
    {
        private readonly Dictionary<string, int> renameCount = new Dictionary<string, int>();

        public void ReserveName(string name) {
            if (renameCount.ContainsKey(name)) {
                throw new Exception($"Can't reserve {name}: already taken!");
            }
            renameCount[name] = 0;
        }

        public Namer<T> MakeNamer<T>(Namer<T>.KeyFunc keyFunc) {
            return new Namer<T>(this, keyFunc);
        }

        /// <summary>
        /// A class for managing objects of a common type within a namespace.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class Namer<T>
        {
            private Namespace ns;
            private readonly Dictionary<T, string> names = new Dictionary<T, string>();
            public delegate string KeyFunc(T t);
            private readonly KeyFunc keyFunc;

            public Namer(Namespace ns, KeyFunc keyFunc) {
                this.ns = ns;
                this.keyFunc = keyFunc;
            }

            public string GetName(T t) {
                string name;
                if (names.TryGetValue(t, out name))
                    return name;
                name = keyFunc(t);
                // This approach avoids linear scan (quadratic blowup) if there are a lot of similarly-named objects.
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
