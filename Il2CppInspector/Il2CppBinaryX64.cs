/*
    Copyright 2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Linq;

namespace Il2CppInspector
{
    internal class Il2CppBinaryX64 : Il2CppBinary
    {
        public Il2CppBinaryX64(IFileFormatReader stream) : base(stream) { }
        public Il2CppBinaryX64(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }

        // Format of 64-bit LEA:
        // 0x48 - prefix signifying 64-bit mode
        // 8x8D - LEA opcode (8D /r, LEA r64, m)
        // 0xX5 - bottom 3 bits = 101 to indicate subsequent operand is a 32-bit displacement; middle 3 bits = register number; top 2 bits = 00
        // Bytes 03-06 - 32-bit displacement
        // Register numbers: 00 = RAX, 01 = RCX, 10 = RDX, 11 = RBX
        // See: https://software.intel.com/sites/default/files/managed/39/c5/325462-sdm-vol-1-2abcd-3abcd.pdf
        // Chapter 2.1, 2.1.3, 2.1.5 table 2-2, page 3-537
        // NOTE: There is a chance of false positives because of x86's variable instruction length architecture
        private (ulong nextInstruction, int reg, uint operand)? findLea(IFileFormatReader image, uint loc, int searchDistance) {

            // Find first LEA but don't search too far
            image.Position = loc;
            var buff = image.ReadBytes(searchDistance);
            var opcode = new byte[] { 0x48, 0x8D };
            uint i, index;

            for (i = 0, index = 0; i < buff.Length && index < opcode.Length; i++)
                if (buff[i] != opcode[index++])
                    index = 0;

            if (index < opcode.Length)
                return null;

            // Found LEA RnX, [RIP + disp32]
            var pLea = i - 2;
            var reg = (buff[i] >> 3) & 7;
            var operand = BitConverter.ToUInt32(buff, (int) pLea + 3);

            return (Image.GlobalOffset + loc + pLea + 7, reg, operand);
        }

        protected override (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc) {

            // Find first LEA in this function
            var lea = findLea(image, loc, 0x18);
            if (lea == null)
                return (0, 0);

            // Assume we've found the pointer to Il2CppCodegenRegistration(void) and jump there
            var pCgr = lea.Value.nextInstruction + lea.Value.operand;

            try {
                pCgr = Image.MapVATR(pCgr);
            }

            // Couldn't map virtual address to data in file, so it's not this function
            catch (InvalidOperationException) {
                return (0, 0);
            }

            // Find the first 2 LEAs which we'll hope contain pointers to CodeRegistration and MetadataRegistration
            var lea1 = findLea(image, (uint) pCgr, 0x60);
            if (lea1 == null)
                return (0, 0);

            var lea2 = findLea(image, Image.MapVATR(lea1.Value.nextInstruction), 0x20);
            if (lea2 == null)
                return (0, 0);

            Console.WriteLine($"{loc:X8}: {lea1.Value.nextInstruction + lea1.Value.operand:X16} / {lea2.Value.nextInstruction + lea2.Value.operand:X16}");
            return (0, 0);
        }
    }
}