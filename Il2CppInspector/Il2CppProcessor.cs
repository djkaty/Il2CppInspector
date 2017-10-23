/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace Il2CppInspector
{
    public class Il2CppProcessor
    {
        public Il2CppReader Code { get; }
        public Metadata Metadata { get; }

        public Il2CppProcessor(Il2CppReader code, Metadata metadata) {
            Code = code;
            Metadata = metadata;
        }

        public static List<Il2CppProcessor> LoadFromFile(string codeFile, string metadataFile) {
            // Load the metadata file
            Metadata metadata;
            try {
                metadata = new Metadata(new MemoryStream(File.ReadAllBytes(metadataFile)));
            }
            catch (Exception ex) {
                Console.Error.WriteLine(ex.Message);
                return null;
            }

            // Load the il2cpp code file (try ELF and PE)
            var memoryStream = new MemoryStream(File.ReadAllBytes(codeFile));
            IFileFormatReader stream =
                (((IFileFormatReader) ElfReader.Load(memoryStream) ??
                                      PEReader.Load(memoryStream)) ??
                                      MachOReader.Load(memoryStream)) ??
                                      UBReader.Load(memoryStream);
            if (stream == null) {
                Console.Error.WriteLine("Unsupported executable file format");
                return null;
            }

            var processors = new List<Il2CppProcessor>();
            foreach (var image in stream.Images) {
                Il2CppReader il2cpp;

                // We are currently supporting x86 and ARM architectures
                switch (image.Arch) {
                    case "x86":
                        il2cpp = new Il2CppReaderX86(image);
                        break;
                    case "ARM":
                        il2cpp = new Il2CppReaderARM(image);
                        break;
                    default:
                        Console.Error.WriteLine("Unsupported architecture");
                        return null;
                }

                // Find code and metadata regions
                if (!il2cpp.Load(metadata.Version)) {
                    Console.Error.WriteLine("Could not process IL2CPP image");
                }
                else {
                    processors.Add(new Il2CppProcessor(il2cpp, metadata));
                }
            }
            return processors;
        }

        public string GetTypeName(Il2CppType pType) {
            string ret;
            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_CLASS || pType.type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE) {
                Il2CppTypeDefinition klass = Metadata.Types[pType.data.klassIndex];
                ret = Metadata.GetString(klass.nameIndex);
            }
            else if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST) {
                Il2CppGenericClass generic_class = Code.Image.ReadMappedObject<Il2CppGenericClass>(pType.data.generic_class);
                Il2CppTypeDefinition pMainDef = Metadata.Types[generic_class.typeDefinitionIndex];
                ret = Metadata.GetString(pMainDef.nameIndex);
                var typeNames = new List<string>();
                Il2CppGenericInst pInst = Code.Image.ReadMappedObject<Il2CppGenericInst>(generic_class.context.class_inst);
                var pointers = Code.Image.ReadMappedArray<uint>(pInst.type_argv, (int)pInst.type_argc);
                for (int i = 0; i < pInst.type_argc; ++i) {
                    var pOriType = Code.Image.ReadMappedObject<Il2CppType>(pointers[i]);
                    pOriType.Init();
                    typeNames.Add(GetTypeName(pOriType));
                }
                ret += $"<{string.Join(", ", typeNames)}>";
            }
            else if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_ARRAY) {
                Il2CppArrayType arrayType = Code.Image.ReadMappedObject<Il2CppArrayType>(pType.data.array);
                var type = Code.Image.ReadMappedObject<Il2CppType>(arrayType.etype);
                type.Init();
                ret = $"{GetTypeName(type)}[]";
            }
            else if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY) {
                var type = Code.Image.ReadMappedObject<Il2CppType>(pType.data.type);
                type.Init();
                ret = $"{GetTypeName(type)}[]";
            }
            else {
                if ((int)pType.type >= szTypeString.Length)
                    ret = "unknow";
                else
                    ret = szTypeString[(int)pType.type];
            }
            return ret;
        }

        public Il2CppType GetTypeFromTypeIndex(int idx) {
            return Code.PtrMetadataRegistration.types[idx];
        }

        public int GetFieldOffsetFromIndex(int typeIndex, int fieldIndexInType) {
            // Versions from 22 onwards use an array of pointers in fieldOffsets
            bool fieldOffsetsArePointers = (Metadata.Version >= 22);

            // Some variants of 21 also use an array of pointers
            if (Metadata.Version == 21) {
                var f = Code.PtrMetadataRegistration.fieldOffsets;
                fieldOffsetsArePointers = (f[0] == 0 && f[1] == 0 && f[2] == 0 && f[3] == 0 && f[4] == 0 && f[5] > 0);
            }

            // All older versions use values directly in the array
            if (!fieldOffsetsArePointers) {
                var typeDef = Metadata.Types[typeIndex];
                return Code.PtrMetadataRegistration.fieldOffsets[typeDef.fieldStart + fieldIndexInType];
            }

            var ptr = Code.PtrMetadataRegistration.fieldOffsets[typeIndex];
            Code.Image.Stream.Position = Code.Image.MapVATR((uint)ptr) + 4 * fieldIndexInType;
            return Code.Image.Stream.ReadInt32();
        }

        private readonly string[] szTypeString =
        {
            "END",
            "void",
            "bool",
            "char",
            "sbyte",
            "byte",
            "short",
            "ushort",
            "int",
            "uint",
            "long",
            "ulong",
            "float",
            "double",
            "string",
            "PTR",//eg. void*
            "BYREF",
            "VALUETYPE",
            "CLASS",
            "T",
            "ARRAY",
            "GENERICINST",
            "TYPEDBYREF",
            "None",
            "IntPtr",
            "UIntPtr",
            "None",
            "FNPTR",
            "object",
            "SZARRAY",
            "T",
            "CMOD_REQD",
            "CMOD_OPT",
            "INTERNAL",
        };
    }
}
