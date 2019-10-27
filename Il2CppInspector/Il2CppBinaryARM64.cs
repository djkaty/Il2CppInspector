/*
    Copyright 2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;

namespace Il2CppInspector
{
    // A64 ISA reference: https://static.docs.arm.com/ddi0596/a/DDI_0596_ARM_a64_instruction_set_architecture.pdf
    internal class Il2CppBinaryARM64 : Il2CppBinary
    {
        public Il2CppBinaryARM64(IFileFormatReader stream) : base(stream) { }

        public Il2CppBinaryARM64(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }

        private (uint reg, ulong page)? getAdrp(uint inst, ulong pc) {
            if ((inst.Bits(24, 8) & 0b_1000_1111) != 1 << 7)
                return null;

            var addendLo = inst.Bits(29, 2);
            var addendHi = inst.Bits(5, 19);
            var addend = (addendHi << 14) + (addendLo << 12);
            var page = pc & ~((1Lu << 12) - 1);
            var reg = inst.Bits(0, 5);

            return (reg, page + addend);
        }

        // https://static.docs.arm.com/100878/0100/fundamentals_of_armv8_a_100878_0100_en.pdf states:
        // Unlike ARMv7-A, there is no implied offset of 4 or 8 bytes
        private (uint reg, ulong addr)? getAdr(uint inst, ulong pc) {
            if (inst.Bits(24, 5) != 0b10000 || inst.Bits(31, 1) != 0)
                return null;

            ulong imm = (inst.Bits(5, 19) << 2) + inst.Bits(29, 2);

            // Sign extend the 21-bit number to 64 bits
            imm = (imm & (1 << 20)) == 0 ? imm : imm | unchecked((ulong) -(1 << 21));

            var reg = inst.Bits(0, 5);

            return (reg, pc + imm);
        }

        private (uint reg_n, uint reg_d, uint imm)? getAdd64(uint inst) {
            if (inst.Bits(22, 10) != 0b_1001_0001_00)
                return null;

            var imm = inst.Bits(10, 12);
            var reg_n = inst.Bits(5, 5);
            var reg_d = inst.Bits(0, 5);

            return (reg_n, reg_d, imm);
        }

        private (uint reg_t, uint reg_n, uint simm)? getLdr64ImmOffset(uint inst) {
            if (inst.Bits(22, 10) != 0b_11_1110_0101)
                return null;

            var imm = inst.Bits(10, 12);
            var reg_t = inst.Bits(0, 5);
            var reg_n = inst.Bits(5, 5);

            return (reg_t, reg_n, imm);
        }

        private bool isB(uint inst) => inst.Bits(26, 6) == 0b_000101;

        private Dictionary<uint, ulong> sweepForAddressLoads(List<uint> func, ulong baseAddress, IFileFormatReader image) {
            // List of registers and addresses loaded into them
            var regs = new Dictionary<uint, ulong>();

            // Iterate each instruction
            var pc = baseAddress;
            foreach (var inst in func) {

                // Is it an ADRP Xn, #page?
                if (getAdrp(inst, pc) is (uint reg, ulong page)) {
                    // If we've had an earlier ADRP for the same register, we'll discard the previous load
                    if (regs.ContainsKey(reg))
                        regs[reg] = page;
                    else
                        regs.Add(reg, page);
                }

                if (getAdr(inst, pc) is (uint reg_adr, ulong addr)) {
                    if (regs.ContainsKey(reg_adr))
                        regs[reg_adr] = addr;
                    else
                        regs.Add(reg_adr, addr);
                }

                // Is it an ADD Xm, Xn, #offset?
                if (getAdd64(inst) is (uint reg_n, uint reg_d, uint imm)) {
                    // We are only interested in registers that have already had an ADRP, and the ADD must be to itself
                    if (reg_n == reg_d && regs.ContainsKey(reg_d))
                        regs[reg_d] += imm;
                }

                // Is it an LDR Xm, [Xn, #offset]?
                if (getLdr64ImmOffset(inst) is (uint reg_t, uint reg_ldr_n, uint simm)) {
                    // We are only interested in registers that have already had an ADRP, and the LDR must be to itself
                    if (reg_t == reg_ldr_n && regs.ContainsKey(reg_ldr_n)) {
                        regs[reg_ldr_n] += simm * 8; // simm is a byte offset in a multiple of 8

                        // Now we have a pointer address, dereference it
                        regs[reg_ldr_n] = image.ReadUInt64(image.MapVATR(regs[reg_ldr_n]));
                    }
                }

                // Advance program counter which we need to calculate ADRP pages correctly
                pc += 4;
            }
            return regs;
        }

        private List<uint> getFunctionAtFileOffset(IFileFormatReader image, uint loc, uint maxLength) {
            // Read a function that ends in a hard branch (B) or exceeds maxLength instructions
            var func = new List<uint>();
            uint inst;

            image.Position = loc;

            do {
                inst = image.ReadUInt32();
                func.Add(inst);
            } while (!isB(inst) && func.Count < maxLength);

            return func;
        }

        // The method for ARM64:
        // - We want to extract values for CodeRegistration and MetadataRegistration from Il2CppCodegenRegistration(void)
        // - One of the functions supplied will be either Il2CppCodeGenRegistration or an initializer for Il2CppCodeGenRegistration.cpp
        // - The initializer (if present) loads a pointer to Il2CppCodegenRegistration in X1, if the function isn't in the function table
        // - Il2CppCodegenRegistration loads CodeRegistration into X0, MetadataRegistration into X1 and Il2CppCodeGenOptions into X2
        // - Loads can be done either with ADRP+ADD (loads the address of the wanted struct) or ADRP+LDR (loads a pointer to the address which must be de-referenced)
        // - Loads do not need to be pairs of sequential instructions
        // - We need to sweep the whole function from the ADRP to the next B to find an ADD or LDR with a corresponding register
        protected override (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc) {
            // Load function into memory
            // In practice, the longest function length we need is not generally longer than 7 instructions (0x1C bytes)
            var func = getFunctionAtFileOffset(image, loc, 7);

            // Don't accept functions longer than 7 instructions (in this case, the last instruction won't be a B)
            if (!isB(func[^1]))
                return (0, 0);

            // Get a list of registers and values in them at the end of the function
            var regs = sweepForAddressLoads(func, image.GlobalOffset + loc, image);

            // Is it the Il2CppCodeRegistration.cpp initializer?
            // X0-X1 will be set and they will be the only registers set
            if (regs.Count == 2 && regs.TryGetValue(0, out _) && regs.TryGetValue(1, out ulong x1)) {
                // Load up the function whose address is in X1
                func = getFunctionAtFileOffset(image, (uint) image.MapVATR(x1), 7);

                if (!isB(func[^1]))
                    return (0, 0);

                regs = sweepForAddressLoads(func, x1, image);
            }

            // Is it Il2CppCodegenRegistration(void)?
            // In v21 and later, X0-X2 will be set and they will be the only registers set
            // Pre-v21, X0-X1 will be the only registers set
            if (image.Version >= 21 && regs.Count == 3 && regs.TryGetValue(0, out ulong x0) && regs.TryGetValue(1, out x1) && regs.TryGetValue(2, out ulong _))
                return (x0, x1);

            if (image.Version < 21 && regs.Count == 2 && regs.TryGetValue(0, out x0) && regs.TryGetValue(1, out x1))
                return (x0, x1);

            return (0, 0);
        }
    }

    internal static class UIntExtensions
    {
        // Return count bits starting at bit low of integer x
        public static uint Bits(this uint x, int low, int count) => (x >> low) & (uint) ((1 << count) - 1);
    }
}