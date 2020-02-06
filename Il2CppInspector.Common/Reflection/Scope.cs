/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;

namespace Il2CppInspector.Reflection
{
    // A code scope with which to evaluate how to output type references
    public class Scope
    {
        // The scope we are currently in
        public TypeInfo Current;

        // The list of namespace using directives in the file
        public IEnumerable<string> Namespaces;
    }
}
