/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Linq;

namespace Il2CppInspector
{
    internal class Il2CppBinaryX86 : Il2CppBinary
    {
        public Il2CppBinaryX86(IFileFormatReader stream) : base(stream) { }
        public Il2CppBinaryX86(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }

        protected override (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc) {
            ulong metadata, code;
            long funcPtr;
            ushort opcode;

            // Variant 1

            // Assembly bytes to search for at start of each function
            var bytes = new byte[] {0x6A, 0x00, 0x6A, 0x00, 0x68};
            image.Position = loc;
            var buff = image.ReadBytes(5);
            if (bytes.SequenceEqual(buff)) {
                // Next 4 bytes are the function pointer being pushed onto the stack
                funcPtr = image.ReadUInt32();

                // Start of next instruction
                if (image.ReadByte() != 0xB9)
                    return (0, 0);

                // Jump to Il2CppCodegenRegistration
                image.Position = image.MapVATR((ulong) funcPtr + 6);
                metadata = image.ReadUInt32();
                image.Position = image.MapVATR((ulong) funcPtr + 11);
                code = image.ReadUInt32();
                return (code, metadata);
            }

            // Variant 2
            bytes = new byte[]
                {0x55, 0x89, 0xE5, 0x53, 0x83, 0xE4, 0xF0, 0x83, 0xEC, 0x20, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x5B};
            image.Position = loc;
            buff = image.ReadBytes(16);
            if (!bytes.SequenceEqual(buff))
                return (0, 0);

            image.Position += 8;
            funcPtr = image.MapVATR(image.ReadUInt32() + image.GlobalOffset);
            if (funcPtr > image.Stream.BaseStream.Length)
                return (0, 0);

            // Extract Metadata pointer
            // An 0x838D opcode indicates LEA (no indirection)
            image.Position = funcPtr + 0x20;
            opcode = image.ReadUInt16();
            metadata = image.ReadUInt32() + image.GlobalOffset;

            // An 8x838B opcode indicates MOV (pointer indirection)
            if (opcode == 0x838B) {
                image.Position = image.MapVATR(metadata);
                metadata = image.ReadUInt32();
            }

            if (opcode != 0x838B && opcode != 0x838D)
                return (0, 0);

            // Repeat the same logic for extracting the Code pointer
            image.Position = funcPtr + 0x2A;
            opcode = image.ReadUInt16();
            code = image.ReadUInt32() + image.GlobalOffset;

            if (opcode == 0x838B) {
                image.Position = image.MapVATR(code);
                code = image.ReadUInt32();
            }

            if (opcode != 0x838B && opcode != 0x838D)
                return (0, 0);

            return (code, metadata);
        }
    }
}
