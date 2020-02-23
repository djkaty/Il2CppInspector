/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Il2CppInspector
{
    internal class Il2CppBinaryARM : Il2CppBinary
    {
        public Il2CppBinaryARM(IFileFormatReader stream) : base(stream) { }

        public Il2CppBinaryARM(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }

        // ARMv7-A Architecture Reference Manual: https://static.docs.arm.com/ddi0406/c/DDI0406C_C_arm_architecture_reference_manual.pdf

        // Section A8.8.7, page A8-312 (ADD encoding A1)
        private (uint reg_d, uint reg_n, uint reg_m)? getAddReg(uint inst) {
            if (inst.Bits(21, 11) != 0b_1110_0000_100)
                return null;

            var reg_d = inst.Bits(12, 4);
            var reg_n = inst.Bits(16, 4);
            var reg_m = inst.Bits(0, 4);
            return (reg_d, reg_n, reg_m);
        }

        // Section A8.8.64, page A8-410 (LDR encoding A1)
        private (uint reg_t, ushort imm)? getLdrLit(uint inst) {
            if (inst.Bits(16, 16) != 0b_1110_0101_1001_1111)
                return null;

            var reg_t = inst.Bits(12, 4);
            var imm12 = inst.Bits(0, 12);
            return (reg_t, (ushort) imm12);
        }

        // Section A8.8.66, page A8-414 (LDR encoding A1)
        // Specifically LDR Rx, [Ry, Rz]
        private (uint reg_t, uint reg_n, uint reg_m)? getLdrReg(uint inst) {
            if (inst.Bits(20, 12) != 0b_1110_0111_1001)
                return null;

            var reg_n = inst.Bits(16, 4);
            var reg_t = inst.Bits(12, 4);
            var reg_m = inst.Bits(0, 4);
            return (reg_t, reg_n, reg_m);
        }

        // Section A8.8.18, page A8-334 (B encoding A1)
        private bool isB(uint inst) => inst.Bits(24, 8) == 0b_1110_1010;

        // Thumb 2 Supplement Reference Manual: http://class.ece.iastate.edu/cpre288/resources/docs/Thumb-2SupplementReferenceManual.pdf

        // Page 4-166 (MOVS encoding T1, MOVW encoding T3), Page 4-171 (MOVT)
        // In Thumb, an 8-byte MOV instruction is MOVW followed by MOVT
        private enum Thumb : uint
        {
            MovW = 0b100100,
            MovT = 0b101100
        }

        // Section 3.1
        private uint getNextThumbInstruction(IFileFormatReader image) {
            // Assume 16-bit
            uint inst = image.ReadUInt16();

            // Is 32-bit?
            if (inst.Bits(13, 15) == 0b111)
                if (inst.Bits(11, 2) != 0b00)
                    inst = (inst << 16) + image.ReadUInt16();

            return inst;
        }

        private (uint reg_d, ushort imm)? getThumbMovImm(uint inst, Thumb movType) {
            uint reg_d, imm;

            // Encoding T1
            if (inst.Bits(11, 21) == 0b00100 && movType == Thumb.MovW) {
                imm = inst.Bits(0, 8);
                reg_d = inst.Bits(8, 3);
                return (reg_d, (ushort) imm);
            }

            // Encoding T3
            if (inst.Bits(20, 6) != (uint) movType || inst.Bits(27, 5) != 0b11110 || inst.Bits(15, 1) != 0)
                return null;

            imm = (inst.Bits(16, 4) << 12) + (inst.Bits(26, 1) << 11) + (inst.Bits(12, 3) << 8) + inst.Bits(0, 8);
            reg_d = inst.Bits(8, 4);
            return (reg_d, (ushort) imm);
        }

        // Section 4.6.4 (ADD encoding T2)
        private (uint reg_dn, uint reg_m)? getThumbAddReg(uint inst) {
            if (inst.Bits(8, 8) != 0b_0100_0100)
                return null;

            var reg_dn = (inst.Bits(7, 1) << 3) + inst.Bits(0, 3);
            var reg_m = inst.Bits(3, 4);
            return (reg_dn, reg_m);
        }

        // Section 4.6.43 (LDR encoding T1)
        private (uint reg_n, uint reg_t, ushort imm)? getThumbLdrImm(uint inst) {
            if (inst.Bits(11, 5) != 0b_01101)
                return null;

            var reg_n = inst.Bits(3, 3);
            var reg_t = inst.Bits(0, 3);
            var imm = inst.Bits(6, 5);
            return (reg_n, reg_t, (ushort) imm);
        }

        // Section 4.6.12 (B.W encoding T4; for encoding T3, flip bit 12)
        private bool isBW(uint inst) => inst.Bits(27, 5) == 0b11110 && inst.Bits(14, 2) == 0b10 && inst.Bits(12, 1) == 1;

        // Sweep a Thumb function and return the register values at the end (register number => value)
        private Dictionary<uint, uint> sweepThumbForAddressLoads(List<uint> func, uint baseAddress, IFileFormatReader image) {
            // List of registers and addresses loaded into them
            var regs = new Dictionary<uint, uint>();

            // Program counter is R15 in ARM
            // https://www.scss.tcd.ie/~waldroj/3d1/arm_arm.pdf states:
            // For a Thumb instruction, the value read is the address of the instruction plus 4 bytes
            regs.Add(15, baseAddress + 4);

            // Iterate each instruction
            foreach (var inst in func) {

                var accepted = false;

                // Is it a MOVW?
                if (getThumbMovImm(inst, Thumb.MovW) is (uint movw_reg_d, ushort movw_imm)) {
                    if (regs.ContainsKey(movw_reg_d))
                        regs[movw_reg_d] = movw_imm; // Clears top 16 bits
                    else
                        regs.Add(movw_reg_d, movw_imm);

                    accepted = true;
                }

                // Is it a MOVT?
                if (getThumbMovImm(inst, Thumb.MovT) is (uint movt_reg_d, ushort movt_imm)) {
                    if (regs.ContainsKey(movt_reg_d))
                        regs[movt_reg_d] |= (uint) movt_imm << 16;
                    else
                        regs.Add(movt_reg_d, (uint) movt_imm << 16);

                    accepted = true;
                }

                // Is it a pointer de-reference (LDR Rt, [Rn, #imm])?
                if (getThumbLdrImm(inst) is (uint ldr_reg_n, uint ldr_reg_t, ushort ldr_imm)) {
                    // The code below works in the generic case for all Rt, Rn and #imm,
                    // but for our scan we want to restrict it such that Rt == Rn and #imm == 0
                    // otherwise we might pick up functions we don't want
                    if (ldr_reg_n == ldr_reg_t && ldr_imm == 0)

                        if (regs.ContainsKey(ldr_reg_n)) {
                            var offset = (regs[ldr_reg_n] & 0xffff_fffe) + ldr_imm;
                            var value = image.ReadUInt32(image.MapVATR(offset));
                            if (regs.ContainsKey(ldr_reg_t))
                                regs[ldr_reg_t] = value;
                            else
                                regs.Add(ldr_reg_t, value);

                            accepted = true;
                        }
                }

                // Is it an ADD Rdn, Rm?
                if (getThumbAddReg(inst) is (uint add_reg_dn, uint add_reg_m)) {
                    if (regs.ContainsKey(add_reg_dn) && regs.ContainsKey(add_reg_m)) {
                        regs[add_reg_dn] += regs[add_reg_m];

                        accepted = true;
                    }
                }

                // is it the end?
                if (isBW(inst))
                    accepted = true;

                // In our scan, we will ONLY accept one of the above instructions
                if (!accepted)
                    return null;

                // Advance program counter which we need to calculate ADDs with PC as operand correctly
                regs[15] += inst.Bits(29, 3) == 0b111 ? 4u : 2u;
            }
            return regs;
        }

        // Get a Thumb function that ends in B.W
        private List<uint> getThumbFunctionAtFileOffset(IFileFormatReader image, uint loc, uint maxLength) {
            // Read a function that ends in a hard branch (B.W) or exceeds maxLength instructions
            var func = new List<uint>();
            uint inst;

            image.Position = loc;

            do {
                inst = getNextThumbInstruction(image);
                func.Add(inst);
            } while (!isBW(inst) && func.Count < maxLength);

            return func;
        }

        protected override (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc) {
            // Assembly bytes to search for at start of each function
            ulong metadataRegistration, codeRegistration;

            // ARMv7 based on ELF GOT
            // Il2CppCodeRegistration.cpp initializer
            image.Position = loc;

            var buff = image.ReadBytes(0xc);
            // LDR R0, [PC, #0x1C]; LDR R1, [PC, #0x1C]; LDR R2, [PC, #0x1C]
            if (new byte[] { 0x1c, 0x0, 0x9f, 0xe5, 0x1c, 0x10, 0x9f, 0xe5, 0x1c, 0x20, 0x9f, 0xe5 }.SequenceEqual(buff)) {

                // Get offset to all addresses
                // The +8 is because in ARM, PC always contains the currently executing instruction + 8
                var offset = image.ReadUInt32(loc + 0x24) + loc + 0xc + 8;

                // Get pointer to Il2CppCodegenRegistration(void)
                var pCgr = image.ReadUInt32(loc + 0x2C) + offset;

                // Read pointer table at end of function
                codeRegistration = image.ReadUInt32(pCgr + 0x28) + offset;
                var pMetadataRegistration = image.ReadUInt32(pCgr + 0x2C) + offset;
                metadataRegistration = image.ReadUInt32(pMetadataRegistration);
                return (codeRegistration, metadataRegistration);
            }

            // ARMv7
            // Il2CppCodeRegistration.cpp initializer
            image.Position = loc;
            buff = image.ReadBytes(0x18);

            // Check for ADD R0, PC, R0; ADD R1, PC, R1 near the end of the function
            if (new byte[] {0x00, 0x00, 0x8F, 0xE0, 0x01, 0x10, 0x8F, 0xE0}.SequenceEqual(buff.Skip(0x10))

                // Check for LDR R1, [PC, #x] where x is an offset to *Il2CppCodegenRegistration
                && new byte[] {0x10, 0x9F, 0xE5}.SequenceEqual(buff.Skip(0x9).Take(3))) {

                // Read offset in LDR operand plus pointer table at end of function to find pCgr
                var pCgr = buff[8] + loc + 0x10;
                image.Position = pCgr;
                pCgr = image.ReadUInt32() + loc + 0x1c;

                // void Il2CppCodegenRegistration()
                // This function must take the form:
                // - three LDR of R0-R2, to pointers at the end of the function
                // - three ADD/LDR, either ADD Rx, PC, Rx or LDR Rx, [PC, Rx] for pointer to pointer
                // - B
                // R0 = CodeRegistration, R1 = MetadataRegistration, R2 = Il2CppCodeGenOptions

                var insts = image.Stream.ReadArray<uint>(pCgr, 10); // 7 instructions + 3 pointers
                var ldrOffsets = new uint[3];
                var pointers = new uint[3];

                // Confirm the 7th final instruction is an unconditional branch
                var okToContinue = isB(insts[6]);
                
                // Fetch the LDR values for R0-R2 as the first three instructions
                // PC points to 8 bytes after the current instruction
                for (var i = 0; i <= 2 && okToContinue; i++) {
                    var ldr = getLdrLit(insts[i]);
                    if (ldr != null && ldr.Value.reg_t <= 2)
                        ldrOffsets[ldr.Value.reg_t] = (uint) i*4 + ldr.Value.imm + 8;
                    else
                        okToContinue = false;
                }

                // Fetch the ADDs or LDRs to determine which are pointers
                // and which are pointers to pointers
                for (var i = 3; i <= 5 && okToContinue; i++) {
                    var add = getAddReg(insts[i]);
                    var ldr = getLdrReg(insts[i]);
                    if (add != null && add.Value.reg_n == 15 && add.Value.reg_d == add.Value.reg_m && add.Value.reg_d <= 2) {
                        pointers[add.Value.reg_d] = pCgr + (uint) i*4 + insts[ldrOffsets[add.Value.reg_d] / 4] + 8;
                    }
                    else if (ldr != null && ldr.Value.reg_n == 15 && ldr.Value.reg_t == ldr.Value.reg_m && ldr.Value.reg_t <= 2) {
                        var p = pCgr + (uint) i * 4 + insts[ldrOffsets[ldr.Value.reg_t] / 4] + 8;
                        pointers[ldr.Value.reg_t] = image.ReadUInt32(image.MapVATR(p));
                    }
                    else
                        okToContinue = false;
                }

                if (okToContinue)
                    return (pointers[0], pointers[1]);
            }

            // Thumb-2
            // We use a method similar to the linear sweep in Il2CppBinaryARM64; see the comments there for details
            loc &= 0xffff_fffe;
            image.Position = loc;

            // Load function into memory
            // In practice, the longest function length we need is not generally longer than 11 instructions
            var func = getThumbFunctionAtFileOffset(image, loc, 11);

            // Don't accept functions longer than 10 instructions (in this case, the last instruction won't be a B.W)
            if (!isBW(func[^1]))
                return (0, 0);

            // Get a list of registers and values in them at the end of the function
            var regs = sweepThumbForAddressLoads(func, (uint) image.GlobalOffset + loc, image);
            if (regs == null)
                return (0, 0);

            uint r0, r1;

            // Is it the Il2CppCodeRegistration.cpp initializer?
            // R0-R3 + PC will be set and they will be the only registers set
            // R2 and R3 must be zero
            if (regs.Count() == 5 && regs.TryGetValue(0, out _) && regs.TryGetValue(1, out r1)
                && regs.TryGetValue(2, out uint r2) && regs.TryGetValue(3, out uint r3)) {

                if (r2 == 0 && r3 == 0) {
                    // Load up the function whose address is in R1
                    func = getThumbFunctionAtFileOffset(image, image.MapVATR(r1 & 0xffff_fffe), 11);

                    if (!isBW(func[^1]))
                        return (0, 0);

                    regs = sweepThumbForAddressLoads(func, r1 & 0xffff_fffe, image);
                }
            }

            // Is it Il2CppCodegenRegistration(void)?
            // In v21 and later, R0-R2 + PC will be set and they will be the only registers set
            // Pre-v21, R0-R1 + PC will be the only registers set

            if (image.Version >= 21 && regs.Count == 4 && regs.TryGetValue(0, out r0) && regs.TryGetValue(1, out r1) && regs.TryGetValue(2, out uint _))
                return (r0 & 0xffff_fffe, r1 & 0xffff_fffe);

            if (image.Version < 21 && regs.Count == 3 && regs.TryGetValue(0, out r0) && regs.TryGetValue(1, out r1))
                return (r0 & 0xffff_fffe, r1 & 0xffff_fffe);

            return (0, 0);
        }
    }
}