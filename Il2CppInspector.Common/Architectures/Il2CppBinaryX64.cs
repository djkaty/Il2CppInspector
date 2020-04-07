/*
    Copyright 2019-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Il2CppInspector
{
    internal class Il2CppBinaryX64 : Il2CppBinary
    {
        public Il2CppBinaryX64(IFileFormatReader stream) : base(stream) { }
        public Il2CppBinaryX64(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }

        // Format of 64-bit LEA:
        // 0x48/0x4C - REX prefix signifying 64-bit mode with 64-bit operand size (REX prefix bits: Volume 2A, page 2-9) (bit 2 is register bit 3)
        // 8x8D - LEA opcode (8D /r, LEA r64, m)
        // 0xX5 - bottom 3 bits = 101 to indicate subsequent operand is a 32-bit displacement; middle 3 bits = register number; top 2 bits = 00
        // Bytes 03-06 - 32-bit displacement
        // Register numbers: 00b = RAX, 01b = RCX, 10b = RDX, 11b = RBX
        // See: https://software.intel.com/sites/default/files/managed/39/c5/325462-sdm-vol-1-2abcd-3abcd.pdf
        // Chapter 2.1, 2.1.3, 2.1.5 table 2-2, page 3-537
        // NOTE: There is a chance of false positives because of x86's variable instruction length architecture
        private (int foundOffset, int reg, uint operand)? findLea(byte[] buff, int offset, int searchDistance) {

            // Find first LEA but don't search too far (either 0x48 0x8D, or 0x4C 0x8D)
            int i, index;

            for (i = offset, index = 0; i < offset + searchDistance && i < buff.Length && index < 2; i++)
                if (index == 1 && buff[i] != 0x8D)
                    index = 0;

                else if (index != 0 || buff[i] == 0x48 || buff[i] == 0x4C) {
                    ++index;
                }

            if (index < 2)
                return null;

            var lea = getLea(buff, (int) i - 2);

            return (i - 2, lea.Value.reg, lea.Value.operand);
        }

        private (int reg, uint operand)? getLea(byte[] buff, int offset) {
            if ((buff[offset] != 0x48 && buff[offset] != 0x4C) || buff[offset + 1] != 0x8D)
                return null;

            // Found LEA RnX, [RIP + disp32]
            var reg = ((buff[offset + 2] >> 3) & 7) + ((buff[offset] << 1) & 8);
            var operand = BitConverter.ToUInt32(buff, offset + 3);

            return (reg, operand);
        }

        // REX.W + 0x89 - for a 5-byte mov r/m64,r64
        // Volume 2B, page 4-35
        private bool isMovRM64R64(byte[] buff, int offset = 0) => buff[offset] == 0x48 && buff[offset + 1] == 0x89;

        // REX 0x40 to set 64-bit mode with 64-bit register size, register bit 3 in REX bit 0; bottom 3 bits of opcode are register bits 0-2
        // or the same thing without REX prefix
        // Volume 2B, page 4-511
        private bool isPushR64(byte[] buff, int offset = 0) =>
            ((buff[offset] == 0x40 || buff[offset] == 0x41) && buff[offset + 1] >= 0x50 && buff[offset + 1] <= 0x57)
            || (buff[offset] >= 0x50 && buff[offset] <= 0x57);

        // push rbp is a one-byte instruction encoded as 0x55
        // mov rbp, rsp is a 3-byte instruction encoded as 0x48 0x89 0xE5
        private bool isPrologue(byte[] buff, int offset = 0) =>
            isPushR64(buff) && buff[offset + 1] == 0x48 && buff[offset + 2] == 0x89 && buff[offset + 3] == 0xE5;

        // 0b0100_0X0Y to set 64-bit mode, 0x31 or 0x33 for XOR, 0b11_XXX_YYY for register numbers
        // Volume 2C, page 5-612
        private (int reg_op1, int reg_op2)? getXorR64R64(byte[] buff, int offset = 0) {
            if ((buff[offset] & 0b1111_1010) != 0b_0100_0000 ||
                (buff[offset + 1] != 0x31 && buff[offset + 1] != 0x33) ||
                (buff[offset + 2] & 0b1100_0000) != 0b1100_0000)
                return null;
            return (((buff[offset] & 0b0000_0100) << 1) + ((buff[offset + 2] & 0b0011_1000) >> 3),
                    ((buff[offset] & 0b0000_0001) << 3) + (buff[offset + 2] & 0b0000_0111));
        }

        // 0x31 or 0x33 for XOR, 0b11_XXX_YYY for register numbers
        // Volume 2C, page 5-612
        private (int reg_op1, int reg_op2)? getXorR32R32(byte[] buff, int offset = 0) {
            if ((buff[offset] != 0x31 && buff[offset] != 0x33) || (buff[offset + 1] & 0b1100_0000) != 0b1100_0000)
                return null;
            return ((buff[offset + 1] & 0b0011_1000) >> 3, buff[offset + 1] & 0b0000_0111);
        }

        protected override (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc) {

            // Setup
            var buffSize = 0x76; // minimum number of bytes to process the longest expected function
            var leaSize = 7; // the length of an LEA instruction with a 64-bit register operand and a 32-bit memory operand
            var xor64Size = 3; // the length of a XOR instruction of two 64-bit registers
            var xor32Size = 2; // the length of a XOR instruction of two 32-bit registers
            var pushSize = 2; // the length of a PUSH instruction with a 64-bit register
            var offset = 0;

            int RAX = 0, RBX = 3, RCX = 1, RDX = 2, RSI = 6, RDI = 7; // R8 = 8

            ulong pCgr = 0; // the point to the code registration function

            image.Position = loc;
            var buff = image.ReadBytes(buffSize);

            // We have seen two versions of the initializer:
            // 1. Regular version
            // 2. Inlined version with il2cpp::utils::RegisterRuntimeInitializeAndCleanup(CallbackFunction, CallbackFunction, order)

            // Version 1 passes "this" in rcx and the arguments in rdx (our wanted pointer), r8d (always zero) and r9d (always zero)
            //           or "this" in rdi, and the arguments in rsi (our wanted pointer), edx (always zero) and ecx (always zero)
            // Version 2 has a standard prologue and loads the wanted pointer into rax or rbp (lea rax/rbp)

            (int reg, uint operand)? lea;

            // Check for regular version
            // Generalize it as follows:
            // - each instruction must be lea r64, imm32 or xor r32, r32
            // - xors must always have the same register for both operands
            // - lea that can't be mapped into the file is the pointer to 'this', otherwise it's the pointer to the init function
            // - the last instruction should always be jmp (not currently enforced)
            // - function length should not be longer than 5 instructions (two leas, two xors and one jmp)

            offset = 0;
            for (var instructions = 0; instructions < 4; instructions++) {
                // All allowed instruction types
                var xor32 = getXorR32R32(buff, offset);
                var xor64 = getXorR64R64(buff, offset);
                lea =       getLea(buff, offset);

                if (xor32 != null && xor32.Value.reg_op1 == xor32.Value.reg_op2) {
                    offset += xor32Size;
                }
                else if (xor64 != null && xor64.Value.reg_op1 == xor64.Value.reg_op2) {
                    offset += xor64Size;
                }
                else if (lea != null) {
                    offset += leaSize;

                    if (pCgr == 0) {
                        try {
                            // We may have found Il2CppCodegenRegistration(void)
                            pCgr = image.GlobalOffset + loc + (ulong) offset + lea.Value.operand;
                            var newLoc = image.MapVATR(pCgr);
                        }
                        catch (InvalidOperationException) {
                            // this pointer
                            pCgr = 0;
                        }
                    }
                }
                else {
                    // not lea or xor
                    pCgr = 0;
                    break;
                }
            }

            // Check for inlined version
            if (pCgr == 0) {
                // Check for prologue
                // - A sequence of 0 or more mov [rsp+argX], rXX followed by 1 or more push rXX
                offset = 0;
                while (isMovRM64R64(buff, offset))
                    offset += 5;

                if (isPushR64(buff, offset)) {
                    // Linear sweep for LEA
                    var leaInlined = findLea(buff, pushSize, buffSize - pushSize);
                    if (leaInlined != null)
                        pCgr = image.GlobalOffset + loc + (uint) leaInlined.Value.foundOffset + (uint) leaSize + leaInlined.Value.operand;
                }
            }

            // Assume we've found the pointer to Il2CppCodegenRegistration(void) and jump there
            if (pCgr != 0) {
                try {
                    Image.Position = Image.MapVATR(pCgr);
                }

                // Couldn't map virtual address to data in file, so it's not this function
                catch (InvalidOperationException) {
                    pCgr = 0;
                }
            }

            // Find the first 2 LEAs which we'll hope contain pointers to CodeRegistration and MetadataRegistration

            // There are two options here:
            // 1. il2cpp::vm::MetadataCache::Register is called directly with arguments in rcx, rdx, r8 or rdi, rsi, rdx (lea, lea, lea, jmp)
            // 2. The two functions being inlined. The arguments are loaded sequentially into rax after the prologue

            if (pCgr != 0) {
                var buff2Size = 0x50;
                var buff2 = image.ReadBytes(buffSize);
                offset = 0;

                var leas = new Dictionary<(int index, ulong address), int>();

                // Find the first three LEAs in the function
                while (offset + leaSize < buff2Size && leas.Count < 3) {
                    var nextLea = findLea(buff2, offset, buff2Size - (offset + leaSize));

                    // Use the original pointer found, not the file location + GlobalOffset because the data may be in a different section
                    if (nextLea != null)
                        leas.Add((leas.Count, pCgr + (uint) nextLea.Value.foundOffset + (uint) leaSize + nextLea.Value.operand), nextLea.Value.reg);
                    
                    offset = nextLea?.foundOffset + leaSize ?? buff2Size;
                }

                if ((image.Version < 21 && leas.Count == 2) || (image.Version >= 21 && leas.Count == 3)) {
                    // Register-based argument passing?
                    var leaRSI = leas.FirstOrDefault(l => l.Value == RSI).Key.address;
                    var leaRDI = leas.FirstOrDefault(l => l.Value == RDI).Key.address;

                    if (leaRSI != 0 && leaRDI != 0)
                        return (leaRDI, leaRSI);

                    var leaRCX = leas.FirstOrDefault(l => l.Value == RCX).Key.address;
                    var leaRDX = leas.FirstOrDefault(l => l.Value == RDX).Key.address;

                    if (leaRCX != 0 && leaRDX != 0)
                        return (leaRCX, leaRDX);

                    // RAX sequential loading? If so, take the first two arguments
                    var leasRAX = leas.Where(l => l.Value == RAX).OrderBy(l => l.Key.index).Select(l => l.Key.address).ToArray();
                    if (leasRAX.Length > 1)
                        return (leasRAX[0], leasRAX[1]);
                }
            }

            // If no initializer is found, we may be looking at a DT_INIT function which calls its own function table manually
            // In the sample we have seen (PlayStation 4), this function runs through two function tables:
            // 1. Start address of table loaded into rbx, pointer past end of table in r12 (lea rbx; lea r12)
            // 2. Pointer to final address of 2nd table loaded into rbx (lea rbx), runs backwards (8 bytes per entry) until finding 0xFFFFFFFF_FFFFFFFF
            // The strategy: find these LEAs, acquire and merge the two function tables, then call ourselves in a loop to check each function address

            // Expect function prologue and at least 3 64-bit register pushes (there are probably more)
            if (!isPrologue(buff) || !isPushR64(buff, 4) || !isPushR64(buff, 6) || !isPushR64(buff, 8))
                return (0, 0);

            // Find the start and end addresses of the first function table
            var leaOfStart = findLea(buff, 10, buffSize - 10);
            if (leaOfStart == null || leaOfStart.Value.reg != RBX) // Most be lea rbx
                return (0, 0);

            var leaOfEnd = findLea(buff, leaOfStart.Value.foundOffset + leaSize, buffSize - (leaOfStart.Value.foundOffset + leaSize));
            if (leaOfEnd == null || leaOfEnd.Value.reg == RBX) // Must be lea with any register besides rbx
                return (0, 0);

            var ptrStart1 = leaOfStart.Value.foundOffset + leaSize + leaOfStart.Value.operand;
            var ptrEnd1   = leaOfEnd  .Value.foundOffset + leaSize + leaOfEnd  .Value.operand;

            // Find the address of the last item in the second function table
            var leaOfLastItem = findLea(buff, leaOfEnd.Value.foundOffset + leaSize, buffSize - (leaOfEnd.Value.foundOffset + leaSize));
            if (leaOfLastItem == null || leaOfLastItem.Value.reg != 0b11) // Must be lea rbx
                return (0, 0);

            var entrySize = 8; // 64-bit array entries
            var ptrEnd2 = leaOfLastItem.Value.foundOffset + leaSize + leaOfLastItem.Value.operand + entrySize;

            // Work backwards to find the address of the first item in the second function table
            var ptrStart2 = ptrEnd2;
            while (image.ReadUInt64(image.MapVATR((ulong) ptrStart2)) != 0xFFFF_FFFF_FFFF_FFFF)
                ptrStart2 -= entrySize;
            ptrStart2 += entrySize;

            // Acquire both function tables
            var funcs1 = image.ReadMappedWordArray((ulong) ptrStart1, (int) (ptrEnd1 - ptrStart1) / entrySize);
            var funcs2 = image.ReadMappedWordArray((ulong) ptrStart2, (int) (ptrEnd2 - ptrStart2) / entrySize);

            // Check every function
            var funcs = funcs1.Concat(funcs2);

            foreach (var pFunc in funcs) {
                var result = ConsiderCode(image, image.MapVATR((ulong) pFunc));
                if (result != (0, 0))
                    return result;
            }
            return (0, 0);
        }
    }
}