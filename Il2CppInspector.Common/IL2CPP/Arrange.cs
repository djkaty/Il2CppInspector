/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Il2CppInspector
{
    // Some IL2CPP applications obfuscate the order of fields in Il2CppCodeRegistration and Il2CppMetadataRegistration
    // specifically to defeat IL2CPP reverse engineering tools. We make an imperfect attempt to defeat this below
    // by re-arranging the fields back into their original order. This can be greatly improved and more deeply validated.
    // Devs: Please don't burn development resources on obfuscation. It's a waste of your time and mine. Spend it making good games instead.
    partial class Il2CppBinary
    {
        private void Arrange() {
            // Get number of pointer/count pairs in each structure
            var codeItemsCount = Metadata.Sizeof(typeof(Il2CppCodeRegistration), Image.Version, Image.Bits / 8) / (Image.Bits / 8) / 2;
            var metaItemsCount = Metadata.Sizeof(typeof(Il2CppMetadataRegistration), Image.Version, Image.Bits / 8) / (Image.Bits / 8) / 2;

            // Read as list of tuples
            var codeArray = Image.ReadMappedArray<ulong>(CodeRegistrationPointer, codeItemsCount * 2);
            var codeItems = Enumerable.Range(0, codeArray.Length / 2).Select(i => (Pointer: codeArray[i*2 + 1], Count: codeArray[i*2]));

            var metaArray = Image.ReadMappedArray<ulong>(MetadataRegistrationPointer, metaItemsCount * 2);
            var metaItems = Enumerable.Range(0, metaArray.Length / 2).Select(i => (Pointer: metaArray[i*2 + 1], Count: metaArray[i*2]));

            // Things we need

            // Il2CppCodeRegistration:
            // methodPointers (<=24.1)    -> list of function pointers
            // genericMethodPointers      -> list of function pointers (first is zero)
            // invokerPointers            -> list of function pointers
            // customAttributeGenerators  -> list of function pointers
            // codeGenModules (>=24.2)    -> list of Il2CppCodeGenModule*

            if (CodeRegistration.methodPointersCount <= CodeRegistration.genericMethodPointersCount && Image.Version <= 24.1)
                throw new Exception("Generic pointers greater than method pointers");
            if (CodeRegistration.genericMethodPointersCount <= CodeRegistration.invokerPointersCount)
                throw new Exception("Invoker pointers greater than generic pointers");

            // Seems to always be true but I'm not sure we can realistically guarantee this
            if (CodeRegistration.customAttributeGenerators < CodeRegistration.invokerPointersCount && Image.Version <= 24.1)
                throw new Exception("Custom attribute generators less than invoker pointers");

            // Il2CppMetadataRegistration:
            // genericInsts               -> list of Il2CppGenericInst* (argc is count of Il2CppType* at data pointer argv; datapoint = GenericParameterIndex)
            // genericMethodTable         -> list of Il2CppGenericMethodFunctionsDefinitions (genericMethodIndex, methodIndex, invokerIndex)
            // types                      -> list of Il2CppType*
            // methodSpecs                -> list of Il2CppMethodSpec
            // methodReferences (<=16)    -> list of uint
            // fieldOffsets (fieldOffsetsArePointers) -> either a list of data pointers (some zero, some VAs not mappable) to list of uints, or a list of uints
            // metadataUsages (>=19, <27) -> list of unmappable data pointers
        }
    }
}
