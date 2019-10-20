/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    public interface IFileFormatReader
    {
        BinaryObjectReader Stream { get; }
        uint NumImages { get; }
        IEnumerable<IFileFormatReader> Images { get; }
        IFileFormatReader this[uint index] { get; }
        long Position { get; set; }
        string Arch { get; }
        uint GlobalOffset { get; }
        Dictionary<string, uint> GetSymbolTable();
        uint[] GetFunctionTable();
        U ReadMappedObject<U>(uint uiAddr) where U : new();
        U[] ReadMappedArray<U>(uint uiAddr, int count) where U : new();
        uint MapVATR(uint uiAddr);

        byte[] ReadBytes(int count);
        ulong ReadUInt64();
        uint ReadUInt32();
        ushort ReadUInt16();
        byte ReadByte();
        string ReadMappedNullTerminatedString(uint uiAddr);
        List<U> ReadMappedObjectPointerArray<U>(uint uiAddr, int count) where U : new();
    }

    internal class FileFormatReader<T> : BinaryObjectReader, IFileFormatReader where T : FileFormatReader<T>
    {
        public FileFormatReader(Stream stream) : base(stream) { }

        public BinaryObjectReader Stream => this;

        public uint NumImages { get; protected set; } = 1;

        public uint GlobalOffset { get; protected set; }

        public virtual string Arch => throw new NotImplementedException();

        public IEnumerable<IFileFormatReader> Images {
            get {
                for (uint i = 0; i < NumImages; i++)
                    yield return this[i];
            }
        }

        public static T Load(string filename) {
            using (var stream = new FileStream(filename, FileMode.Open))
                return Load(stream);
        }

        public static T Load(Stream stream) {
            stream.Position = 0;
            var pe = (T) Activator.CreateInstance(typeof(T), stream);
            return pe.Init() ? pe : null;
        }

        // Confirm file is valid and set up RVA mappings
        protected virtual bool Init() => throw new NotImplementedException();

        // Choose a sub-binary within the image for multi-architecture binaries
        public virtual IFileFormatReader this[uint index] {
            get {
                if (index == 0)
                    return this;
                throw new IndexOutOfRangeException("Binary image index out of bounds");
            }
        }

        // Find search locations in the symbol table for Il2Cpp data
        public virtual Dictionary<string, uint> GetSymbolTable() => null;

        // Find search locations in the machine code for Il2Cpp data
        public virtual uint[] GetFunctionTable() => throw new NotImplementedException();

        // Map an RVA to an offset into the file image
        // No mapping by default
        public virtual uint MapVATR(uint uiAddr) => uiAddr;

        // Retrieve object(s) from specified RVA(s)
        public U ReadMappedObject<U>(uint uiAddr) where U : new() {
            return ReadObject<U>(MapVATR(uiAddr));
        }

        public U[] ReadMappedArray<U>(uint uiAddr, int count) where U : new() {
            return ReadArray<U>(MapVATR(uiAddr), count);
        }

        public string ReadMappedNullTerminatedString(uint uiAddr) {
            return ReadNullTerminatedString(MapVATR(uiAddr));
        }

        // Reads a list of pointers, then reads each object pointed to
        public List<U> ReadMappedObjectPointerArray<U>(uint uiAddr, int count) where U : new() {
            var pointers = ReadMappedArray<uint>(uiAddr, count);
            var array = new List<U>();
            for (int i = 0; i < count; i++)
                array.Add(ReadMappedObject<U>(pointers[i]));
            return array;
        }
    }
}