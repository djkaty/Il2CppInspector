/*
    Copyright 2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

namespace Il2CppInspector
{
    internal class Il2CppBinaryARM64 : Il2CppBinary
    {
        public Il2CppBinaryARM64(IFileFormatReader stream) : base(stream) { }

        public Il2CppBinaryARM64(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }

        private (uint reg, ulong page) getAdrp(uint inst, ulong pc) {
            if ((inst.Bits(24, 8) & 0b_1000_1111) != 1 << 7)
                return (0, 0);

            var addendLo = inst.Bits(29, 2);
            var addendHi = inst.Bits(5, 19);
            var addend = (addendHi << 14) + (addendLo << 12);
            var page = pc & ~((1Lu << 12) - 1);
            var reg = inst.Bits(0, 5);

            return (reg, page + addend);
        }

        private (uint reg_n, uint reg_d, uint imm) getAdd64(uint inst) {
            if (inst.Bits(22, 10) != 0b_1001_0001_00)
                return (0, 0, 0);

            var imm = inst.Bits(10, 12);
            var reg_n = inst.Bits(5, 5);
            var reg_d = inst.Bits(0, 5);

            return (reg_n, reg_d, imm);
        }

        private ulong getAddressLoad(IFileFormatReader image, uint loc) {
            // Get candidate ADRP Xa, #PAGE instruction
            var inst = image.ReadUInt32(loc);

            var adrp = getAdrp(inst, loc);
            if (adrp.page == 0)
                return 0;

            // Get candidate ADD Xb, Xc, #OFFSET instruction
            inst = image.ReadUInt32();

            var add64 = getAdd64(inst);
            if (add64.imm == 0)
                return 0;

            // Confirm a == b == c
            if (adrp.reg != add64.reg_d || add64.reg_d != add64.reg_n)
                return 0;

            return adrp.page + add64.imm;
        }

        protected override (ulong, ulong) ConsiderCode(IFileFormatReader image, uint loc) {

            var codeRegistration = getAddressLoad(image, loc);
            if (codeRegistration == 0)
                return (0, 0);

            var metadataRegistration = getAddressLoad(image, loc + 8);
            if (metadataRegistration == 0)
                return (0, 0);

            // There should be an Il2CppCodeGenOptions address load after the above two
            if (getAddressLoad(image, loc + 16) == 0)
                return (0, 0);

            // TODO: Verify loc + 24 is a hard branch (B)

            return (image.GlobalOffset + codeRegistration, image.GlobalOffset + metadataRegistration);
        }
    }

    internal static class UIntExtensions
    {
        // Return count bits starting at bit low of integer x
        public static uint Bits(this uint x, int low, int count) => (x >> low) & (uint) ((1 << count) - 1);
    }
}