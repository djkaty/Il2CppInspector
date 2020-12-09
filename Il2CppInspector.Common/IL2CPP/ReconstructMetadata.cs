/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Il2CppInspector
{
    // Some IL2CPP applications obfuscate the order of fields in Il2CppCodeRegistration and Il2CppMetadataRegistration
    // specifically to defeat IL2CPP reverse engineering tools. We make an imperfect attempt to defeat this below
    // by re-arranging the fields back into their original order. This can be greatly improved and much more deeply analyzed.
    // Devs: Please don't burn development resources on obfuscation. It's a waste of your time and mine. Spend it making good games instead.
    partial class Il2CppBinary
    {
        // Loads all the pointers and counts for the specified IL2CPP metadata type regardless of version or word size into two arrays
        // Sorts the pointers and calculates the maximum number of bytes between each,
        // optinally constrained by the highest count in the IL2CPP metadata type and by the end of the nearest section in the image.
        // Returns an array of the pointers in sorted order and an array of maximum byte/word counts with corresponding indexes
        private (List<ulong> ptrs, List<int> limits, List<int> originalCounts)
            preparePointerList(Type type, ulong typePtr, IEnumerable<Section> sections, bool itemsAreWords = false) {

            // Get number of pointer/count pairs in each structure
            var itemsCount = Metadata.Sizeof(type, Image.Version, Image.Bits / 8) / (Image.Bits / 8) / 2;

            // Read pointers and counts as two lists
            var itemArray = Image.ReadMappedArray<ulong>(typePtr, itemsCount * 2);
            var itemPtrs = Enumerable.Range(0, itemArray.Length / 2).Select(i => itemArray[i*2 + 1]).ToList();
            var itemCounts = Enumerable.Range(0, itemArray.Length / 2).Select(i => (int) itemArray[i*2]).ToList();

            // Get maximum number of bytes/count between each pair of pointers
            // None of the maximums should be higher than the maximum count specified in the struct
            // Rule out zero pointers for no longer used fields (eg. customAttributeGenerators >=27)
            var itemMaxBytes = itemsAreWords? itemCounts.Max() * (Image.Bits / 8) : int.MaxValue;
            var itemPtrsOrdered = itemPtrs.Where(p => p != 0).OrderBy(p => p).ToList();
            var itemCountLimits = itemPtrsOrdered
                                    .Zip(itemPtrsOrdered.Skip(1), (a, b) => Math.Min((int) (b - a), itemMaxBytes))
                                    .Append(itemMaxBytes)
                                    .ToList();

            // Prevent a pointer list from overrunning the end of a section
            for (var i = 0; i < itemPtrsOrdered.Count; i++) {
                var section = sections.FirstOrDefault(s => s.VirtualStart <= itemPtrsOrdered[i] && s.VirtualEnd >= itemPtrsOrdered[i]);
                if (section != null) {
                    var maxSize = (int) (section.VirtualEnd + 1 - itemPtrsOrdered[i]);
                    itemCountLimits[i] = Math.Min(itemCountLimits[i], maxSize);
                }
            }

            // Convert byte sizes to words if applicable
            if (itemsAreWords)
                itemCountLimits = itemCountLimits.Select(i => i / (Image.Bits / 8)).ToList();

            return (itemPtrsOrdered, itemCountLimits, itemCounts);
        }

        // Reconstruct Il2CppCodeRegistration and Il2CppMetadataRegistration into their original, unobfuscated field order
        // Supports metadata >=19, <27
        private void ReconstructMetadata(Metadata metadata) {
            // If the section table is not available, give up and do nothing
            if (!Image.TryGetSections(out var sections))
                return;

            // Get relevant image sections
            var codeSections = sections.Where(s => s.IsExec).ToList();
            var dataSections = sections.Where(s => s.IsData && !s.IsBSS).ToList();
            var bssSections  = sections.Where(s => s.IsBSS).ToList();

            // For PE files, data sections in memory can be larger than in the image
            // The unmapped portion is BSS data so create fake sections for this and shrink the existing ones
            foreach (var section in dataSections) {
                var virtualSize = section.VirtualEnd - section.VirtualStart;
                var imageSize   = section.ImageEnd   - section.ImageStart;

                if (imageSize < virtualSize) {
                    var bssEnd = section.VirtualEnd;
                    section.VirtualEnd = section.VirtualStart + imageSize;

                    bssSections.Add(new Section {
                        VirtualStart = section.VirtualStart + imageSize + 1,
                        VirtualEnd   = bssEnd,

                        ImageStart   = 0,
                        ImageEnd     = 0,

                        IsExec       = false,
                        IsData       = false,
                        IsBSS        = true,

                        Name         = ".bss"
                    });
                }
            }

            // Fetch and sanitize our pointer and count lists
            var (codePtrsOrdered, codeCountLimits, codeCounts) = preparePointerList(typeof(Il2CppCodeRegistration), CodeRegistrationPointer, dataSections, true);
            var (metaPtrsOrdered, metaCountLimits, metaCounts) = preparePointerList(typeof(Il2CppMetadataRegistration), MetadataRegistrationPointer, dataSections);

            // Progress updater
            var maxProgress = codeCounts.Sum() + metaCounts.Sum();
            var currentProgress = 0;

            void UpdateProgress(int workDone) {
                currentProgress += workDone;
                StatusUpdate($"Reconstructing obfuscated registration metadata ({currentProgress * 100 / maxProgress:F0}%)");
            }

            Console.WriteLine("Reconstructing obfuscated registration metadata...");
            UpdateProgress(0);

            // Counts from minimal compiles

            // v21 test project:
            // genericMethodPointers - 0x07B5, customAttributeGenerators - 0x0747, invokerPointers - 0x04DB, methodPointers - 0x226A
            // v24.1 empty Unity project:
            // genericMethodPointers - 0x0C15, customAttributeGenerators - 0x0A21, invokerPointers - 0x0646, methodPointers - 0x268B
            // v24.2 without Unity:
            // genericMethodPointers - 0x2EC2, customAttributeGenerators - 0x15EC, invokerPointers - 0x0B65

            // v21 test project:
            // genericInsts - 0x0150, genericMethodTable - 0x0805, types - 0x1509, methodSpecs - 0x08D8, fieldOffsets - 0x0569, metadataUsages - 0x1370
            // v24.1 empty Unity project:
            // genericInsts - 0x025E, genericMethodTable - 0x0E3F, types - 0x2632, methodSpecs - 0x0FD4, fieldOffsets - 0x0839, metadataUsages - 0x1850
            // v24.2 without Unity:
            // genericInsts - 0x06D4, genericMethodTable - 0x31E8, types - 0x318A, methodSpecs - 0x3AD8, fieldOffsets - 0x0B3D, metadataUsages - 0x3BA8

            // Some heuristic constants

            // The maximum address gap in a sequential list of pointers before the sequence is considered to be 'broken'
            const int MAX_SEQUENCE_GAP = 0x10000;

            // The minimum number of Il2CppTypes we expect
            const int MIN_TYPES = 0x1400;

            // The maximum number of generic type parameters we expect for any class or method
            const int MAX_GENERIC_TYPE_PARAMETERS = 32;

            // The minimum number of Il2CppGenericInsts we expect
            const int MIN_GENERIC_INSTANCES = 0x140;

            // The maximum number of generic methods in generic classes we expect to find in a single sequence of Il2CppMethodSpec
            // The highest we have seen in a production app is 3414; the next highest 2013, the next highest 1380
            // 300-600 is typical
            const int MAX_SEQUENTIAL_GENERIC_CLASS_METHODS = 5000;

            // The minimum number of Il2CppMethodSpecs we expect
            const int MIN_METHOD_SPECS = 0x0800;

            // The minimum number of Il2CppGenericMethodFunctionsDefinitions we expect
            const int MIN_GENERIC_METHOD_TABLE = 0x600;

            // The minimum spread of values in an instance of Il2CppGenericMethodFunctionsDefinitions under which the threshold counter is incremented
            const int MIN_GENERIC_METHOD_TABLE_SPREAD = 100;

            // The maximum number of instances in a row with a spread under the minimum we expect to find in a single sequence
            const int MAX_SEQUENTIAL_GENERIC_METHOD_TABLE_LOW_SPREAD_INSTANCES = 100;

            // The minimum number of field offsets we expect
            const int MIN_FIELD_OFFSETS = 0x400;

            // The maximum value for a field offset
            const int MAX_FIELD_OFFSET = 0x100000;

            // The minimum and maximum proportions of inversions we expect in a non-pointer field offset list
            // Example values: 0.385, 0.415, 0.468
            const double MIN_FIELD_OFFSET_INVERSION = 0.3;
            const double MAX_FIELD_OFFSET_INVERSION = 0.6;

            // The minimum and maximum proportions of zeroes we expect in a non-pointer field offset list
            // Example values: 0.116, 0.179, 0.303, 0.321, 0.385
            // The main thing is to force enough zeroes to prevent it being confused with a list with no zeroes (eg. genericClasses)
            const double MIN_FIELD_OFFSET_ZEROES = 0.10;
            const double MAX_FIELD_OFFSET_ZEROES = 0.5;

            // The maximum allowed gap between two field offsets
            const int MAX_FIELD_OFFSET_GAP = 0x10000;

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
                if (foundCount > 0) {
                    foundCount = ptrs.Take(foundCount)
                        .Zip(ptrs.Take(foundCount).Skip(1), (a, b) => (a, b))
                        .TakeWhile(t => ((long) t.b - (long) t.a >= 0 || t.b == 0)
                                        && ((long) t.b - (long) t.a < MAX_SEQUENCE_GAP || t.a == 0)
                                        // Disallow two zero pointers in a row
                                        && (t.a != 0 || t.b != 0))
                        .Count() + 1;

                    // Prune count of trailing zero pointers
                    while (foundCount > 0 && ptrs[foundCount - 1] == 0ul)
                        foundCount--;
                }

                seqFnPtrs.Add(codePtrsOrdered[i], foundCount);

                UpdateProgress(foundCount);
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
            #if false
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

            // TODO: Write changes to stream
            isModified = true;

            // Things we need from Il2CppMetadataRegistration

            // genericInsts               -> list of Il2CppGenericInst* (argc is count of Il2CppType* at data pointer argv; datapoint = GenericParameterIndex)
            // genericMethodTable         -> list of Il2CppGenericMethodFunctionsDefinitions (genericMethodIndex, methodIndex, invokerIndex)
            // types                      -> list of Il2CppType*
            // methodSpecs                -> list of Il2CppMethodSpec
            // methodReferences (<=16)    -> list of uint (ignored, we don't support <=16 here)
            // fieldOffsets (fieldOffsetsArePointers) -> either a list of data pointers (some zero, some VAs not mappable) to list of uints, or a list of uints
            // metadataUsages (>=19, <27) -> list of unmappable data pointers

            // We can only perform this re-ordering if we can refer to a loaded global-metadata.dat
            if (metadata == null)
                return;

            // Read in all the required data once since we'll be using nested loops
            var metaPtrData = new List<(ulong, int, ulong[], int)>();

            for (var i = 0; i < metaPtrsOrdered.Count; i++) {
                // Pointers in this list
                var ptrs = Image.ReadMappedArray<ulong>(metaPtrsOrdered[i], metaCountLimits[i] / (Image.Bits / 8));

                // First set of pointers that point to a data section virtual address
                var foundDataPtrsCount = ptrs.TakeWhile(p => dataSections.Any(s => p >= s.VirtualStart && p <= s.VirtualEnd)).Count();

                // First set of pointers that can be mapped anywhere into the image
                var foundImageMappablePtrsCount = ptrs.TakeWhile(p => Image.TryMapVATR(p, out _)).Count();
#if DEBUG
                // First set of pointers that can be mapped into a data section in the image
                var mappableDataPtrs = ptrs.Take(foundImageMappablePtrsCount)
                        .TakeWhile(p => dataSections.Any(s => Image.MapVATR(p) >= s.ImageStart && Image.MapVATR(p) <= s.ImageEnd))
                        .ToArray();

                var foundNonBSSDataPtrsCount = mappableDataPtrs.Length;

                if (foundDataPtrsCount != foundNonBSSDataPtrsCount)
                    throw new Exception($"Pointer count mismatch: {foundDataPtrsCount:x8} / {foundNonBSSDataPtrsCount:x8}");
#endif
                metaPtrData.Add((metaPtrsOrdered[i], metaCountLimits[i], ptrs, foundDataPtrsCount));
            }

            // Items we need to search for
            var types              = (ptr: 0ul, count: -1);
            var genericInsts       = (ptr: 0ul, count: -1);
            var methodSpecs        = (ptr: 0ul, count: -1);
            var genericMethodTable = (ptr: 0ul, count: -1);
            var metadataUsages     = (ptr: 0ul, count: -1);
            var fieldOffsets       = (ptr: 0ul, count: -1);

            var NOT_FOUND          = (ptr: 0xfffffffful, count: -1);

            // Intermediary items
            var typesPtrs = new List<ulong>();
            Il2CppMethodSpec[] methodSpec = null;

            // IL2CPP doesn't calculate metadataUsagesCount correctly so we do it here
            // Adapted from Il2CppInspector.buildMetadataUsages()
            var usages = new HashSet<uint>();

            // Only on supported metadata versions (>=19, <27)
            if (metadata.MetadataUsageLists != null)
                foreach (var metadataUsageList in metadata.MetadataUsageLists)
                    for (var i = 0; i < metadataUsageList.count; i++)
                        usages.Add(metadata.MetadataUsagePairs[metadataUsageList.start + i].destinationindex);

            // Determine what each pointer is
            // We need to do this in a certain order because validating some items relies on earlier items
            while (metaPtrData.Any()) {

                ref var foundItem = ref NOT_FOUND;
                (ulong ptr, int count) foundData = (0ul, -1);

                // We loop repeatedly through every set of data looking for our next target item,
                // remove the matching set from the list and then repeat the outer while loop
                // until there is nothing left to find
                foreach (var (ptr, limit, ptrs, dataPtrsCount) in metaPtrData) {

                    foundData = (ptr, 0);

                    // Test for Il2CppType**
                    // ---------------------
                    if (types.count == -1) {

                        // We don't ever expect there to be less than MIN_TYPES types
                        if (dataPtrsCount >= MIN_TYPES) {

                            var testItems = Image.ReadMappedObjectPointerArray<Il2CppType>(ptr, dataPtrsCount);

                            foreach (var item in testItems) {
                                // TODO: v27 will fail this because of the bit shifting in Il2CppType.bits
                                if (item.num_mods != 0)
                                    break;
                                if (!Enum.IsDefined(typeof(Il2CppTypeEnum), item.type))
                                    break;
                                if (item.type == Il2CppTypeEnum.IL2CPP_TYPE_END)
                                    break;

                                // Test datapoint
                                if (item.type switch {
                                    var t when (t is Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE || t is Il2CppTypeEnum.IL2CPP_TYPE_CLASS)
                                                && item.datapoint >= (ulong) metadata.Types.Length => false,

                                    var t when (t is Il2CppTypeEnum.IL2CPP_TYPE_VAR || t is Il2CppTypeEnum.IL2CPP_TYPE_MVAR)
                                                && item.datapoint >= (ulong) metadata.GenericParameters.Length => false,

                                    var t when (t is Il2CppTypeEnum.IL2CPP_TYPE_PTR || t is Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY)
                                                && !ptrs.Take(dataPtrsCount).Contains(item.datapoint) => false,

                                    // Untested cases, we could add more here (IL2CPP_TYPE_ARRAY, IL2CPP_TYPE_GENERICINST)
                                    _ => true
                                })
                                    foundData.count++;
                                else
                                    break;
                            }

                            if (foundData.count >= MIN_TYPES) {
                                foundItem = ref types;
                                typesPtrs = ptrs.ToList();
                                break;
                            }
                        }
                    }

                    // Test for Il2CppGenericInst**
                    // ----------------------------
                    else if (genericInsts.count == -1) {

                        if (dataPtrsCount >= MIN_GENERIC_INSTANCES) {

                            var testItems = Image.ReadMappedObjectPointerArray<Il2CppGenericInst>(ptr, dataPtrsCount);

                            foreach (var item in testItems) {
                                // Let's pray no generic type has more than this many type parameters
                                if (item.type_argc > MAX_GENERIC_TYPE_PARAMETERS)
                                    break;

                                // All the generic type paramters must be in the total list of types,
                                // ie. typePtrs must be a subset of typesData.Keys
                                try {
                                    var typePtrs = Image.ReadMappedArray<ulong>(item.type_argv, (int) item.type_argc);
                                    if (typePtrs.Any(p => !typePtrs.Contains(p)))
                                        break;
                                    // Pointers were invalid
                                }
                                catch (InvalidOperationException) {
                                    break;
                                }

                                foundData.count++;
                            }

                            if (foundData.count >= MIN_GENERIC_INSTANCES) {
                                foundItem = ref genericInsts;
                                break;
                            }
                        }
                    }

                    // Test for Il2CppMethodSpec*
                    // --------------------------
                    else if (methodSpecs.count == -1) {
                        var max = limit / metadata.Sizeof(typeof(Il2CppMethodSpec));

                        if (max >= MIN_METHOD_SPECS) {

                            var testItems = Image.ReadMappedArray<Il2CppMethodSpec>(ptr, max);
                            var nonNegativePairs = 0;

                            foreach (var item in testItems) {
                                if (item.methodDefinitionIndex < 0 || item.methodDefinitionIndex >= metadata.Methods.Length)
                                    break;
                                if (item.classIndexIndex < -1 || item.classIndexIndex >= genericInsts.count)
                                    break;
                                if (item.methodIndexIndex < -1 || item.methodIndexIndex >= genericInsts.count)
                                    break;

                                // Non-negative pairs shouldn't appear in large groups
                                nonNegativePairs = item.classIndexIndex != -1 && item.methodIndexIndex != -1 ? nonNegativePairs + 1 : 0;
                                if (nonNegativePairs > MAX_SEQUENTIAL_GENERIC_CLASS_METHODS)
                                    break;

                                foundData.count++;
                            }

                            // Assumes last methods are not generic methods in generic classes
                            foundData.count -= nonNegativePairs;

                            if (foundData.count >= MIN_METHOD_SPECS) {
                                foundItem = ref methodSpecs;
                                methodSpec = testItems;
                                break;
                            }
                        }
                    }

                    // Test for Il2CppGenericMethodFunctionsDefinitions*
                    // -------------------------------------------------
                    else if (genericMethodTable.count == -1) {
                        var max = limit / metadata.Sizeof(typeof(Il2CppGenericMethodFunctionsDefinitions));

                        if (max >= MIN_GENERIC_METHOD_TABLE) {

                            var testItems = Image.ReadMappedArray<Il2CppGenericMethodFunctionsDefinitions>(ptr, max);
                            var lowSpreadCount = 0;

                            foreach (var item in testItems) {
                                if (item.genericMethodIndex < 0 || item.genericMethodIndex >= methodSpecs.count)
                                    break;
                                if (item.indices.methodIndex < 0 || item.indices.methodIndex >= genericMethodPointers.Value)
                                    break;
                                if (item.indices.invokerIndex < 0 || item.indices.invokerIndex >= invokerPointers.Value)
                                    break;
                                // methodIndex is an index into the method pointer table
                                // For generic type definitions, there is no concrete function so this must be 0xffffffff
                                // TODO: For >=24.2, we need to use the method token to look up the value in an Il2CppCodeGenModule, not currently implemented
                                if (Image.Version <= 24.1)
                                    if (metadata.Methods[methodSpec[item.genericMethodIndex].methodDefinitionIndex].methodIndex != -1)
                                        break;
                                foundData.count++;

                                // Instances where all the values are clustered should be rare
                                var spread = Math.Max(Math.Max(item.indices.methodIndex, item.indices.invokerIndex), item.genericMethodIndex)
                                           - Math.Min(Math.Min(item.indices.methodIndex, item.indices.invokerIndex), item.genericMethodIndex);
                                
                                lowSpreadCount = spread < MIN_GENERIC_METHOD_TABLE_SPREAD ? lowSpreadCount + 1 : 0;
                                if (lowSpreadCount > MAX_SEQUENTIAL_GENERIC_METHOD_TABLE_LOW_SPREAD_INSTANCES)
                                    break;
                            }

                            // Assumes the last instances don't have clustered values
                            foundData.count -= lowSpreadCount;

                            if (foundData.count >= MIN_GENERIC_METHOD_TABLE) {
                                foundItem = ref genericMethodTable;
                                break;
                            }
                        }
                    }

                    // Test for metadata usages
                    // ------------------------
                    else if (metadataUsages.count == -1) {

                        // No metadata usages for these versions
                        if (Image.Version < 19 || Image.Version >= 27) {
                            foundData.ptr = 0ul;
                            foundItem = ref metadataUsages;
                            break;
                        }

                        // Metadata usages always map to BSS sections and has only a maximum of a small number of null pointers
                        if (dataPtrsCount == 0 && limit / (Image.Bits / 8) >= usages.Count) {

                            // No null pointers allowed
                            if (ptrs.Take(usages.Count).All(p => p != 0ul)) {

                                // All the pointers must map to a BSS section
                                // For PE files this relies on our section modding above
                                var bssMappableCount = ptrs.Take(usages.Count).Count(p => bssSections.Any(s => p >= s.VirtualStart && p <= s.VirtualEnd));

                                foundData.count = bssMappableCount;
                                foundItem = ref metadataUsages;
                                break;
                            }
                        }
                    }

                    // Test for field offsets
                    // ----------------------
                    else if (fieldOffsets.count == -1) {
                        // This could be a list of pointers to locally incrementing sequences of uints,
                        // or it could be a sequence of uints

                        // Some uints may be zero, but must otherwise never be less than the minimum heap offset of the first parameter
                        // for the binary's function calling convention, and never greater than the maximum heap offset of the last parameter

                        // Try as sequence of uints
                        if (metadata.Version <= 21) {
                            var max = limit / sizeof(uint);

                            if (max >= MIN_FIELD_OFFSETS) {

                                var testItems = Image.ReadMappedArray<uint>(ptr, max);
                                var previousItem = 0u;
                                var inversions = 0;
                                var zeroes = 0;

                                foreach (var item in testItems) {
                                    if (item > MAX_FIELD_OFFSET && item != 0xffffffff)
                                        break;
                                    if (item > previousItem + MAX_FIELD_OFFSET_GAP && item != 0xffffffff)
                                        break;
                                    // Count zeroes and inversions (equality counts as inversion here since two arguments can't share a heap offset)
                                    if (item <= previousItem)
                                        inversions++;
                                    if (item == 0)
                                        zeroes++;
                                    previousItem = item;

                                    foundData.count++;
                                }

                                if (foundData.count >= MIN_FIELD_OFFSETS) {
                                    var inversionsPc = (double) inversions / foundData.count;
                                    var zeroesPc = (double) zeroes / foundData.count;

                                    if (inversionsPc >= MIN_FIELD_OFFSET_INVERSION && inversionsPc <= MAX_FIELD_OFFSET_INVERSION
                                        && zeroesPc >= MIN_FIELD_OFFSET_ZEROES && zeroesPc <= MAX_FIELD_OFFSET_ZEROES) {
                                        foundItem = ref fieldOffsets;
                                        break;
                                    }
                                }
                            }
                        }

                        // Try as a sequence of pointers to sets of uints
                        if (metadata.Version >= 21) {
                            foundData.count = 0;
                            var max = limit / (Image.Bits / 8);

                            if (max >= MIN_FIELD_OFFSETS) {

                                var testItems = Image.ReadMappedArray<ulong>(ptr, max);
                                var zeroes = 0;

                                foreach (var item in testItems) {
                                    // Every pointer must either be zero or mappable into a data or BSS section
                                    if (item != 0ul && !dataSections.Any(s => item >= s.VirtualStart && item < s.VirtualEnd)
                                                    && !bssSections.Any(s => item >= s.VirtualStart && item < s.VirtualEnd))
                                        break;

                                    // Count zeroes
                                    if (item == 0ul)
                                        zeroes++;

                                    // Every valid pointer must point to a series of incrementing offsets until an inversion or a large gap
                                    else if (dataSections.Any(s => item >= s.VirtualStart && item < s.VirtualEnd)) {
                                        Image.Position = Image.MapVATR(item);
                                        var previous = 0u;
                                        var offset = 0u;
                                        var valid = true;
                                        while (offset != 0xffffffff && offset > previous && valid) {
                                            previous = offset;
                                            offset = Image.ReadUInt32();
                                            // Consider a large gap as the end of the sequence
                                            if (offset > previous + MAX_FIELD_OFFSET_GAP && offset != 0xffffffff)
                                                break;
                                            // A few offsets seem to have the top bit set for some reason
                                            if (offset >= previous && (offset & 0x7fffffff) > MAX_FIELD_OFFSET && offset != 0xffffffff)
                                                valid = false;
                                        }
                                        if (!valid)
                                            break;
                                    }

                                    foundData.count++;
                                }

                                if (foundData.count >= MIN_FIELD_OFFSETS) {
                                    var zeroesPc = (double) zeroes / foundData.count;

                                    if (zeroesPc >= MIN_FIELD_OFFSET_ZEROES && zeroesPc <= MAX_FIELD_OFFSET_ZEROES) {
                                        foundItem = ref fieldOffsets;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    foundData = (0ul, -1);
                }

                // We didn't find anything - break to avoid an infinite loop
                if (foundItem == NOT_FOUND)
                    break;

                // Remove pointer from list of remaining pointers to test
                metaPtrData = metaPtrData.Where(m => foundData.ptr != m.Item1).ToList();

                // Select the highest count in the original data that is lower or equal to our found count
                // Skip items not implemented by the specific metadata version we are analyzing (ptr == 0, count == 0)
                // Skip metadataUsages because it is calculated incorrectly by IL2CPP and the count is wrong
                if (foundData.count > 0 && foundData.count != usages.Count) {
                    // Aggregate uses the first value for 'next' as the seed for 'nearest' unless we specify a starting seed
                    foundData.count = metaCounts.Aggregate(0, (nearest, next) => next - foundData.count > nearest - foundData.count && next - foundData.count <= 0? next : nearest);
                    metaCounts = metaCounts.Where(c => c != foundData.count).ToList();
                }

                // Set item via ref
                foundItem = foundData;

                // If we just found the Il2CppTypes data, prune the pointer list to the correct length
                if (foundItem == types && typesPtrs.Count != foundData.count)
                    typesPtrs = typesPtrs.Take(foundData.count).ToList();

                UpdateProgress(foundData.count);
            }

            #region Debugging validation checks
            #if false
            // Used on non-obfuscated binaries during development to confirm the output is correct
            if (types.ptr != MetadataRegistration.ptypes)
                throw new Exception("Il2CppType** incorrect");
            if (genericInsts.ptr != MetadataRegistration.genericInsts)
                throw new Exception("Il2CppGenericInst** incorrect");
            if (methodSpecs.ptr != MetadataRegistration.methodSpecs)
                throw new Exception("Il2CppMethodSpec* incorrect");
            if (genericMethodTable.ptr != MetadataRegistration.genericMethodTable)
                throw new Exception("Il2CppGenericMethodFunctionsDefinitions* incorrect");
            if (metadataUsages.ptr != MetadataRegistration.metadataUsages)
                throw new Exception("Metadata usages pointer incorrect");
            if (fieldOffsets.ptr != MetadataRegistration.pfieldOffsets)
                throw new Exception("Field offsets pointer incorrect");

            if (types.count != MetadataRegistration.typesCount)
                throw new Exception("Count of Il2CppType* incorrect");
            if (genericInsts.count != MetadataRegistration.genericInstsCount)
                throw new Exception("Count of Il2CppGenericInst* incorrect");
            if (methodSpecs.count != MetadataRegistration.methodSpecsCount)
                throw new Exception("Count of Il2CppMethodSpec incorrect");
            if (genericMethodTable.count != MetadataRegistration.genericMethodTableCount)
                throw new Exception("Count of Il2CppGenericMethodFunctionsDefinitions");
            if (metadataUsages.count != usages.Count)
                throw new Exception("Count of metadata usages incorrect");
            if (fieldOffsets.count != MetadataRegistration.fieldOffsetsCount)
                throw new Exception("Count of field offsets incorrect");
            #endif
            #endregion

            // Perform substitution
            MetadataRegistration.ptypes                    = types.ptr;
            MetadataRegistration.typesCount                = types.count;
            MetadataRegistration.genericInsts              = genericInsts.ptr;
            MetadataRegistration.genericInstsCount         = genericInsts.count;
            MetadataRegistration.methodSpecs               = methodSpecs.ptr;
            MetadataRegistration.methodSpecsCount          = methodSpecs.count;
            MetadataRegistration.genericMethodTable        = genericMethodTable.ptr;
            MetadataRegistration.genericMethodTableCount   = genericMethodTable.count;
            MetadataRegistration.metadataUsages            = metadataUsages.ptr;
            MetadataRegistration.metadataUsagesCount       = (ulong) metadataUsages.count;
            MetadataRegistration.pfieldOffsets             = fieldOffsets.ptr;
            MetadataRegistration.fieldOffsetsCount         = fieldOffsets.count;

            // Force MetadataRegistration to pass validation in Il2CppBinary.Configure()
            MetadataRegistration.typeDefinitionsSizesCount = 0;
            MetadataRegistration.genericClassesCount       = MetadataRegistration.genericInstsCount + 1;

            // TODO: Write changes to stream

            StatusUpdate("Analyzing IL2CPP image");
        }
    }
}