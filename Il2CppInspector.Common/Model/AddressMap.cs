/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Il2CppInspector.Cpp;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Model
{
    // A map of absolutely everything in the binary we know about
    // Designed to be used by static analysis disassembly frameworks such as Capstone.NET
    public class AddressMap : IDictionary<ulong, object>
    {
        // IL2CPP application model
        public AppModel Model { get; }

        // Underlying collection
        // Objects this can return (subject to change):
        // - AppMethod                   (for method function body)
        // - AppMethodReference          (for MethodInfo *)
        // - List<CustomAttributeData>   (for custom attributes generator)
        // - MethodInvoker               (for a Method.Invoke think)
        // - string (System.String)      (for a string literal)
        // - Il2CppCodeRegistration
        // - Il2CppMetadataRegistration
        // - Dictionary<string, ulong>   (for Il2CppCodeGenModules *[])
        // - Il2CppCodeGenModule
        // - CppFnPtrType                (for il2cpp_codegen_register, Il2CPP API exports, unknown functions)
        // - Export                      (for exports)
        // - Symbol                      (for symbols)
        public SortedDictionary<ulong, object> Items { get; } = new SortedDictionary<ulong, object>();

        #region Surrogate implementation of IDictionary

        // Surrogate implementation of IDictionary
        public ICollection<ulong> Keys => ((IDictionary<ulong, object>) Items).Keys;
        public ICollection<object> Values => ((IDictionary<ulong, object>) Items).Values;
        public int Count => ((ICollection<KeyValuePair<ulong, object>>) Items).Count;
        public bool IsReadOnly => ((ICollection<KeyValuePair<ulong, object>>) Items).IsReadOnly;
        public object this[ulong key] { get => ((IDictionary<ulong, object>) Items)[key]; set => ((IDictionary<ulong, object>) Items)[key] = value; }

        public bool TryAdd(ulong addr, object item) => Items.TryAdd(addr, item);
        public void Add(ulong key, object value) => ((IDictionary<ulong, object>) Items).Add(key, value);
        public bool ContainsKey(ulong key) => ((IDictionary<ulong, object>) Items).ContainsKey(key);
        public bool Remove(ulong key) => ((IDictionary<ulong, object>) Items).Remove(key);
        public bool TryGetValue(ulong key, out object value) => ((IDictionary<ulong, object>) Items).TryGetValue(key, out value);
        public void Add(KeyValuePair<ulong, object> item) => ((ICollection<KeyValuePair<ulong, object>>) Items).Add(item);
        public void Clear() => ((ICollection<KeyValuePair<ulong, object>>) Items).Clear();
        public bool Contains(KeyValuePair<ulong, object> item) => ((ICollection<KeyValuePair<ulong, object>>) Items).Contains(item);
        public void CopyTo(KeyValuePair<ulong, object>[] array, int arrayIndex) => ((ICollection<KeyValuePair<ulong, object>>) Items).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<ulong, object> item) => ((ICollection<KeyValuePair<ulong, object>>) Items).Remove(item);
        public IEnumerator<KeyValuePair<ulong, object>> GetEnumerator() => ((IEnumerable<KeyValuePair<ulong, object>>) Items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) Items).GetEnumerator();
        #endregion

        public AddressMap(AppModel model) {
            Model = model;
            build();
        }

        private void build() {
            // Get handle to C++ types
            var cppTypes = Model.CppTypeCollection;

            // Start by adding all of the .NET methods from all groups
            var methodInfoPtrType = cppTypes.GetType("MethodInfo *");

            foreach (var method in Model.Methods.Values) {
                // Method function body
                // Shared generic methods may share the same address!
                if (method.HasCompiledCode)
                    TryAdd(method.MethodCodeAddress, method);

                // Method reference (MethodInfo *)
                if (method.HasMethodInfo)
                    Add(method.MethodInfoPtrAddress,
                        new AppMethodReference {
                            Field = new CppField($"{method.CppFnPtrType.Name}__MethodInfo", methodInfoPtrType),
                            Method = method
                        });
            }

            // Add all custom attributes generators
            // The compiler might perform ICF which will cause duplicates with the above
            foreach (var cag in Model.TypeModel.CustomAttributeGeneratorsByAddress)
                TryAdd(cag.Key, cag.Value);

            // Add all method invokers. Multiple invoker indices may reference the same function address
            foreach (var mi in Model.TypeModel.MethodInvokers.Where(m => m != null))
                TryAdd(mi.VirtualAddress.Start, mi);

            // String literals (metadata >= 19)
            if (!Model.StringIndexesAreOrdinals)
                foreach (var str in Model.Strings)
                    Add(str.Key, str.Value);

            // Type definitions and references
            var classPtrType = cppTypes.GetType("Il2CppClass *");
            var classRefPtrType = cppTypes.GetType("Il2CppType *");
            foreach (var type in Model.Types.Values) {
                if (type.TypeClassAddress != 0xffffffff_ffffffff)
                    Add(type.TypeClassAddress, new AppTypeReference {
                        Field = new CppField($"{type.Name}__TypeInfo", classPtrType),
                        Type = type
                    });

                if (type.TypeRefPtrAddress != 0xffffffff_ffffffff)
                    Add(type.TypeRefPtrAddress, new AppTypeReference {
                        Field = new CppField($"{type.Name}__TypeRef", classRefPtrType),
                        Type = type
                    });
            }
             
            // Internal metadata
            var binary = Model.Package.Binary;
            Add(binary.CodeRegistrationPointer, binary.CodeRegistration);
            Add(binary.MetadataRegistrationPointer, binary.MetadataRegistration);

            if (Model.Package.Version >= 24.2) {
                Add(binary.CodeRegistration.pcodeGenModules, binary.CodeGenModulePointers);

                foreach (var ptr in binary.CodeGenModulePointers)
                    Add(ptr.Value, binary.Modules[ptr.Key]);
            }

            if (binary.RegistrationFunctionPointer != 0)
                if (Model.UnityVersion.CompareTo("5.3.5") >= 0)
                    Add(binary.RegistrationFunctionPointer, CppFnPtrType.FromSignature(cppTypes,
                        "void (*il2cpp_codegen_register)(const Il2CppCodeRegistration* const codeRegistration, const Il2CppMetadataRegistration* const metadataRegistration, const Il2CppCodeGenOptions* const codeGenOptions)"));
                else
                    Add(binary.RegistrationFunctionPointer, CppFnPtrType.FromSignature(cppTypes,
                        "void (*il2cpp_codegen_register)(const Il2CppCodeRegistration* const codeRegistration, const Il2CppMetadataRegistration* const metadataRegistration)"));

            // IL2CPP API exports
            // Alternative names like il2cpp_class_from_type and il2cpp_class_from_il2cpp_type may point to the same address
            foreach (var api in Model.AvailableAPIs) {
                var address = Model.AvailableAPIs.primaryToSubkeyMapping[api.Key];
                TryAdd(address, api.Value);
            }

            // Unknown functions
            // We'll skip over all the functions already added via the method iterators above,
            // leaving just the ones we haven't done any processing on
            foreach (var func in Model.Package.FunctionAddresses.Keys)
                TryAdd(func, CppFnPtrType.FromSignature(cppTypes, $"void (*unk_{func.ToAddressString()})()"));

            // Remaining exports
            var voidPtrType = cppTypes.GetType("void *");
            foreach (var export in Model.Exports)
                TryAdd(export.VirtualAddress, export);

            // Symbols
            // The symbols may also include the exports
            foreach (var symbol in Model.Symbols.Values)
                TryAdd(symbol.VirtualAddress, symbol);
        }
    }
}
