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
        // 0x48 - REX prefix signifying 64-bit mode with 64-bit operand size (REX prefix bits: Volume 2A, page 2-9)
        // 8x8D - LEA opcode (8D /r, LEA r64, m)
        // 0xX5 - bottom 3 bits = 101 to indicate subsequent operand is a 32-bit displacement; middle 3 bits = register number; top 2 bits = 00
        // Bytes 03-06 - 32-bit displacement
        // Register numbers: 00 = RAX, 01 = RCX, 10 = RDX, 11 = RBX
        // See: https://software.intel.com/sites/default/files/managed/39/c5/325462-sdm-vol-1-2abcd-3abcd.pdf
        // Chapter 2.1, 2.1.3, 2.1.5 table 2-2, page 3-537
        // NOTE: There is a chance of false positives because of x86's variable instruction length architecture
        private (int foundOffset, int reg, uint operand)? findLea(byte[] buff, int offset, int searchDistance) {

            // Find first LEA but don't search too far
            var opcode = new byte[] { 0x48, 0x8D };
            int i, index;

            for (i = offset, index = 0; i < offset + searchDistance && i < buff.Length && index < opcode.Length; i++)
                if (buff[i] != opcode[index++]) {
                    index = 0;

                    // Maybe we're starting a new match
                    if (buff[i] != opcode[index++])
                        index = 0;
                }

            if (index < opcode.Length)
                return null;

            var lea = getLea(buff, (int) i - 2);

            return (i - 2, lea.Value.reg, lea.Value.operand);
        }

        private (int reg, uint operand)? getLea(byte[] buff, int offset) {
            if (buff[offset] != 0x48 || buff[offset + 1] != 0x8D)
                return null;

            // Found LEA RnX, [RIP + disp32]
            var reg = (buff[offset + 2] >> 3) & 7;
            var operand = BitConverter.ToUInt32(buff, offset + 3);

            return (reg, operand);
        }

        // 0x40 to set 64-bit mode with 32-bit register size, 0x50+rd to push specified register number
        // Volume 2B, page 4-511
        private bool isPushR32(byte[] buff, int offset) => buff[offset] == 0x40 && buff[offset + 1] >= 0x50 && buff[offset + 1] < 0x58;

        // 0b0100_0X0Y to set 64-bit mode, 0x33 for XOR, 0b11_XXX_YYY for register numbers
        // Volume 2C, page 5-278
        private (int reg_op1, int reg_op2)? getXorR64R64(byte[] buff, int offset) {
            if ((buff[offset] & 0b1111_1010) != 0b_0100_0000 || buff[offset + 1] != 0x33 || (buff[offset + 2] & 0b1100_0000) != 0b1100_0000)
                return null;
            return (((buff[offset] & 0b0000_0100) << 1) + ((buff[offset + 2] & 0b0011_1000) >> 3),
                    ((buff[offset] & 0b0000_0001) << 3) + (buff[offset + 2] & 0b0000_0111));
        }

        protected override (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc) {

            // We have seen two versions of the initializer:
            // 1. Regular version
            // 2. Inlined version with il2cpp::utils::RegisterRuntimeInitializeAndCleanup(CallbackFunction, CallbackFunction, order)

            // Version 1 passes "this" in rcx and the arguments in rdx (our wanted pointer), r8d (always zero) and r9d (always zero)
            // Version 2 has a standard prologue and loads the wanted pointer into rax (lea rax)

            (int reg, uint operand)? lea;
            ulong pCgr = 0;

            image.Position = loc;
            var buff = image.ReadBytes(0x20); // arbitrary number of bytes, but enough to process the function

            // Check for regular version
            var xor = getXorR64R64(buff, 0); // 3 bytes
            if (xor != null && xor.Value.reg_op1 == xor.Value.reg_op2) {
                lea = getLea(buff, 3); // 7 bytes

                if (lea != null) {
                    xor = getXorR64R64(buff, 10);
                    if (xor != null && xor.Value.reg_op1 == xor.Value.reg_op2) {
                        // We found Il2CppCodegenRegistration(void)
                        pCgr = image.GlobalOffset + loc + 10 + lea.Value.operand;
                    }
                }
            }

            // Check for inlined version
            if (pCgr == 0) {
                // Check for prologue
                if (isPushR32(buff, 0)) {
                    // Linear sweep for LEA
                    var leaInlined = findLea(buff, 2, 0x1E); // 0x20 - 2
                    if (leaInlined == null)
                        return (0, 0);
                    // LEA is 7 bytes long
                    pCgr = image.GlobalOffset + loc + (uint) leaInlined.Value.foundOffset + 7 + leaInlined.Value.operand;
                }
            }

            if (pCgr == 0)
                return (0, 0);

            // Assume we've found the pointer to Il2CppCodegenRegistration(void) and jump there
            try {
                Image.Position = Image.MapVATR(pCgr);
            }

            // Couldn't map virtual address to data in file, so it's not this function
            catch (InvalidOperationException) {
                return (0, 0);
            }

            // Find the first 2 LEAs which we'll hope contain pointers to CodeRegistration and MetadataRegistration

            // There are two options here:
            // 1. il2cpp::vm::MetadataCache::Register is called directly with arguments in rcx, rdx and r8 (lea, lea, lea, jmp)
            // 2. The two functions being inlined. The arguments are loaded sequentially into rax after the prologue

            // By ignoring the REX R flag (bit 2 of the instruction prefix) which specifies an extension bit to the register operand,
            // we skip over "lea r8". This will leave us with two LEAs containing our desired pointers.

            buff = image.ReadBytes(0x40);

            // LEA is 7 bytes long
            var lea1 = findLea(buff, 0, 0x40 - 7);
            if (lea1 == null)
                return (0, 0);

            var lea2 = findLea(buff, lea1.Value.foundOffset + 7, 0x40 - lea1.Value.foundOffset - 7);
            if (lea2 == null)
                return (0, 0);

            // Use the original pointer found, not the file location + GlobalOffset because the data may be in a different section
            var ptr1 = pCgr + (uint) lea1.Value.foundOffset + 7 + lea1.Value.operand;
            var ptr2 = pCgr + (uint) lea2.Value.foundOffset + 7 + lea2.Value.operand;

            // RCX and RDX argument passing?
            if (lea1.Value.reg == 2 /* RDX */ && lea2.Value.reg == 1 /* RCX */)
                return (ptr2, ptr1);

            // RAX sequential loading?
            if (lea1.Value.reg == 0 /* RAX */ && lea2.Value.reg == 0 /* RAX */)
                return (ptr1, ptr2);

            return (0, 0);
        }
    }
}