/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Linq;

namespace Il2CppInspector
{
    internal class Il2CppBinaryX86 : Il2CppBinary
    {
        public Il2CppBinaryX86(IFileFormatReader stream) : base(stream) { }
        public Il2CppBinaryX86(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }

        protected override (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc) {
            ulong metadata, code;
            long pCgr;

            // x86
            // Assembly bytes to search for at start of each function
            var bytes = new byte[] {0x6A, 0x00, 0x6A, 0x00, 0x68};
            image.Position = loc;
            var buff = image.ReadBytes(5);
            if (bytes.SequenceEqual(buff)) {
                // Next 4 bytes are the function pointer being pushed onto the stack
                pCgr = image.ReadUInt32();

                // Start of next instruction
                if (image.ReadByte() != 0xB9)
                    return (0, 0);

                // Jump to Il2CppCodegenRegistration
                if(image.Version < 21) {
                    image.Position = image.MapVATR((ulong)pCgr + 1);
                    metadata = image.ReadUInt32();
                    image.Position = image.MapVATR((ulong)pCgr + 6);
                    code = image.ReadUInt32();
                } else {
                    image.Position = image.MapVATR((ulong)pCgr + 6);
                    metadata = image.ReadUInt32();
                    image.Position = image.MapVATR((ulong)pCgr + 11);
                    code = image.ReadUInt32();
                }
                return (code, metadata);
            }

            // x86 based on ELF PLT
            if (image is IElfReader elf) {
                var plt = elf.GetPLTAddress();

                // push ebp; mov ebp, esp; push ebx; and esp, 0FFFFFFF0h; sub esp, 20h; call $+5; pop ebx
                bytes = new byte[]
                    {0x55, 0x89, 0xE5, 0x53, 0x83, 0xE4, 0xF0, 0x83, 0xEC, 0x20, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x5B};
                image.Position = loc;
                buff = image.ReadBytes(16);
                if (!bytes.SequenceEqual(buff))
                    return (0, 0);

                // lea eax, (pCgr - offset)[ebx] (Position + 6 is the opcode lea eax; Position + 8 is the operand)
                image.Position += 6;

                // Ensure it's lea eax, #address
                if (image.ReadUInt16() != 0x838D)
                    return (0, 0);

                try {
                    pCgr = image.MapVATR(image.ReadUInt32() + plt);
                }
                // Could not find a mapping in the section table
                catch (InvalidOperationException) {
                    return (0, 0);
                }

                // Extract Metadata pointer
                // An 0x838D opcode indicates LEA (no indirection)
                image.Position = pCgr + 0x20;
                var opcode = image.ReadUInt16();
                metadata = image.ReadUInt32() + plt;

                // An 8x838B opcode indicates MOV (pointer indirection)
                if (opcode == 0x838B) {
                    image.Position = image.MapVATR(metadata);
                    metadata = image.ReadUInt32();
                }

                if (opcode != 0x838B && opcode != 0x838D)
                    return (0, 0);

                // Repeat the same logic for extracting the Code pointer
                image.Position = pCgr + 0x2A;
                opcode = image.ReadUInt16();
                code = image.ReadUInt32() + plt;

                if (opcode == 0x838B) {
                    image.Position = image.MapVATR(code);
                    code = image.ReadUInt32();
                }

                if (opcode != 0x838B && opcode != 0x838D)
                    return (0, 0);

                return (code, metadata);
            }

            return (0, 0);
        }
    }
}
