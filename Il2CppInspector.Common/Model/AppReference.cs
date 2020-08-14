/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using Il2CppInspector.Cpp;

namespace Il2CppInspector.Model
{
    // Class that represents a single pointer in the address map which references a method or type
    public class AppReference
    {
        public CppField Field { get; set; }
    }

    // Reference to a method (MethodInfo *)
    public class AppMethodReference : AppReference
    {
        public AppMethod Method { get; set; }
    }

    // Reference to a type (Il2CppObject * or Il2CppType *, use Field to determine which)
    public class AppTypeReference : AppReference
    {
        // The corresponding C++ function pointer type
        public AppType Type { get; set; }
    }
}
