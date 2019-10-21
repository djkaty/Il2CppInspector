/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    public enum Elf : uint
    {
        // elf_header.m_dwFormat
        ELFMAG = 0x464c457f, // "\177ELF"

        // elf_header.e_machine
        EM_386 = 0x03,
        EM_ARM = 0x28,
        EM_X86_64 = 0x3E,
        EM_AARCH64 = 0xB7,

        // elf_header.m_arch
        ELFCLASS32 = 1,
        ELFCLASS64 = 2,

        // PHTs
        PT_DYNAMIC = 2,
        DT_PLTGOT = 3,

        // SHTs
        SHT_SYMTAB = 2,
        SHT_STRTAB = 3,
        SHT_RELA = 4,
        SHT_REL = 9,
        SHT_DYNSYM = 11,

        // dynamic sections
        DT_STRTAB = 5,
        DT_SYMTAB = 6,
        DT_RELA = 7,
        DT_RELASZ = 8,
        DT_RELAENT = 9,
        DT_REL = 17,
        DT_RELSZ = 18,
        DT_RELENT = 19,
        DT_INIT_ARRAY = 25,
        DT_INIT_ARRAYSZ = 27,

        // relocation types
        R_ARM_ABS32 = 2,
        R_ARM_REL32 = 3,
        R_ARM_COPY = 20,

        R_386_32 = 1,
        R_386_PC32 = 2,
        R_386_GLOB_DAT = 6,
        R_386_JMP_SLOT = 7,

        R_AMD64_64 = 1
    }

#pragma warning disable CS0649
    internal class elf_header
    {
        // 0x7f followed by ELF in ascii
        public uint m_dwFormat;

        // 1 - 32 bit
        // 2 - 64 bit
        public byte m_arch;

        // 1 - little endian
        // 2 - big endian
        public byte m_endian;

        // 1 is original elf format
        public byte m_version;

        // set based on OS, refer to OSABI enum
        public byte m_osabi;

        // refer to elf documentation
        public byte m_osabi_ver;

        // unused
        [ArrayLength(FixedSize=7)]
        public byte[] e_pad;//byte[7]

        // 1 - relocatable
        // 2 - executable
        // 3 - shared
        // 4 - core
        public ushort e_type;

        // refer to isa enum
        public ushort e_machine;

        public uint e_version;

        public uint e_entry;
        public uint e_phoff;
        public uint e_shoff;
        public uint e_flags;
        public ushort e_ehsize;
        public ushort e_phentsize;
        public ushort e_phnum;
        public ushort e_shentsize;
        public ushort e_shnum;
        public ushort e_shtrndx;
    }

    internal class elf_32_phdr
    {
        public uint p_type;
        public uint p_offset;
        public uint p_vaddr;
        public uint p_paddr;
        public uint p_filesz;
        public uint p_memsz;
        public uint p_flags;
        public uint p_align;
        //public byte[] p_data;忽略
    }

    internal class elf_32_shdr
    {
        public uint sh_name;
        public uint sh_type;
        public uint sh_flags;
        public uint sh_addr;
        public uint sh_offset;
        public uint sh_size;
        public uint sh_link;
        public uint sh_info;
        public uint sh_addralign;
        public uint sh_entsize;
    }

    internal class elf_32_sym
    {
        public uint st_name;
        public uint st_value;
        public uint st_size;
        public byte st_info;
        public byte st_other;
        public ushort st_shndx;
    }

    internal class elf_32_dynamic
    {
        public uint d_tag;
        public uint d_un;
    }

    internal class elf_32_rel
    {
        public uint r_offset;
        public uint r_info;
    }

    internal class elf_32_rela
    {
        public uint r_offset;
        public uint r_info;
        public uint r_addend;
    }
#pragma warning restore CS0649
}
