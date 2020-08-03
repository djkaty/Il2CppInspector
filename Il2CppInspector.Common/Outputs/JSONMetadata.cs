// Copyright (c) 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using Il2CppInspector.Reflection;
using Il2CppInspector.Model;

namespace Il2CppInspector.Outputs
{
    // Output module to produce machine readable metadata in JSON format
    public class JSONMetadata
    {
        private readonly AppModel model;
        private Utf8JsonWriter writer;

        // Allow non-compliant C-style comments in JSON output
        public bool AllowComments { get; set; } = false;

        public JSONMetadata(AppModel model) => this.model = model;

        // Write JSON metadata to file
        public void Write(string outputFile) {

            using var fs = new FileStream(outputFile, FileMode.Create);
            writer = new Utf8JsonWriter(fs, options: new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();

            // Output address map of everything in the binary that we recognize
            writeObject(
                "addressMap",
                () => {
                    writeMethods();
                    writeStringLiterals();
                    writeUsages();
                    writeFunctions();
                    writeMetadata();
                },
                "Address map of methods, internal functions, type pointers and string literals in the binary file"
            );

            writer.WriteEndObject();
            writer.Dispose();
        }

        private void writeMethods() {
            writeArray("methodDefinitions", () => writeMethods(model.GetMethodGroup("types_from_methods")), "Method definitions");
            writeArray("constructedGenericMethods", () => writeMethods(model.GetMethodGroup("types_from_generic_methods")), "Constructed generic methods");

            writeArray("customAttributesGenerators", () => {
                foreach (var method in model.ILModel.AttributesByIndices.Values) {
                    writeObject(() => writeTypedFunctionName(method.VirtualAddress.Value.Start, method.Signature, method.Name));
                }
            }, "Custom attributes generators");

            writeArray("methodInvokers", () => {
                foreach (var method in model.ILModel.MethodInvokers.Where(m => m != null)) {
                    writeObject(() => writeTypedFunctionName(method.VirtualAddress.Start, method.GetSignature(model.UnityVersion), method.Name));
                }
            }, "Method.Invoke thunks");
        }

        private void writeMethods(IEnumerable<AppMethod> methods) {
            foreach (var method in methods) {
                writeObject(() => {
                    writeTypedFunctionName(method.MethodCodeAddress, method.CppFnPtrType.ToSignatureString(), method.CppFnPtrType.Name);
                    writeDotNetSignature(method.Method);
                });
            }
        }

        private void writeStringLiterals() {
            writeArray("stringLiterals", () => {
                foreach (var str in model.Strings)
                    writeObject(() => {
                        // For version < 19
                        if (model.StringIndexesAreOrdinals) {
                            writer.WriteNumber("ordinal", str.Key);
                            writer.WriteString("name", $"STRINGLITERAL_{str.Key}_{stringToIdentifier(str.Value)}");
                        // For version >= 19
                        } else {
                            writer.WriteString("virtualAddress", str.Key.ToAddressString());
                            writer.WriteString("name", "StringLiteral_" + stringToIdentifier(str.Value));
                        }
                        writer.WriteString("string", str.Value);
                    });
            }, "String literals");
        }

        private void writeUsages() {
            // TypeInfo addresses for all types from metadata usages
            writeArray("typeInfoPointers",
                () => {
                    foreach (var type in model.Types.Values) {
                        // A type may have no addresses, for example an unreferenced array type

                        if (type.TypeClassAddress != 0xffffffff_ffffffff) {
                            writeObject(() => {
                                writeTypedName(type.TypeClassAddress, $"struct {type.Name}__Class *", $"{type.Name}__TypeInfo");
                                writeDotNetTypeName(type.ILType);
                            });
                        }
                    }
                }, "Il2CppClass (TypeInfo) pointers");

            // Reference addresses for all types from metadata usages
            writeArray("typeRefPointers",
                () => {
                    foreach (var type in model.Types.Values) {
                        if (type.TypeRefPtrAddress != 0xffffffff_ffffffff) {
                            writeObject(() => {
                                // A generic type definition does not have any direct C++ types, but may have a reference
                                writeName(type.TypeRefPtrAddress, $"{type.Name}__TypeRef");
                                writeDotNetTypeName(type.ILType);
                            });
                        }
                    }
                }, "Il2CppType (TypeRef) pointers");

            // Metedata usage methods
            writeArray("methodInfoPointers",
                () => {
                    foreach (var method in model.Methods.Values.Where(m => m.MethodInfoPtrAddress != 0xffffffff_ffffffff)) {
                        writeObject(() => {
                            writeName(method.MethodInfoPtrAddress, $"{method.CppFnPtrType.Name}__MethodInfo");
                            writeDotNetSignature(method.Method);
                        });
                    }
                }, "MethodInfo pointers");
        }

        private void writeFunctions() {
            writeArray("functionAddresses", () => {
            foreach (var func in model.Package.FunctionAddresses)
                writer.WriteStringValue(func.Key.ToAddressString());
            }, "Function boundaries");
        }

        private void writeMetadata() {
            var binary = model.Package.Binary;

            // TODO: In the future, add struct definitions/fields, data ranges and the entire IL2CPP metadata tree
            writeArray("typeMetadata", () => {
                writeObject(() => writeTypedName(binary.CodeRegistrationPointer, "struct Il2CppCodeRegistration", "g_CodeRegistration"));
                writeObject(() => writeTypedName(binary.MetadataRegistrationPointer, "struct Il2CppMetadataRegistration", "g_MetadataRegistration"));

                if (model.Package.Version >= 24.2)
                    writeObject(() => writeTypedName(binary.CodeRegistration.pcodeGenModules,
                        // Ghidra doesn't like *[x] or ** so use * * instead
                        $"struct Il2CppCodeGenModule * *", "g_CodeGenModules"));

                foreach (var ptr in binary.CodeGenModulePointers)
                    writeObject(() => writeTypedName(ptr.Value, "struct Il2CppCodeGenModule", $"g_{ptr.Key.Replace(".dll", "")}CodeGenModule"));
            }, "IL2CPP Type Metadata");

            writeArray("functionMetadata", () => {
                // This will be zero if we found the structs from the symbol table
                if (binary.RegistrationFunctionPointer != 0)
                    writeObject(() => writeTypedFunctionName(binary.RegistrationFunctionPointer,
                        "void il2cpp_codegen_register(const Il2CppCodeRegistration* const codeRegistration, const Il2CppMetadataRegistration* const metadataRegistration, const Il2CppCodeGenOptions* const codeGenOptions)",
                        "il2cpp_codegen_register"));
            }, "IL2CPP Function Metadata");
        }

        private void writeObject(Action objectWriter) => writeObject(null, objectWriter);

        private void writeObject(string name, Action objectWriter, string description = null) {
            if (AllowComments && description != null)
                writer.WriteCommentValue(" " + description + " ");
            if (name != null)
                writer.WriteStartObject(name);
            else
                writer.WriteStartObject();
            objectWriter();
            writer.WriteEndObject();
        }

        private void writeArray(string name, Action arrayWriter, string description = null) {
            writer.WriteStartArray(name);
            if (AllowComments && description != null)
                writer.WriteCommentValue(" " + description + " ");
            arrayWriter();
            writer.WriteEndArray();
        }

        private void writeName(ulong address, string name) {
            writer.WriteString("virtualAddress", address.ToAddressString());
            writer.WriteString("name", name.ToEscapedString());
        }

        private void writeTypedName(ulong address, string type, string name) {
            writeName(address, name);
            writer.WriteString("type", type.ToEscapedString());
        }

        private void writeTypedFunctionName(ulong address, string type, string name) {
            writeName(address, name);
            writer.WriteString("signature", type.ToEscapedString());
        }

        private void writeDotNetSignature(MethodBase method) {
            writer.WriteString("dotNetSignature", method.ToString().ToEscapedString());
        }

        private void writeDotNetTypeName(TypeInfo type) {
            writer.WriteString("dotNetType", type.CSharpName);
        }

        private static string stringToIdentifier(string str) {
            str = str.Substring(0, Math.Min(32, str.Length));
            return str.ToCIdentifier();
        }
    }
}
