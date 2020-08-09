/*
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    public interface IFileFormatReader
    {
        BinaryObjectReader Stream { get; }
        double Version { get; set; }
        long Length { get; }
        uint NumImages { get; }
        IEnumerable<IFileFormatReader> Images { get; }
        IFileFormatReader this[uint index] { get; }
        long Position { get; set; }
        string Format { get; }
        string Arch { get; }
        int Bits { get; }
        ulong GlobalOffset { get; } // The virtual address where the code section (.text) would be loaded in memory
        ulong ImageBase { get; } // The virtual address of where the image would be loaded in memory (same as GlobalOffset except for PE)
        Dictionary<string, Symbol> GetSymbolTable();
        uint[] GetFunctionTable();
        IEnumerable<Export> GetExports();

        uint MapVATR(ulong uiAddr);
        bool TryMapVATR(ulong uiAddr, out uint fileOffset);
        ulong MapFileOffsetToVA(uint offset);
        bool TryMapFileOffsetToVA(uint offset, out ulong va);

        byte ReadByte();
        byte ReadByte(long uiAddr);
        byte[] ReadBytes(int count);
        byte[] ReadBytes(long uiAddr, int count);
        bool ReadBoolean();
        bool ReadBoolean(long uiAddr);
        long ReadInt64();
        long ReadInt64(long uiAddr);
        int ReadInt32();
        int ReadInt32(long uiAddr);
        short ReadInt16();
        short ReadInt16(long uiAddr);
        ulong ReadUInt64();
        ulong ReadUInt64(long uiAddr);
        uint ReadUInt32();
        uint ReadUInt32(long uiAddr);
        ushort ReadUInt16();
        ushort ReadUInt16(long uiAddr);
        U ReadObject<U>() where U : new();
        U ReadObject<U>(long uiAddr) where U : new();
        U[] ReadArray<U>(int count) where U : new();
        U[] ReadArray<U>(long uiAddr, int count) where U : new();
        string ReadNullTerminatedString(Encoding encoding = null);
        string ReadNullTerminatedString(long uiAddr, Encoding encoding = null);
        string ReadFixedLengthString(int length, Encoding encoding = null);
        string ReadFixedLengthString(long uiAddr, int length, Encoding encoding = null);

        long ReadWord();
        long ReadWord(long uiAddr);
        long[] ReadWordArray(int count);
        long[] ReadWordArray(long uiAddr, int count);

        byte ReadMappedByte(ulong uiAddr);
        byte[] ReadMappedBytes(ulong uiAddr, int count);
        bool ReadMappedBoolean(ulong uiAddr);
        long ReadMappedInt64(ulong uiAddr);
        int ReadMappedInt32(ulong uiAddr);
        short ReadMappedInt16(ulong uiAddr);
        ulong ReadMappedUInt64(ulong uiAddr);
        uint ReadMappedUInt32(ulong uiAddr);
        ushort ReadMappedUInt16(ulong uiAddr);
        U ReadMappedObject<U>(ulong uiAddr) where U : new();
        U[] ReadMappedArray<U>(ulong uiAddr, int count) where U : new();
        string ReadMappedNullTerminatedString(ulong uiAddr, Encoding encoding = null);
        string ReadMappedFixedLengthString(ulong uiAddr, int length, Encoding encoding = null);

        long ReadMappedWord(ulong uiAddr);
        long[] ReadMappedWordArray(ulong uiAddr, int count);
        List<U> ReadMappedObjectPointerArray<U>(ulong uiAddr, int count) where U : new();

        EventHandler<string> OnStatusUpdate { get; set; }
    }

    public class FileFormatReader
    {
        // Helper method to try all defined file formats when the contents of the binary is unknown
        public static IFileFormatReader Load(string filename, EventHandler<string> statusCallback = null) => Load(new FileStream(filename, FileMode.Open, FileAccess.Read), statusCallback);

        public static IFileFormatReader Load(Stream stream, EventHandler<string> statusCallback = null) {
            var types = Assembly.GetExecutingAssembly().DefinedTypes
                        .Where(x => x.ImplementedInterfaces.Contains(typeof(IFileFormatReader)) && !x.IsGenericTypeDefinition);

            foreach (var type in types) {
                try {
                    if (type.GetMethod("Load", BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public,
                            null, new[] {typeof(Stream), typeof(EventHandler<string>)}, null)
                        .Invoke(null, new object[] {stream, statusCallback}) is IFileFormatReader loaded)
                        return loaded;
                }
                catch (TargetInvocationException ex) {
                    throw ex.InnerException;
                }
            }
            return null;
        }
    }

    public abstract class FileFormatReader<T> : BinaryObjectReader, IFileFormatReader where T : FileFormatReader<T>
    {
        public FileFormatReader(Stream stream) : base(stream) { }

        public BinaryObjectReader Stream => this;

        public long Length => BaseStream.Length;

        public uint NumImages { get; protected set; } = 1;

        public ulong GlobalOffset { get; protected set; }

        public virtual ulong ImageBase => GlobalOffset;

        public virtual string Format => throw new NotImplementedException();

        public virtual string Arch => throw new NotImplementedException();

        public virtual int Bits => throw new NotImplementedException();

        public EventHandler<string> OnStatusUpdate { get; set; }

        protected void StatusUpdate(string status) => OnStatusUpdate?.Invoke(this, status);

        public IEnumerable<IFileFormatReader> Images {
            get {
                for (uint i = 0; i < NumImages; i++)
                    yield return this[i];
            }
        }

        public static T Load(string filename, EventHandler<string> statusCallback = null) {
            using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            return Load(stream, statusCallback);
        }

        public static T Load(Stream stream, EventHandler<string> statusCallback = null) {
            // Copy the original stream in case we modify it
            var ms = new MemoryStream();

            if (stream.CanSeek)
                stream.Position = 0;
            stream.CopyTo(ms);
            
            ms.Position = 0;
            var pe = (T) Activator.CreateInstance(typeof(T), ms);
            return pe.InitImpl(statusCallback) ? pe : null;
        }

        private bool InitImpl(EventHandler<string> statusCallback = null) {
            OnStatusUpdate = statusCallback;
            return Init();
        }

        // Confirm file is valid and set up RVA mappings
        protected virtual bool Init() => throw new NotImplementedException();

        // Choose a sub-binary within the image for multi-architecture binaries
        public virtual IFileFormatReader this[uint index] => (index == 0)? this : throw new IndexOutOfRangeException("Binary image index out of bounds");

        // Find search locations in the symbol table for Il2Cpp data
        public virtual Dictionary<string, Symbol> GetSymbolTable() => new Dictionary<string, Symbol>();

        // Find search locations in the machine code for Il2Cpp data
        public virtual uint[] GetFunctionTable() => throw new NotImplementedException();

        // Find all symbol exports for the image
        public virtual IEnumerable<Export> GetExports() => null;

        // Map an RVA to an offset into the file image
        // No mapping by default
        public virtual uint MapVATR(ulong uiAddr) => (uint) uiAddr;

        // Try to map an RVA to an offset in the file image
        public bool TryMapVATR(ulong uiAddr, out uint fileOffset) {
            try {
                fileOffset = MapVATR(uiAddr);
                return true;
            } catch (InvalidOperationException) {
                fileOffset = 0;
                return false;
            }
        }

        // Map an offset into the file image to an RVA
        // No mapping by default
        public virtual ulong MapFileOffsetToVA(uint offset) => offset;

        // Try to map an offset into the file image to an RVA
        public bool TryMapFileOffsetToVA(uint offset, out ulong va) {
            try {
                va = MapFileOffsetToVA(offset);
                return true;
            }
            catch (InvalidOperationException) {
                va = 0;
                return false;
            }
        }

        // Read a file format dependent word (32 or 64 bits)
        // The primitive mappings in Bin2Object will automatically read a uint if the file is 32-bit
        public long ReadWord() => ReadObject<long>();
        public long ReadWord(long uiAddr) => ReadObject<long>(uiAddr);
        public long[] ReadWordArray(int count) => ReadArray<long>(count);
        public long[] ReadWordArray(long uiAddr, int count) => ReadArray<long>(uiAddr, count);

        // Retrieve items from specified RVA(s)
        public byte ReadMappedByte(ulong uiAddr) => ReadByte(MapVATR(uiAddr));
        public byte[] ReadMappedBytes(ulong uiAddr, int count) => ReadBytes(MapVATR(uiAddr), count);
        public bool ReadMappedBoolean(ulong uiAddr) => ReadBoolean(MapVATR(uiAddr));
        public long ReadMappedInt64(ulong uiAddr) => ReadInt64(MapVATR(uiAddr));
        public int ReadMappedInt32(ulong uiAddr) => ReadInt32(MapVATR(uiAddr));
        public short ReadMappedInt16(ulong uiAddr) => ReadInt16(MapVATR(uiAddr));
        public ulong ReadMappedUInt64(ulong uiAddr) => ReadUInt64(MapVATR(uiAddr));
        public uint ReadMappedUInt32(ulong uiAddr) => ReadUInt32(MapVATR(uiAddr));
        public ushort ReadMappedUInt16(ulong uiAddr) => ReadUInt16(MapVATR(uiAddr));

        public U ReadMappedObject<U>(ulong uiAddr) where U : new() => ReadObject<U>(MapVATR(uiAddr));
        public U[] ReadMappedArray<U>(ulong uiAddr, int count) where U : new() => ReadArray<U>(MapVATR(uiAddr), count);
        public string ReadMappedNullTerminatedString(ulong uiAddr, Encoding encoding = null) => ReadNullTerminatedString(MapVATR(uiAddr), encoding);
        public string ReadMappedFixedLengthString(ulong uiAddr, int length, Encoding encoding = null) => ReadFixedLengthString(MapVATR(uiAddr), length, encoding);

        // Read a file format dependent array of words (32 or 64 bits)
        // The primitive mappings in Bin2Object will automatically read a uint if the file is 32-bit
        public long ReadMappedWord(ulong uiAddr) => ReadWord(MapVATR(uiAddr));
        public long[] ReadMappedWordArray(ulong uiAddr, int count) => ReadArray<long>(MapVATR(uiAddr), count);

        // Reads a list of pointers, then reads each object pointed to
        public List<U> ReadMappedObjectPointerArray<U>(ulong uiAddr, int count) where U : new() {
            var pointers = ReadMappedArray<ulong>(uiAddr, count);
            var array = new List<U>();
            for (int i = 0; i < count; i++)
                array.Add(ReadMappedObject<U>(pointers[i]));
            return array;
        }
    }
}