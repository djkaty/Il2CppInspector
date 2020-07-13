// Copyright (c) 2019-2020 Carter Bush - https://github.com/carterbush
// Copyright (c) 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// Copyright 2020 Robert Xiao - https://robertxiao.ca/
// All rights reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Il2CppInspector.Reflection;
using Il2CppInspector.Cpp;
using Il2CppInspector.Model;

namespace Il2CppInspector.Outputs
{
    public class IDAPythonScript
    {
        // TODO: Make this readonly when we've integrated with ApplicationModel
        private AppModel model;
        private StreamWriter writer;
        // TODO: Remove when integrated with ApplicationModel
        private CppDeclarationGenerator declGenerator;

        public IDAPythonScript(AppModel model) => this.model = model;

        public void WriteScriptToFile(string outputFile) {
            // TODO: Integrate with ApplicationModel - use this hack so we can use CppDeclarationGenerator without disturbing the model passed in
            var internalModel = new AppModel(model.ILModel);
            internalModel.UnityVersion = model.UnityVersion;
            internalModel.UnityHeader = model.UnityHeader;
            internalModel.CppTypeCollection = CppTypeCollection.FromUnityHeaders(model.UnityHeader, model.WordSize);
            model = internalModel;
            declGenerator = new CppDeclarationGenerator(model);

            using var fs = new FileStream(outputFile, FileMode.Create);
            writer = new StreamWriter(fs, Encoding.UTF8);

            writeLine("# Generated script file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty");
            writeLine("# Target Unity version: " + model.UnityHeader);
            writeLine("print('Generated script file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty')");
            writeSectionHeader("Preamble");
            writePreamble();

            writeMethods();

            writeSectionHeader("Metadata Usages");
            writeUsages();

            writeSectionHeader("Function boundaries");
            writeFunctions();

            writeSectionHeader("IL2CPP Metadata");
            writeMetadata();

            writeLine("print('Script execution complete.')");
            writer.Close();
        }
        
