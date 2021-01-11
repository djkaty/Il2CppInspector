/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

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
    public interface IFileFormatStream
    {
        double Version { get; set; }
        long Length { get; }
        uint NumImages { get; }
        string DefaultFilename { get; }
        bool IsModified { get; internal set; } 
        IEnumerable<IFileFormatStream> Images { get; } // Each child image of this object (eg. 32/64-bit versions in Fat MachO file)
        IFileFormatStream this[uint index] { get; } // With no additional override, one object = one file, this[0] == this
        long Position { get; set; }
        string Format { get; }
        string Arch { get; }
        int Bits { get; }
        ulong GlobalOffset { get; } // The virtual address where the code section (.text) would be loaded in memory
        ulong ImageBase { get; } // The virtual address of where the image would be loaded in memory (same as GlobalOffset except for PE)
        IEnumerable<IFileFormatStream> TryNextLoadStrategy(); // Some images can be loaded multiple ways, eg. default, packed
        Dictionary<string, Symbol> GetSymbolTable();
        uint[] GetFunctionTable();
        IEnumerable<Export> GetExports();
        IEnumerable<Section> GetSections();
        bool TryGetSections(out IEnumerable<Section> sections);

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

        void WriteEndianBytes(byte[] bytes);
        void Write(long int64);
        void Write(ulong uint64);
        void Write(int int32);
        void Write(uint uint32);
        void Write(short int16);
        void Write(ushort uint16);
        void Write(long addr, byte[] bytes);
        void Write(long addr, long int64);
        void Write(long addr, ulong uint64);
        void Write(long addr, int int32);
        void Write(long addr, uint uint32);
        void Write(long addr, short int16);
        void Write(long addr, ushort uint16);
        void Write(long addr, byte value);
        void Write(long addr, bool value);
        void WriteObject<T>(long addr, T obj);
        void WriteObject<T>(T obj);
        void WriteArray<T>(long addr, T[] array);
        void WriteArray<T>(T[] array);
        void WriteNullTerminatedString(long addr, string str, Encoding encoding = null);
        void WriteNullTerminatedString(string str, Encoding encoding = null);
        void WriteFixedLengthString(long addr, string str, int size = -1, Encoding encoding = null);
        void WriteFixedLengthString(string str, int size = -1, Encoding encoding = null);

        EventHandler<string> OnStatusUpdate { get; set; }

        public void AddPrimitiveMapping(Type objType, Type streamType);
        public void CopyTo(Stream stream);
    }

    public class FileFormatStream
    {
        // Helper method to try all defined file formats when the contents of the binary is unknown
        public static IFileFormatStream Load(string filename, LoadOptions loadOptions = null, EventHandler<string> statusCallback = null)
            => Load(new FileStream(filename, FileMode.Open, FileAccess.Read), loadOptions, statusCallback);

        public static IFileFormatStream Load(Stream stream, LoadOptions loadOptions = null, EventHandler<string> statusCallback = null) {
            var types = Assembly.GetExecutingAssembly().DefinedTypes
                        .Where(x => x.ImplementedInterfaces.Contains(typeof(IFileFormatStream))
                                && !x.IsGenericTypeDefinition && !x.IsAbstract && !x.IsInterface);

            // Copy to memory-based stream
            var binaryObjectStream = new BinaryObjectStream();
            stream.Position = 0;
            stream.CopyTo(binaryObjectStream);
            binaryObjectStream.Position = 0;

            // Plugin hook to pre-process image before we determine its format
            var preProcessResult = PluginHooks.PreProcessImage(binaryObjectStream);

            foreach (var type in types) {
                try {
                    if (type.GetMethod("Load", BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public,
                            null, new[] { typeof(BinaryObjectStream), typeof(LoadOptions), typeof(EventHandler<string>) }, null)
                        .Invoke(null, new object[] { binaryObjectStream, loadOptions, statusCallback }) is IFileFormatStream loaded) {

                        loaded.IsModified |= preProcessResult.IsStreamModified;
                        return loaded;
                    }
                }
                catch (TargetInvocationException ex) {
                    throw ex.InnerException;
                }
            }
            return null;
        }
    }

    public abstract class FileFormatStream<T> : BinaryObjectStream, IFileFormatStream where T : FileFormatStream<T>
    {
        public abstract string DefaultFilename { get; }

        bool IFileFormatStream.IsModified { get => IsModified; set => IsModified = value; }
        public bool IsModified { get; protected set; } = false;

        public uint NumImages { get; protected set; } = 1;

        public ulong GlobalOffset { get; protected set; }

        public virtual ulong ImageBase => GlobalOffset;

        public virtual string Format => throw new NotImplementedException();

        public virtual string Arch => throw new NotImplementedException();

        public virtual int Bits => throw new NotImplementedException();

        // Extra parameters to be passed to a loader
        protected LoadOptions LoadOptions;

        public EventHandler<string> OnStatusUpdate { get; set; }

        protected void StatusUpdate(string status) => OnStatusUpdate?.Invoke(this, status);

        public IEnumerable<IFileFormatStream> Images {
            get {
                for (uint i = 0; i < NumImages; i++)
                    yield return this[i];
            }
        }

        public static T Load(string filename, LoadOptions loadOptions = null, EventHandler<string> statusCallback = null) {
            return Load(new BinaryObjectStream(File.ReadAllBytes(filename)), loadOptions, statusCallback);
        }

        public static T Load(Stream stream, LoadOptions loadOptions = null, EventHandler<string> statusCallback = null) {
            var binary = (T) Activator.CreateInstance(typeof(T));
            if (stream.CanSeek)
                stream.Position = 0;
            stream.CopyTo(binary);
            return binary.InitImpl(loadOptions, statusCallback) ? binary : null;
        }

        private bool InitImpl(LoadOptions loadOptions, EventHandler<string> statusCallback) {
            LoadOptions = loadOptions;
            OnStatusUpdate = statusCallback;
            Position = 0;

            if (Init()) {
                // Call post-process plugin hook if load succeeded
                IsModified |= PluginHooks.PostProcessImage(this).IsStreamModified;
                return true;
            }
            return false;
        }

        // Confirm file is valid and set up RVA mappings
        protected virtual bool Init() => throw new NotImplementedException();

        // Choose an image within the file for multi-architecture binaries
        public virtual IFileFormatStream this[uint index] => (index == 0)? this : throw new IndexOutOfRangeException("Binary image index out of bounds");

        // For images that can be loaded and then tested with Il2CppBinary in multiple ways, get the next possible version of the image
        public virtual IEnumerable<IFileFormatStream> TryNextLoadStrategy() { yield return this; }

        // Find search locations in the symbol table for Il2Cpp data
        public virtual Dictionary<string, Symbol> GetSymbolTable() => new Dictionary<string, Symbol>();

        // Find search locations in the machine code for Il2Cpp data
        public virtual uint[] GetFunctionTable() => throw new NotImplementedException();

        // Find all symbol exports for the image
        public virtual IEnumerable<Export> GetExports() => null;

        // Get all sections for the image in a universal format
        public virtual IEnumerable<Section> GetSections() => throw new NotImplementedException();

        public bool TryGetSections(out IEnumerable<Section> sections) {
            try {
                sections = GetSections();
                return true;
            } catch (NotImplementedException) {
                sections = null;
                return false;
            }
        }

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
        public byte[] ReadMappedBytes(ulong uiAddr, int count) => count > 0? ReadBytes(MapVATR(uiAddr), count) : new byte[0];
        public bool ReadMappedBoolean(ulong uiAddr) => ReadBoolean(MapVATR(uiAddr));
        public long ReadMappedInt64(ulong uiAddr) => ReadInt64(MapVATR(uiAddr));
        public int ReadMappedInt32(ulong uiAddr) => ReadInt32(MapVATR(uiAddr));
        public short ReadMappedInt16(ulong uiAddr) => ReadInt16(MapVATR(uiAddr));
        public ulong ReadMappedUInt64(ulong uiAddr) => ReadUInt64(MapVATR(uiAddr));
        public uint ReadMappedUInt32(ulong uiAddr) => ReadUInt32(MapVATR(uiAddr));
        public ushort ReadMappedUInt16(ulong uiAddr) => ReadUInt16(MapVATR(uiAddr));

        public U ReadMappedObject<U>(ulong uiAddr) where U : new() => ReadObject<U>(MapVATR(uiAddr));
        public U[] ReadMappedArray<U>(ulong uiAddr, int count) where U : new() => count > 0 ? ReadArray<U>(MapVATR(uiAddr), count) : new U[0];
        public string ReadMappedNullTerminatedString(ulong uiAddr, Encoding encoding = null) => ReadNullTerminatedString(MapVATR(uiAddr), encoding);
        public string ReadMappedFixedLengthString(ulong uiAddr, int length, Encoding encoding = null) => ReadFixedLengthString(MapVATR(uiAddr), length, encoding);

        // Read a file format dependent array of words (32 or 64 bits)
        // The primitive mappings in Bin2Object will automatically read a uint if the file is 32-bit
        public long ReadMappedWord(ulong uiAddr) => ReadWord(MapVATR(uiAddr));
        public long[] ReadMappedWordArray(ulong uiAddr, int count) => count > 0 ? ReadArray<long>(MapVATR(uiAddr), count) : new long[0];

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