// Copyright (c) 2017 Katy Coe - https://www.djkaty.com - https://github.com/djlaty
// All rights reserved

using System.IO;
using System.Text;

namespace Il2CppInspector
{
    public class Il2CppDumper
    {
        private readonly Il2CppInspector il2cpp;

        public Il2CppDumper(Il2CppInspector proc) {
            il2cpp = proc;
        }

        public void WriteFile(string outFile) {
            using (var writer = new StreamWriter(new FileStream(outFile, FileMode.Create))) {
                var metadata = il2cpp.Metadata;

                for (int imageIndex = 0; imageIndex < metadata.Images.Length; imageIndex++) {
                    var imageDef = metadata.Images[imageIndex];
                    writer.Write($"// Image {imageIndex}: {metadata.Strings[imageDef.nameIndex]} - {imageDef.typeStart}\n");
                }
                for (int idx = 0; idx < metadata.Types.Length; ++idx) {
                    var typeDef = metadata.Types[idx];
                    writer.Write($"// Namespace: {metadata.Strings[typeDef.namespaceIndex]}\n");
                    if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_SERIALIZABLE) != 0)
                        writer.Write("[Serializable]\n");
                    if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) ==
                        DefineConstants.TYPE_ATTRIBUTE_PUBLIC)
                        writer.Write("public ");
                    if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_ABSTRACT) != 0)
                        writer.Write("abstract ");
                    if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_SEALED) != 0)
                        writer.Write("sealed ");
                    if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_INTERFACE) != 0)
                        writer.Write("interface ");
                    else
                        writer.Write("class ");
                    writer.Write($"{metadata.Strings[typeDef.nameIndex]} // TypeDefIndex: {idx}\n{{\n");
                    writer.Write("\t// Fields\n");
                    var fieldEnd = typeDef.fieldStart + typeDef.field_count;
                    for (int i = typeDef.fieldStart; i < fieldEnd; ++i) {
                        var pField = metadata.Fields[i];
                        var pType = il2cpp.GetTypeFromTypeIndex(pField.typeIndex);
                        writer.Write("\t");
                        if ((pType.attrs & DefineConstants.FIELD_ATTRIBUTE_PRIVATE) ==
                            DefineConstants.FIELD_ATTRIBUTE_PRIVATE)
                            writer.Write("private ");
                        if ((pType.attrs & DefineConstants.FIELD_ATTRIBUTE_PUBLIC) ==
                            DefineConstants.FIELD_ATTRIBUTE_PUBLIC)
                            writer.Write("public ");
                        if ((pType.attrs & DefineConstants.FIELD_ATTRIBUTE_STATIC) != 0)
                            writer.Write("static ");
                        if ((pType.attrs & DefineConstants.FIELD_ATTRIBUTE_INIT_ONLY) != 0)
                            writer.Write("readonly ");
                        writer.Write($"{il2cpp.GetTypeName(pType)} {metadata.Strings[pField.nameIndex]}");
                        object multi = il2cpp.GetDefaultValueForField(i);
                        if (multi is string)
                            writer.Write($" = \"{multi}\"");
                        else if (multi != null)
                            writer.Write($" = {multi}");
                        writer.Write("; // 0x{0:x}\n",
                            il2cpp.GetFieldOffsetFromIndex(idx, i - typeDef.fieldStart));
                    }
                    writer.Write("\t// Methods\n");
                    var methodEnd = typeDef.methodStart + typeDef.method_count;
                    for (int i = typeDef.methodStart; i < methodEnd; ++i) {
                        var methodDef = metadata.Methods[i];
                        writer.Write("\t");
                        Il2CppType pReturnType = il2cpp.GetTypeFromTypeIndex(methodDef.returnType);
                        if ((methodDef.flags & DefineConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) ==
                            DefineConstants.METHOD_ATTRIBUTE_PRIVATE)
                            writer.Write("private ");
                        if ((methodDef.flags & DefineConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) ==
                            DefineConstants.METHOD_ATTRIBUTE_PUBLIC)
                            writer.Write("public ");
                        if ((methodDef.flags & DefineConstants.METHOD_ATTRIBUTE_VIRTUAL) != 0)
                            writer.Write("virtual ");
                        if ((methodDef.flags & DefineConstants.METHOD_ATTRIBUTE_STATIC) != 0)
                            writer.Write("static ");

                        writer.Write($"{il2cpp.GetTypeName(pReturnType)} {metadata.Strings[methodDef.nameIndex]}(");
                        for (int j = 0; j < methodDef.parameterCount; ++j) {
                            Il2CppParameterDefinition pParam = metadata.Params[methodDef.parameterStart + j];
                            string szParamName = metadata.Strings[pParam.nameIndex];
                            Il2CppType pType = il2cpp.GetTypeFromTypeIndex(pParam.typeIndex);
                            string szTypeName = il2cpp.GetTypeName(pType);
                            if ((pType.attrs & DefineConstants.PARAM_ATTRIBUTE_OPTIONAL) != 0)
                                writer.Write("optional ");
                            if ((pType.attrs & DefineConstants.PARAM_ATTRIBUTE_OUT) != 0)
                                writer.Write("out ");
                            if (j != methodDef.parameterCount - 1) {
                                writer.Write($"{szTypeName} {szParamName}, ");
                            }
                            else {
                                writer.Write($"{szTypeName} {szParamName}");
                            }
                        }
                        if (methodDef.methodIndex >= 0)
                            writer.Write("); // {0:x} - {1}\n",
                                il2cpp.Binary.MethodPointers[methodDef.methodIndex],
                                methodDef.methodIndex);
                        else
                            writer.Write("); // 0 - -1\n");
                    }
                    writer.Write("}\n");
                }
            }
        }
    }
}
