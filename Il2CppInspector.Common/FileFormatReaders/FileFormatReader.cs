/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        ulong GlobalOffset { get; }
        Dictionary<string, ulong> GetSymbolTable();
        uint[] GetFunctionTable();
        IEnumerable<Export> GetExports();
        U ReadMappedObject<U>(ulong uiAddr) where U : new();
        U[] ReadMappedArray<U>(ulong uiAddr, int count) where U : new();
        long[] ReadMappedWordArray(ulong uiAddr, int count);
        uint MapVATR(ulong uiAddr);

        byte[] ReadBytes(int count);
        ulong ReadUInt64();
        ulong ReadUInt64(long uiAddr);
        uint ReadUInt32();
        uint ReadUInt32(long uiAddr);
        ushort ReadUInt16();
        ushort ReadUInt16(long uiAddr);
        byte ReadByte();
        byte ReadByte(long uiAddr);
        long ReadWord();
        long ReadWord(long uiAddr);
        U ReadObject<U>() where U : new();
        string ReadMappedNullTerminatedString(ulong uiAddr);
        List<U> ReadMappedObjectPointerArray<U>(ulong uiAddr, int count) where U : new();
        event EventHandler<string> OnStatusUpdate;
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

        public virtual string Format => throw new NotImplementedException();

        public virtual string Arch => throw new NotImplementedException();

        public virtual int Bits => throw new NotImplementedException();

        public event EventHandler<string> OnStatusUpdate;

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
            stream.Position = 0;
            stream.CopyTo(ms);
            
            ms.Position = 0;
            var pe = (T) Activator.CreateInstance(typeof(T), ms);
            return pe.InitImpl(statusCallback) ? pe : null;
        }

        private bool InitImpl(EventHandler<string> statusCallback = null) {
            OnStatusUpdate += statusCallback;
            return Init();
        }

        // Confirm file is valid and set up RVA mappings
        protected virtual bool Init() => throw new NotImplementedException();

        // Choose a sub-binary within the image for multi-architecture binaries
        public virtual IFileFormatReader this[uint index] => (index == 0)? this : throw new IndexOutOfRangeException("Binary image index out of bounds");

        // Find search locations in the symbol table for Il2Cpp data
        public virtual Dictionary<string, ulong> GetSymbolTable() => null;

        // Find search locations in the machine code for Il2Cpp data
        public virtual uint[] GetFunctionTable() => throw new NotImplementedException();

        // Find all symbol exports for the image
        public virtual IEnumerable<Export> GetExports() => null;

        // Map an RVA to an offset into the file image
        // No mapping by default
        public virtual uint MapVATR(ulong uiAddr) => (uint) uiAddr;

        // Read a file format dependent word (32 or 64 bits)
        // The primitive mappings in Bin2Object will automatically read a uint if the file is 32-bit
        public long ReadWord() => ReadObject<long>();
        public long ReadWord(long uiAddr) => ReadObject<long>(uiAddr);

        // Retrieve object(s) from specified RVA(s)
        public U ReadMappedObject<U>(ulong uiAddr) where U : new() => ReadObject<U>(MapVATR(uiAddr));

        public U[] ReadMappedArray<U>(ulong uiAddr, int count) where U : new() => ReadArray<U>(MapVATR(uiAddr), count);

        // Read a file format dependent array of words (32 or 64 bits)
        // The primitive mappings in Bin2Object will automatically read a uint if the file is 32-bit
        public long[] ReadMappedWordArray(ulong uiAddr, int count) => ReadArray<long>(MapVATR(uiAddr), count);

        public string ReadMappedNullTerminatedString(ulong uiAddr) => ReadNullTerminatedString(MapVATR(uiAddr));

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