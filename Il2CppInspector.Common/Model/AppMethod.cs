/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using Il2CppInspector.Cpp;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Model
{
    // Class that represents a composite IL/C++ method
    public class AppMethod
    {
        // The corresponding C++ function pointer type
        public CppFnPtrType CppFnPtrType { get; internal set; }

        // The corresponding .NET method
        public MethodBase Method { get; internal set; }

        // The VA of the MethodInfo* (VA of the pointer to the MethodInfo) object which defines this method
        public ulong MethodInfoPtrAddress { get; internal set; } 

        // The VA of the method code itself, or 0 if unknown/not compiled
        public ulong MethodCodeAddress => Method.VirtualAddress?.Start ?? 0;

        public AppMethod(MethodBase method, CppFnPtrType cppMethod, ulong methodInfoPtr = 0xffffffff_ffffffff) {
            Method = method;
            CppFnPtrType = cppMethod;
            MethodInfoPtrAddress = methodInfoPtr;
        }

        public override string ToString() => CppFnPtrType.ToSignatureString();
    }
}
