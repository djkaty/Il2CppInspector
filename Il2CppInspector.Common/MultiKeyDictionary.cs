// *************************************************
// Created by Aron Weiler
// Feel free to use this code in any way you like, 
// just don't blame me when your coworkers think you're awesome.
// Comments?  Email aronweiler@gmail.com
// Revision 1.6
// Revised locking strategy based on the some bugs found with the existing lock objects. 
// Possible deadlock and race conditions resolved by swapping out the two lock objects for a ReaderWriterLockSlim. 
// Performance takes a very small hit, but correctness is guaranteed.
// *************************************************

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Aron.Weiler
{
	/// <summary>
	/// Multi-Key Dictionary Class
	/// </summary>	
	/// <typeparam name="K">Primary Key Type</typeparam>
	/// <typeparam name="L">Sub Key Type</typeparam>
	/// <typeparam name="V">Value Type</typeparam>
	public class MultiKeyDictionary<K, L, V>
	{
		internal readonly Dictionary<K, V> baseDictionary = new Dictionary<K, V>();
		internal readonly Dictionary<L, K> subDictionary = new Dictionary<L, K>();
		internal readonly Dictionary<K, L> primaryToSubkeyMapping = new Dictionary<K, L>();

		ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();

		public V this[L subKey]
		{
			get
			{
				V item;
				if (TryGetValue(subKey, out item))
					return item;

				throw new KeyNotFoundException("sub key not found: " + subKey.ToString());
			}
		}

		public V this[K primaryKey]
		{
			get
			{
				V item;
				if (TryGetValue(primaryKey, out item))
					return item;

				throw new KeyNotFoundException("primary key not found: " + primaryKey.ToString());
			}
		}

		public void Associate(L subKey, K primaryKey)
		{
			readerWriterLock.EnterUpgradeableReadLock();

			try
			{
				if (!baseDictionary.ContainsKey(primaryKey))
					throw new KeyNotFoundException(string.Format("The base dictionary does not contain the key '{0}'", primaryKey));

				if (primaryToSubkeyMapping.ContainsKey(primaryKey)) // Remove the old mapping first
				{
					readerWriterLock.EnterWriteLock();

					try
					{
						if (subDictionary.ContainsKey(primaryToSubkeyMapping[primaryKey]))
						{
							subDictionary.Remove(primaryToSubkeyMapping[primaryKey]);
						}

						primaryToSubkeyMapping.Remove(primaryKey);
					}
					finally
					{
						readerWriterLock.ExitWriteLock();
					}
				}

				subDictionary[subKey] = primaryKey;
				primaryToSubkeyMapping[primaryKey] = subKey;
			}
			finally
			{
				readerWriterLock.ExitUpgradeableReadLock();
			}
		}

		public bool TryGetValue(L subKey, out V val)
		{
			val = default(V);

			K primaryKey;

			readerWriterLock.EnterReadLock();

			try
			{
				if (subDictionary.TryGetValue(subKey, out primaryKey))
				{
					return baseDictionary.TryGetValue(primaryKey, out val);
				}
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}

			return false;
		}

		public bool TryGetValue(K primaryKey, out V val)
		{
			readerWriterLock.EnterReadLock();

			try
			{
				return baseDictionary.TryGetValue(primaryKey, out val);
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		}

		public bool ContainsKey(L subKey)
		{
			V val;

			return TryGetValue(subKey, out val);
		}

		public bool ContainsKey(K primaryKey)
		{
			V val;

			return TryGetValue(primaryKey, out val);
		}

		public void Remove(K primaryKey)
		{
			readerWriterLock.EnterWriteLock();

			try
			{
				if (primaryToSubkeyMapping.ContainsKey(primaryKey))
				{
					if (subDictionary.ContainsKey(primaryToSubkeyMapping[primaryKey]))
					{
						subDictionary.Remove(primaryToSubkeyMapping[primaryKey]);
					}

					primaryToSubkeyMapping.Remove(primaryKey);
				}

				baseDictionary.Remove(primaryKey);
			}
			finally
			{
				readerWriterLock.ExitWriteLock();
			}
		}

		public void Remove(L subKey)
		{
			readerWriterLock.EnterWriteLock();

			try
			{
				baseDictionary.Remove(subDictionary[subKey]);

				primaryToSubkeyMapping.Remove(subDictionary[subKey]);

				subDictionary.Remove(subKey);
			}
			finally
			{
				readerWriterLock.ExitWriteLock();
			}
		}

		public void Add(K primaryKey, V val)
		{
			readerWriterLock.EnterWriteLock();

			try
			{
				baseDictionary.Add(primaryKey, val);
			}
			finally
			{
				readerWriterLock.ExitWriteLock();
			}
		}

		public void Add(K primaryKey, L subKey, V val)
		{
			Add(primaryKey, val);

			Associate(subKey, primaryKey);
		}

		public V[] CloneValues()
		{
			readerWriterLock.EnterReadLock();

			try
			{
				V[] values = new V[baseDictionary.Values.Count];

				baseDictionary.Values.CopyTo(values, 0);

				return values;
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		}

		public List<V> Values
		{
			get
			{
				readerWriterLock.EnterReadLock();

				try
				{
					return baseDictionary.Values.ToList();
				}
				finally
				{
					readerWriterLock.ExitReadLock();
				}
			}
		}

		public K[] ClonePrimaryKeys()
		{
			readerWriterLock.EnterReadLock();

			try
			{
				K[] values = new K[baseDictionary.Keys.Count];

				baseDictionary.Keys.CopyTo(values, 0);

				return values;
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		}

		public L[] CloneSubKeys()
		{
			readerWriterLock.EnterReadLock();

			try
			{
				L[] values = new L[subDictionary.Keys.Count];

				subDictionary.Keys.CopyTo(values, 0);

				return values;
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		}

		public void Clear()
		{
			readerWriterLock.EnterWriteLock();

			try
			{
				baseDictionary.Clear();

				subDictionary.Clear();

				primaryToSubkeyMapping.Clear();
			}
			finally
			{
				readerWriterLock.ExitWriteLock();
			}
		}

		public int Count
		{
			get
			{
				readerWriterLock.EnterReadLock();

				try
				{
					return baseDictionary.Count;
				}
				finally
				{
					readerWriterLock.ExitReadLock();
				}
			}
		}

		public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
		{
			readerWriterLock.EnterReadLock();

			try
			{
				return baseDictionary.GetEnumerator();
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		}
	}
}
