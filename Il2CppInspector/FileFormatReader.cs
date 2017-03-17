/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.IO;
using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    public interface IFileFormatReader
    {
        BinaryObjectReader Stream { get; }
        long Position { get; set; }
        string Arch { get; }
        uint GlobalOffset { get; }
        uint[] GetSearchLocations();
        U ReadMappedObject<U>(uint uiAddr) where U : new();
        U[] ReadMappedArray<U>(uint uiAddr, int count) where U : new();
        uint MapVATR(uint uiAddr);

        byte[] ReadBytes(int count);
        ulong ReadUInt64();
        uint ReadUInt32();
        ushort ReadUInt16();
        byte ReadByte();
    }

    internal class FileFormatReader<T> : BinaryObjectReader, IFileFormatReader where T : FileFormatReader<T>
    {
        public FileFormatReader(Stream stream) : base(stream) { }

        public BinaryObjectReader Stream => this;

        public uint GlobalOffset { get; protected set; }

        public virtual string Arch => throw new NotImplementedException();

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

        // Find search locations in the machine code for Il2Cpp data
        public virtual uint[] GetSearchLocations() => throw new NotImplementedException();
        
        // Map an RVA to an offset into the file image
        public virtual uint MapVATR(uint uiAddr) => throw new NotImplementedException();

        // Retrieve object(s) from specified RVA(s)
        public U ReadMappedObject<U>(uint uiAddr) where U : new() {
            return ReadObject<U>(MapVATR(uiAddr));
        }

        public U[] ReadMappedArray<U>(uint uiAddr, int count) where U : new() {
            return ReadArray<U>(MapVATR(uiAddr), count);
        }
    }
}