        private void writePreamble() {
            writeLine(
@"import idaapi

def SetName(addr, name):
  ret = idc.set_name(addr, name, SN_NOWARN | SN_NOCHECK)
  if ret == 0:
    new_name = name + '_' + str(addr)
    ret = idc.set_name(addr, new_name, SN_NOWARN | SN_NOCHECK)

def MakeFunction(start):
  ida_funcs.add_func(start)

def SetType(addr, type):
  ret = idc.SetType(addr, type)
  if ret is None:
    print('SetType(0x%x, %r) failed!' % (addr, type))
");

            // Compatibility (in a separate decl block in case these are already defined)
            writeDecls(@"
typedef unsigned __int8 uint8_t;
typedef unsigned __int16 uint16_t;
typedef unsigned __int32 uint32_t;
typedef unsigned __int64 uint64_t;
typedef __int8 int8_t;
typedef __int16 int16_t;
typedef __int32 int32_t;
typedef __int64 int64_t;
");
            // IL2CPP internal types
            writeDecls(model.UnityHeaderText);
        }

        private void writeMethods() {
            writeSectionHeader("Method definitions");
            writeMethods(model.ILModel.MethodsByDefinitionIndex);

            writeSectionHeader("Constructed generic methods");
            writeMethods(model.ILModel.GenericMethods.Values);

            writeSectionHeader("Custom attributes generators");
            foreach (var method in model.ILModel.AttributesByIndices.Values.Where(m => m.VirtualAddress.HasValue)) {
                var address = method.VirtualAddress.Value.Start;
                writeName(address, $"{method.AttributeType.Name}_CustomAttributesCacheGenerator");
                writeComment(address, $"{method.AttributeType.Name}_CustomAttributesCacheGenerator(CustomAttributesCache *)");
            }

            writeSectionHeader("Method.Invoke thunks");
            foreach (var method in model.ILModel.MethodInvokers.Where(m => m != null)) {
                var address = method.VirtualAddress.Start;
                writeName(address, method.Name);
                writeComment(address, method);
            }
        }

        private void writeMethods(IEnumerable<MethodBase> methods) {
            foreach (var method in methods.Where(m => m.VirtualAddress.HasValue)) {
                declGenerator.IncludeMethod(method);
                writeDecls(declGenerator.GenerateRemainingTypeDeclarations());
                var address = method.VirtualAddress.Value.Start;
                writeTypedName(address, declGenerator.GenerateMethodDeclaration(method).ToSignatureString(), declGenerator.GlobalNamer.GetName(method));
                writeComment(address, method);
            }
        }
        
        private static string stringToIdentifier(string str) {
            str = str.Substring(0, Math.Min(32, str.Length));
            return str.ToCIdentifier();
        }

        private void writeUsages() {
            if (model.Package.MetadataUsages == null) {
                /* Version < 19 calls `il2cpp_codegen_string_literal_from_index` to get string literals.
                 * Unfortunately, metadata references are just loose globals in Il2CppMetadataUsage.cpp
                 * so we can't automatically name those. Next best thing is to define an enum for the strings. */
                var enumSrc = new StringBuilder();
                enumSrc.Append("enum StringLiteralIndex {\n");
                for (int i = 0; i < model.Package.StringLiterals.Length; i++) {
                    var str = model.Package.StringLiterals[i];
                    enumSrc.Append($"  STRINGLITERAL_{i}_{stringToIdentifier(str)},\n");
                }
                enumSrc.Append("};\n");

                writeDecls(enumSrc.ToString());

                return;
            }
            
            var stringType = declGenerator.AsCType(model.ILModel.TypesByFullName["System.String"]);
            foreach (var usage in model.Package.MetadataUsages) {
                var address = usage.VirtualAddress;
                string name;

                switch (usage.Type) {
                    case MetadataUsageType.StringLiteral:
                        var str = model.ILModel.GetMetadataUsageName(usage);
                        writeTypedName(address, stringType.ToString(), $"StringLiteral_{stringToIdentifier(str)}");
                        writeComment(address, str);
                        break;
                    case MetadataUsageType.Type:
                    case MetadataUsageType.TypeInfo:
                        var type = model.ILModel.GetMetadataUsageType(usage);
                        declGenerator.IncludeType(type);
                        writeDecls(declGenerator.GenerateRemainingTypeDeclarations());

                        name = declGenerator.TypeNamer.GetName(type);
                        if (usage.Type == MetadataUsageType.TypeInfo)
                            writeTypedName(address, $"struct {name}__Class *", $"{name}__TypeInfo");
                        else
                            writeTypedName(address, $"struct Il2CppType *", $"{name}__TypeRef");
                        writeComment(address, type.CSharpName);
                        break;
                    case MetadataUsageType.MethodDef:
                    case MetadataUsageType.MethodRef:
                        var method = model.ILModel.GetMetadataUsageMethod(usage);
                        declGenerator.IncludeMethod(method);
                        writeDecls(declGenerator.GenerateRemainingTypeDeclarations());

                        name = declGenerator.GlobalNamer.GetName(method);
                        writeTypedName(address, "struct MethodInfo *", $"{name}__MethodInfo");
                        writeComment(address, method);
                        break;
                }
            }
        }

        private void writeFunctions() {
            foreach (var func in model.Package.FunctionAddresses)
                writeLine($"MakeFunction({func.Key.ToAddressString()})");
        }

        private void writeMetadata() {
            var binary = model.Package.Binary;

            // TODO: In the future, add struct definitions/fields, data ranges and the entire IL2CPP metadata tree

            writeTypedName(binary.CodeRegistrationPointer, "struct Il2CppCodeRegistration", "g_CodeRegistration");
            writeTypedName(binary.MetadataRegistrationPointer, "struct Il2CppMetadataRegistration", "g_MetadataRegistration");

            if (model.Package.Version >= 24.2)
                writeTypedName(binary.CodeRegistration.pcodeGenModules,
                    $"struct Il2CppCodeGenModule *[{binary.CodeRegistration.codeGenModulesCount}]", "g_CodeGenModules");

            foreach (var ptr in binary.CodeGenModulePointers)
                writeTypedName(ptr.Value, "struct Il2CppCodeGenModule", $"g_{ptr.Key.Replace(".dll", "")}CodeGenModule");

            // This will be zero if we found the structs from the symbol table
            if (binary.RegistrationFunctionPointer != 0)
                writeName(binary.RegistrationFunctionPointer, "__GLOBAL__sub_I_Il2CppCodeRegistration.cpp");
        }

        private void writeSectionHeader(string sectionName) {
            writeLine("");
            writeLine($"# SECTION: {sectionName}");
            writeLine($"# -----------------------------");
            writeLine($"print('Processing {sectionName}')");
            writeLine("");
        }

        private void writeDecls(string decls) {
            var lines = decls.Replace("\r", "").Split('\n');
            var cleanLines = lines.Select((s) => s.ToEscapedString());
            var declString = string.Join('\n', cleanLines);
            if (declString != "")
                writeLine("idc.parse_decls('''" + declString + "''')");
        }

        // TODO: Temporary compatibility function, remove when integrated with ApplicationModel
        private void writeDecls(List<(TypeInfo ilType, CppComplexType valueType, CppComplexType referenceType,
            CppComplexType fieldsType, CppComplexType vtableType, CppComplexType staticsType)> types)
            => writeDecls(string.Join("\n",
                  types.SelectMany(t => new List<CppType> {t.vtableType, t.staticsType, t.fieldsType, t.valueType, t.referenceType})
                            .Where(t => t != null)
                            .Select(t => t.ToString())));

        private void writeName(ulong address, string name) {
            writeLine($"SetName({address.ToAddressString()}, r'{name.ToEscapedString()}')");
        }

        private void writeTypedName(ulong address, string type, string name) {
            writeName(address, name);
            writeLine($"SetType({address.ToAddressString()}, r'{type.ToEscapedString()}')");
        }

        private void writeComment(ulong address, object comment) {
            writeLine($"idc.set_cmt({address.ToAddressString()}, r'{comment.ToString().ToEscapedString()}', 1)");
        }

        private void writeLine(string line) => writer.WriteLine(line);
    }
}
