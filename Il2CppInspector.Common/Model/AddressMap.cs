/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Il2CppInspector.Model
{
    // A map of absolutely everything in the binary we know about
    // Designed to be used by static analysis disassembly frameworks such as Capstone.NET
    public class AddressMap
    {
        public Dictionary<ulong, object> Item { get; } = new Dictionary<ulong, object>();

        public AppModel Model { get; }

        public AddressMap(AppModel model) {
            Model = model;
            build();
        }

        private void build() {
            // TODO: Build address map
        }

        public object At(ulong addr) => Item.ContainsKey(addr)? Item[addr] : null;

        public bool TryAdd(ulong addr, object item) => Item.TryAdd(addr, item);
    }
}
