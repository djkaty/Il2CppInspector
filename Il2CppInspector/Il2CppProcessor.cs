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

        public static Il2CppProcessor LoadFromFile(string codeFile, string metadataFile) {
            // Load the metadata file
            var metadata = new Metadata(new MemoryStream(File.ReadAllBytes(metadataFile)));

            // Load the il2cpp code file (try ELF and PE)
            var memoryStream = new MemoryStream(File.ReadAllBytes(codeFile));
            IFileFormatReader stream = (IFileFormatReader) ElfReader.Load(memoryStream) ?? PEReader.Load(memoryStream);
            if (stream == null) {
                Console.Error.WriteLine("Unsupported executable file format");
                return null;
            }

            Il2CppReader il2cpp;

            // We are currently supporting x86 and ARM architectures
            switch (stream.Arch) {
                case "x86":
                    il2cpp = new Il2CppReaderX86(stream);
                    break;
                case "ARM":
                    il2cpp = new Il2CppReaderARM(stream);
                    break;
                default:
                    Console.Error.WriteLine("Unsupported architecture");
                    return null;
            }

            // Find code and metadata regions
            if (!il2cpp.Load()) {
                Console.Error.WriteLine("Could not process IL2CPP image");
                return null;
            }

            return new Il2CppProcessor(il2cpp, metadata);
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
