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
    // by re-arranging the fields back into their original order. This can be greatly improved and much more deeply analyzed.
    // Devs: Please don't burn development resources on obfuscation. It's a waste of your time and mine. Spend it making good games instead.
    partial class Il2CppBinary
    {
        // Loads all the pointers and counts for the specified IL2CPP metadata type regardless of version or word size into two arrays
        // Sorts the pointers and calculates the maximum number of words between each, constrained by the highest count in the IL2CPP metadata type
        // and by the end of the nearest section in the image.
        // Returns an array of the pointers in sorted order and an array of maximum word counts with corresponding indexes
        private (List<ulong> ptrs, List<int> counts) preparePointerList(Type type, ulong typePtr, IEnumerable<Section> sections) {
            // Get number of pointer/count pairs in each structure
            var itemsCount = Metadata.Sizeof(type, Image.Version, Image.Bits / 8) / (Image.Bits / 8) / 2;

            // Read pointers and counts as two lists
            var itemArray = Image.ReadMappedArray<ulong>(typePtr, itemsCount * 2);
            var itemPtrs = Enumerable.Range(0, itemArray.Length / 2).Select(i => itemArray[i*2 + 1]).ToList();
            var itemCounts = Enumerable.Range(0, itemArray.Length / 2).Select(i => (int) itemArray[i*2]).ToList();

            // Get maximum count between each pair of pointers
            // None of the maximums should be higher than the maximum count specified in the struct
            // Rule out zero pointers for no longer used fields (eg. customAttributeGenerators >=27)
            var itemMaxCount = itemCounts.Max();
            var itemPtrsOrdered = itemPtrs.Where(p => p != 0).OrderBy(p => p).ToList();
            var itemCountLimits = itemPtrsOrdered
                                    .Zip(itemPtrsOrdered.Skip(1), (a, b) => Math.Min((int) (b - a) / (Image.Bits / 8), itemMaxCount))
                                    .Append(itemMaxCount)
                                    .ToList();

            // Prevent a pointer list from overrunning the end of a section
            for (var i = 0; i < itemPtrsOrdered.Count; i++) {
                var section = sections.FirstOrDefault(s => s.VirtualStart <= itemPtrsOrdered[i] && s.VirtualEnd >= itemPtrsOrdered[i]);
                if (section != null) {
                    var maxSize = (int) (section.VirtualEnd + 1 - itemPtrsOrdered[i]) / (Image.Bits / 8);
                    itemCountLimits[i] = Math.Min(itemCountLimits[i], maxSize);
                }
            }

            return (itemPtrsOrdered, itemCountLimits);
        }

        // Reconstruct Il2CppCodeRegistration and Il2CppMetadataRegistration into their original, unobfuscated field order
        private void ReconstructMetadata(Metadata metadata) {
            // If the section table is not available, give up and do nothing
            if (!Image.TryGetSections(out var sections))
                return;

            // Get relevant image sections
            var codeSections = sections.Where(s => s.IsExec);
            var dataSections = sections.Where(s => s.IsData);

            // Fetch and sanitize our pointer and count lists
            var (codePtrsOrdered, codeCountLimits) = preparePointerList(typeof(Il2CppCodeRegistration), CodeRegistrationPointer, dataSections);
            var (metaPtrsOrdered, metaCountLimits) = preparePointerList(typeof(Il2CppMetadataRegistration), MetadataRegistrationPointer, dataSections);

            // Things we need from Il2CppCodeRegistration

            // methodPointers (<=24.1)         -> list of function pointers (1st count) (non-sequential)
            // genericMethodPointers           -> list of function pointers (first IS zero) (2nd count) (not sequential)
            // customAttributeGenerators (<27) -> list of function pointers (first MAY be zero) (2nd count) (sequential)
            // invokerPointers                 -> list of function pointers (3rd count) (sequential)
            // codeGenModules (>=24.2)         -> list of Il2CppCodeGenModule* // TODO: We only support <=24.1 currently
            // (interopData will probably have 6 sequential pointers since Il2CppInteropData starts with 5 function pointers and a GUID)

            // Let's see how many valid pointers and sequential valid pointers we actually find at each address
            // Scan each pointer address for valid list of function pointers and sort into size order
            // Consider the sequence to be broken if there is a gap over a certain threshold
            var sequenceThreshold = 0x10000;
            var fnPtrs = new SortedDictionary<ulong, int>();
            var seqFnPtrs = new SortedDictionary<ulong, int>();
            for (var i = 0; i < codePtrsOrdered.Count; i++) {

                // Non-sequential valid pointers
                var ptrs = Image.ReadMappedArray<ulong>(codePtrsOrdered[i], codeCountLimits[i]);
                var foundCount = ptrs.TakeWhile(p => codeSections.Any(s => p >= s.VirtualStart && p <= s.VirtualEnd || p == 0)).Count();

                // Prune count of trailing zero pointers
                while (foundCount > 0 && ptrs[foundCount - 1] == 0ul)
                    foundCount--;

                fnPtrs.Add(codePtrsOrdered[i], foundCount);

                // Binaries compiled with MSVC (generally PE files) use /OPT:ICF by default (enable ICF) so this won't work.
                // For these binaries, we'll use a different selection strategy below
                if (Image is PEReader)
                    continue;

                // Sequential valid pointers (a subset of non-sequential valid pointers)
                foundCount = ptrs.Take(foundCount)
                    .Zip(ptrs.Take(foundCount).Skip(1), (a, b) => (a, b))
                    .TakeWhile(t => ((long) t.b - (long) t.a >= 0 || t.b == 0)
                                    && ((long) t.b - (long) t.a < sequenceThreshold || t.a == 0)
                                    // Disallow two zero pointers in a row
                                    && (t.a != 0 || t.b != 0))
                    .Count() + 1;

                // Prune count of trailing zero pointers
                while (foundCount > 0 && ptrs[foundCount - 1] == 0ul)
                    foundCount--;

                seqFnPtrs.Add(codePtrsOrdered[i], foundCount);
            }

            KeyValuePair<ulong, int> methodPointers, genericMethodPointers, customAttributeGenerators, invokerPointers;

            // Solution without ICF
            if (!(Image is PEReader)) {
                // The two largest sequential groups are customAttributeGenerators and invokerPointers
                var seqLongest = seqFnPtrs.OrderByDescending(kv => kv.Value).Take(2).ToList();
                (customAttributeGenerators, invokerPointers) = (seqLongest[0], seqLongest[1]);

                // For >=27, customAttributeGenerators is zero and so the largest group is invokerPointers
                if (Image.Version >= 27) {
                    invokerPointers = customAttributeGenerators;
                    customAttributeGenerators = new KeyValuePair<ulong, int>(0ul, 0);
                }

                // After removing these from the non-sequential list, the largest groups are methodPointers and genericMethodPointers
                var longest = fnPtrs.Except(seqLongest).OrderByDescending(kv => kv.Value).Take(2).ToList();
                (methodPointers, genericMethodPointers) = (longest[0], longest[1]);

                // For >=24.2, methodPointers is zero and so the largest group is genericMethodPointers
                if (Image.Version >= 24.2) {
                    genericMethodPointers = methodPointers;
                    methodPointers = new KeyValuePair<ulong, int>(0ul, 0);
                }

                // Prune genericMethodPointers at 2nd zero (first pointer is always zero)
                var gmPtr = Image.ReadMappedArray<ulong>(genericMethodPointers.Key, genericMethodPointers.Value);
                var gmZero = Array.IndexOf(gmPtr, 0ul, 1);
                if (gmZero != -1)
                    genericMethodPointers = new KeyValuePair<ulong, int>(genericMethodPointers.Key, gmZero);
            }

            // Solution with ICF
            else {
                // Take and remove the first item and assume it's methodPointers for <=24.1, otherwise set to zero
                var orderedPtrs = fnPtrs.OrderByDescending(kv => kv.Value).ToList();
                if (Image.Version <= 24.1) {
                    methodPointers = orderedPtrs[0];
                    orderedPtrs.RemoveAt(0);
                } else
                    methodPointers = new KeyValuePair<ulong, int>(0ul, 0);

                // Assume this order is right most of the time
                // TODO: generic and custom attribute might be the wrong way round (eg. #102)
                (genericMethodPointers, customAttributeGenerators, invokerPointers) = (orderedPtrs[0], orderedPtrs[1], orderedPtrs[2]);

                // customAttributeGenerators is removed in metadata >=27
                if (Image.Version >= 27) {
                    invokerPointers = customAttributeGenerators;
                    customAttributeGenerators = new KeyValuePair<ulong, int>(0ul, 0);
                }
            }

            #region Debugging validation checks
            #if DEBUG
            // Used on non-obfuscated binaries during development to confirm the output is correct
            if (methodPointers.Key != CodeRegistration.pmethodPointers)
                throw new Exception("Method Pointers incorrect");
            if (invokerPointers.Key != CodeRegistration.invokerPointers)
                throw new Exception("Invoker Pointers incorrect");
            if (customAttributeGenerators.Key != CodeRegistration.customAttributeGenerators)
                throw new Exception("Custom attribute generators incorrect");
            if (genericMethodPointers.Key != CodeRegistration.genericMethodPointers)
                throw new Exception("Generic method pointers incorrect");

            if (methodPointers.Value != (int) CodeRegistration.methodPointersCount)
                throw new Exception("Count of Method Pointers incorrect");
            if (invokerPointers.Value != (int) CodeRegistration.invokerPointersCount)
                throw new Exception("Count of Invoker Pointers incorrect");
            if (customAttributeGenerators.Value != (int) CodeRegistration.customAttributeCount)
                throw new Exception("Count of Custom attribute generators incorrect");
            if (genericMethodPointers.Value != (int) CodeRegistration.genericMethodPointersCount)
                throw new Exception("Count of Generic method pointers incorrect");
            #endif
            #endregion

            // Perform substitution
            CodeRegistration.genericMethodPointers      = genericMethodPointers.Key;
            CodeRegistration.genericMethodPointersCount = (ulong) genericMethodPointers.Value;
            CodeRegistration.customAttributeGenerators  = customAttributeGenerators.Key;
            CodeRegistration.customAttributeCount       = customAttributeGenerators.Value;
            CodeRegistration.invokerPointers            = invokerPointers.Key;
            CodeRegistration.invokerPointersCount       = (ulong) invokerPointers.Value;
            CodeRegistration.pmethodPointers            = methodPointers.Key;
            CodeRegistration.methodPointersCount        = (ulong) methodPointers.Value;

            // Force CodeRegistration to pass validation in Il2CppBinary.Configure()
            CodeRegistration.reversePInvokeWrapperCount = 0;
            CodeRegistration.unresolvedVirtualCallCount = 0;
            CodeRegistration.interopDataCount           = 0;

            // Things we need from Il2CppMetadataRegistration

            // genericInsts               -> list of Il2CppGenericInst* (argc is count of Il2CppType* at data pointer argv; datapoint = GenericParameterIndex)
            // genericMethodTable         -> list of Il2CppGenericMethodFunctionsDefinitions (genericMethodIndex, methodIndex, invokerIndex)
            // types                      -> list of Il2CppType*
            // methodSpecs                -> list of Il2CppMethodSpec
            // methodReferences (<=16)    -> list of uint
            // fieldOffsets (fieldOffsetsArePointers) -> either a list of data pointers (some zero, some VAs not mappable) to list of uints, or a list of uints
            // metadataUsages (>=19, <27) -> list of unmappable data pointers

            // We can only perform this re-ordering if we can refer to a loaded global-metadata.dat
            if (metadata == null)
                return;

            (ulong ptr, int count) types = (0, 0);

            // Determine what each pointer is
            for (var i = 0; i < metaPtrsOrdered.Count; i++) {

                // Pointers in this list
                var ptrs = Image.ReadMappedArray<ulong>(metaPtrsOrdered[i], metaCountLimits[i]);

                // foundCount and foundMappableCount will generally be the same below
                // except in PE files where data and bss can overlap in our interpretation of the sections

                // First set of pointers that point to a data section virtual address
                var foundCount = ptrs.TakeWhile(p => dataSections.Any(s => p >= s.VirtualStart && p <= s.VirtualEnd)).Count();

                // First set of pointers that can be mapped anywhere into the image
                var foundMappableCount = ptrs.TakeWhile(p => Image.TryMapVATR(p, out _)).Count();

                // First set of pointers that can be mapped into a data section in the image
                var ptrsMappableData = ptrs.Take(foundMappableCount)
                    .TakeWhile(p => dataSections.Any(s => Image.MapVATR(p) >= s.ImageStart && Image.MapVATR(p) <= s.ImageEnd))
                    .ToList();

                var foundMappableDataCount = ptrsMappableData.Count;

                // Test for Il2CppType**
                // We don't ever expect there to be less than 0x1000 types
                if (foundMappableDataCount >= 0x1000) {
                    // This statement is quite slow. We could speed it up with a two-stage approach
                    var testTypes = Image.ReadMappedObjectPointerArray<Il2CppType>(metaPtrsOrdered[i], foundMappableDataCount);

                    var foundTypes = 0;
                    foreach (var testType in testTypes) {
                        // TODO: v27 will fail this because of the bit shifting in Il2CppType.bits
                        if (testType.num_mods != 0)
                            break;
                        if (!Enum.IsDefined(typeof(Il2CppTypeEnum), testType.type))
                            break;
                        if (testType.type == Il2CppTypeEnum.IL2CPP_TYPE_END)
                            break;

                        // Test datapoint
                        if (testType.type switch {
                            var t when (t is Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE || t is Il2CppTypeEnum.IL2CPP_TYPE_CLASS)
                                        && testType.datapoint >= (ulong) metadata.Types.Length => false,

                            var t when (t is Il2CppTypeEnum.IL2CPP_TYPE_VAR || t is Il2CppTypeEnum.IL2CPP_TYPE_MVAR)
                                        && testType.datapoint >= (ulong) metadata.GenericParameters.Length => false,

                            var t when (t is Il2CppTypeEnum.IL2CPP_TYPE_PTR || t is Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY)
                                        && !ptrsMappableData.Contains(testType.datapoint) => false,

                            // Untested cases, we could add more here (IL2CPP_TYPE_ARRAY, IL2CPP_TYPE_GENERICINST)
                            _ => true
                        })
                            foundTypes++;
                        else
                            break;
                    }
                    if (foundTypes >= 0x1000) {
                        types = (metaPtrsOrdered[i], foundTypes);
                    }
                }
            }

            #region Debugging validation checks
            #if DEBUG
            // Used on non-obfuscated binaries during development to confirm the output is correct
            if (types.ptr != MetadataRegistration.ptypes)
                throw new Exception("Il2CppType** incorrect");

            if (types.count != MetadataRegistration.typesCount)
                throw new Exception("Count of Il2CppType* incorrect");
            #endif
            #endregion

            // Perform substitution
            MetadataRegistration.ptypes                    = types.ptr;
            MetadataRegistration.typesCount                = types.count;

            // Force MetadataRegistration to pass validation in Il2CppBinary.Configure()
            MetadataRegistration.typeDefinitionsSizesCount = 0;
            MetadataRegistration.genericClassesCount       = MetadataRegistration.genericInstsCount + 1;
            MetadataRegistration.genericMethodTableCount   = MetadataRegistration.genericInstsCount + 1;
        }
    }
}