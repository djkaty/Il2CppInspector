/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Il2CppInspector.Model
{
    // A map of absolutely everything in the binary we know about
    // Designed to be used by static analysis disassembly frameworks such as Capstone.NET
    public class AddressMap : IDictionary<ulong, object>
    {
        // IL2CPP application model
        public AppModel Model { get; }

        // Underlying collection
        public Dictionary<ulong, object> Items { get; } = new Dictionary<ulong, object>();

        #region Surrogate implementation of IDictionary

        // Surrogate implementation of IDictionary
        public ICollection<ulong> Keys => ((IDictionary<ulong, object>) Items).Keys;
        public ICollection<object> Values => ((IDictionary<ulong, object>) Items).Values;
        public int Count => ((ICollection<KeyValuePair<ulong, object>>) Items).Count;
        public bool IsReadOnly => ((ICollection<KeyValuePair<ulong, object>>) Items).IsReadOnly;
        public object this[ulong key] { get => ((IDictionary<ulong, object>) Items)[key]; set => ((IDictionary<ulong, object>) Items)[key] = value; }

        public bool TryAdd(ulong addr, object item) => Items.TryAdd(addr, item);
        public void Add(ulong key, object value) => ((IDictionary<ulong, object>) Items).Add(key, value);
        public bool ContainsKey(ulong key) => ((IDictionary<ulong, object>) Items).ContainsKey(key);
        public bool Remove(ulong key) => ((IDictionary<ulong, object>) Items).Remove(key);
        public bool TryGetValue(ulong key, out object value) => ((IDictionary<ulong, object>) Items).TryGetValue(key, out value);
        public void Add(KeyValuePair<ulong, object> item) => ((ICollection<KeyValuePair<ulong, object>>) Items).Add(item);
        public void Clear() => ((ICollection<KeyValuePair<ulong, object>>) Items).Clear();
        public bool Contains(KeyValuePair<ulong, object> item) => ((ICollection<KeyValuePair<ulong, object>>) Items).Contains(item);
        public void CopyTo(KeyValuePair<ulong, object>[] array, int arrayIndex) => ((ICollection<KeyValuePair<ulong, object>>) Items).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<ulong, object> item) => ((ICollection<KeyValuePair<ulong, object>>) Items).Remove(item);
        public IEnumerator<KeyValuePair<ulong, object>> GetEnumerator() => ((IEnumerable<KeyValuePair<ulong, object>>) Items).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) Items).GetEnumerator();
        #endregion

        public AddressMap(AppModel model) {
            Model = model;
            build();
        }

        private void build() {
            // TODO: Build address map
        }
    }
}
