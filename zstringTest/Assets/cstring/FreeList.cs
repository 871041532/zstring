using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public class FreeList<T> : IList<T>, IList, ICollection
{
    T[] _items;
    int _size;
    int _version;

    static readonly T[] EmptyArray = new T[0];
    static ArrayPool<T> pool = new ArrayPool<T>();    

    public FreeList()
    {
        _items = EmptyArray;
    }

    public FreeList(IEnumerable<T> collection)
    {
        CheckCollection(collection);        
        ICollection<T> c = collection as ICollection<T>;

        if (c == null)
        {
            _items = EmptyArray;
            AddEnumerable(collection);
        }
        else
        {
            _items = pool.Alloc(c.Count);
            AddCollection(c);
        }
    }

    public FreeList(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException("capacity");
        }

        _items = pool.Alloc(capacity);
    }

    internal FreeList(T[] data, int size)
    {
        _items = data;
        _size = size;
    }

    public void Dispose()
    {
        if (_items != EmptyArray)
        {
            pool.Collect(_items);
        }

        _items = EmptyArray;
        _size = 0;
    }

    public void Add(T item)
    {                
        if (_size == _items.Length)
        {
            GrowIfNeeded(1);
        }

        _items[_size++] = item;
        _version++;
    }

    void GrowIfNeeded(int newCount)
    {
        int newSize = _size + newCount;

        if (newSize > _items.Length)
        {
            T[] a = pool.Alloc(newSize);
            Array.Copy(_items, a, _size);
            pool.Collect(_items);
            _items = a;            
        }            
    }

    void CheckRange(int idx, int count)
    {
        if (idx < 0)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        if ((uint)idx + (uint)count > (uint)_size)
        {
            throw new ArgumentException("index and count exceed length of list");
        }
    }

    void AddCollection(ICollection<T> collection)
    {
        int collectionCount = collection.Count;

        if (collectionCount == 0)
        {
            return;
        }

        GrowIfNeeded(collectionCount);
        collection.CopyTo(_items, _size);
        _size += collectionCount;
    }

    void AddEnumerable(IEnumerable<T> enumerable)
    {
        foreach (T t in enumerable)
        {
            Add(t);
        }
    }

    public void AddRange(IEnumerable<T> collection)
    {
        CheckCollection(collection);
        ICollection<T> c = collection as ICollection<T>;

        if (c != null)
        {
            AddCollection(c);
        }
        else
        {
            AddEnumerable(collection);
        }

        _version++;
    }

    public void AddRange(FreeList<T> list, int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        GrowIfNeeded(length);        
        Array.Copy(list._items, 0, _items, _size, length);
        _size += length;
    }

    public void AddRange(T[] array, int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        GrowIfNeeded(length);
        Array.Copy(array, 0, _items, _size, length);
        _size += length;
    }

    public ReadOnlyCollection<T> AsReadOnly()
    {
        return new ReadOnlyCollection<T>(this);
    }

    public int BinarySearch(T item)
    {
        return Array.BinarySearch<T>(_items, 0, _size, item);
    }

    public int BinarySearch(T item, IComparer<T> comparer)
    {
        return Array.BinarySearch<T>(_items, 0, _size, item, comparer);
    }

    public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
    {
        CheckRange(index, count);
        return Array.BinarySearch<T>(_items, index, count, item, comparer);
    }

    public void Clear()
    {
        //Array.Clear(_items, 0, _items.Length);
        _size = 0;
        _version++;
    }

    public bool Contains(T item)
    {
        return Array.IndexOf<T>(_items, item, 0, _size) != -1;
    }

    public FreeList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
    {
        if (converter == null)
        {
            throw new ArgumentNullException("converter");
        }

        FreeList<TOutput> u = new FreeList<TOutput>(_size);

        for (int i = 0; i < _size; i++)
        {
            u._items[i] = converter(_items[i]);
        }

        u._size = _size;
        return u;
    }

    public void CopyTo(T[] array)
    {
        Array.Copy(_items, 0, array, 0, _size);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(_items, 0, array, arrayIndex, _size);
    }

    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
        CheckRange(index, count);
        Array.Copy(_items, index, array, arrayIndex, count);
    }

    public bool Exists(Predicate<T> match)
    {
        CheckMatch(match);
        return GetIndex(0, _size, match) != -1;
    }

    public T Find(Predicate<T> match)
    {
        CheckMatch(match);
        int i = GetIndex(0, _size, match);
        return (i != -1) ? _items[i] : default(T);
    }

    static void CheckMatch(Predicate<T> match)
    {
        if (match == null)
        {
            throw new ArgumentNullException("match");
        }
    }

    public FreeList<T> FindAll(Predicate<T> match)
    {
        CheckMatch(match);

        if (this._size <= 0x10000) // <= 8 * 1024 * 8 (8k in stack)
        {
            return FindAllStackBits(match);
        }
        else
        {
            return FindAllList(match);
        }
    }

    protected FreeList<T> FindAllStackBits(Predicate<T> match)
    {
        unsafe
        {
            uint* bits = stackalloc uint[(this._size / 32) + 1];
            uint* ptr = bits;
            int found = 0;
            uint bitmask = 0x80000000;

            for (int i = 0; i < this._size; i++)
            {
                if (match(this._items[i]))
                {
                    (*ptr) = (*ptr) | bitmask;
                    found++;
                }

                bitmask = bitmask >> 1;

                if (bitmask == 0)
                {
                    ptr++;
                    bitmask = 0x80000000;
                }
            }

            T[] results = pool.Alloc(found);
            bitmask = 0x80000000;
            ptr = bits;
            int j = 0;

            for (int i = 0; i < this._size && j < found; i++)
            {
                if (((*ptr) & bitmask) == bitmask)
                {
                    results[j++] = this._items[i];
                }

                bitmask = bitmask >> 1;

                if (bitmask == 0)
                {
                    ptr++;
                    bitmask = 0x80000000;
                }
            }

            return new FreeList<T>(results, found);
        }
    }

    protected FreeList<T> FindAllList(Predicate<T> match)
    {
        FreeList<T> results = new FreeList<T>();

        for (int i = 0; i < this._size; i++)
        {
            if (match(_items[i]))
            {
                results.Add(_items[i]);
            }
        }

        return results;
    }

    public int FindIndex(Predicate<T> match)
    {
        CheckMatch(match);
        return GetIndex(0, _size, match);
    }

    public int FindIndex(int startIndex, Predicate<T> match)
    {
        CheckMatch(match);
        CheckIndex(startIndex);
        return GetIndex(startIndex, _size - startIndex, match);
    }

    public int FindIndex(int startIndex, int count, Predicate<T> match)
    {
        CheckMatch(match);
        CheckRange(startIndex, count);
        return GetIndex(startIndex, count, match);
    }

    int GetIndex(int startIndex, int count, Predicate<T> match)
    {
        int end = startIndex + count;

        for (int i = startIndex; i < end; i++)
        {
            if (match(_items[i]))
            {
                return i;
            }
        }

        return -1;
    }

    public T FindLast(Predicate<T> match)
    {
        CheckMatch(match);
        int i = GetLastIndex(0, _size, match);
        return i == -1 ? default(T) : this[i];
    }

    public int FindLastIndex(Predicate<T> match)
    {
        CheckMatch(match);
        return GetLastIndex(0, _size, match);
    }

    public int FindLastIndex(int startIndex, Predicate<T> match)
    {
        CheckMatch(match);
        CheckIndex(startIndex);
        return GetLastIndex(0, startIndex + 1, match);
    }

    public int FindLastIndex(int startIndex, int count, Predicate<T> match)
    {
        CheckMatch(match);
        int start = startIndex - count + 1;
        CheckRange(start, count);
        return GetLastIndex(start, count, match);
    }

    int GetLastIndex(int startIndex, int count, Predicate<T> match)
    {
        // unlike FindLastIndex, takes regular params for search range
        for (int i = startIndex + count; i != startIndex;)
        {
            if (match(_items[--i]))
            {
                return i;
            }
        }

        return -1;
    }

    public void ForEach(Action<T> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException("action");
        }

        for (int i = 0; i < _size; i++)
        {
            action(_items[i]);
        }
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public FreeList<T> GetRange(int index, int count)
    {
        CheckRange(index, count);
        T[] tmpArray = pool.Alloc(count);
        Array.Copy(_items, index, tmpArray, 0, count);
        return new FreeList<T>(tmpArray, count);
    }

    public int IndexOf(T item)
    {
        return Array.IndexOf<T>(_items, item, 0, _size);
    }

    public int IndexOf(T item, int index)
    {
        CheckIndex(index);
        return Array.IndexOf<T>(_items, item, index, _size - index);
    }

    public int IndexOf(T item, int index, int count)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        if ((uint)index + (uint)count > (uint)_size)
        {
            throw new ArgumentOutOfRangeException("index and count exceed length of list");
        }

        return Array.IndexOf<T>(_items, item, index, count);
    }

    void Shift(int start, int delta)
    {
        if (delta < 0)
        {
            start -= delta;
        }

        if (start < _size)
        {
            Array.Copy(_items, start, _items, start + delta, _size - start);
        }

        _size += delta;

        if (delta < 0)
        {
            Array.Clear(_items, _size, -delta);
        }        
    }

    void CheckIndex(int index)
    {
        if (index < 0 || (uint)index > (uint)_size)
        {
            throw new ArgumentOutOfRangeException("index");
        }
    }

    public void Insert(int index, T item)
    {
        CheckIndex(index);

        if (_size == _items.Length)
        {
            GrowIfNeeded(1);
        }

        Shift(index, 1);
        _items[index] = item;
        _version++;
    }

    void CheckCollection(IEnumerable<T> collection)
    {
        if (collection == null)
        {
            throw new ArgumentNullException("collection");
        }
    }

    public void InsertRange(int index, IEnumerable<T> collection)
    {
        CheckCollection(collection);
        CheckIndex(index);

        if (collection == this)
        {
            T[] buffer = pool.Alloc(_size);
            CopyTo(buffer, 0);
            GrowIfNeeded(_size);
            Shift(index, buffer.Length);
            Array.Copy(buffer, 0, _items, index, buffer.Length);
            pool.Collect(buffer);
        }
        else
        {
            ICollection<T> c = collection as ICollection<T>;

            if (c != null)
            {
                InsertCollection(index, c);
            }
            else
            {
                InsertEnumeration(index, collection);
            }
        }

        _version++;
    }

    void InsertCollection(int index, ICollection<T> collection)
    {
        int collectionCount = collection.Count;
        GrowIfNeeded(collectionCount);

        Shift(index, collectionCount);
        collection.CopyTo(_items, index);
    }

    void InsertEnumeration(int index, IEnumerable<T> enumerable)
    {
        foreach (T t in enumerable)
        {
            Insert(index++, t);
        }
    }

    public int LastIndexOf(T item)
    {
        return Array.LastIndexOf<T>(_items, item, _size - 1, _size);
    }

    public int LastIndexOf(T item, int index)
    {
        CheckIndex(index);
        return Array.LastIndexOf<T>(_items, item, index, index + 1);
    }

    public int LastIndexOf(T item, int index, int count)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException("index", index, "index is negative");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count", count, "count is negative");
        }

        if (index - count + 1 < 0)
        {
            throw new ArgumentOutOfRangeException("cound", count, "count is too large");
        }

        return Array.LastIndexOf<T>(_items, item, index, count);
    }

    public bool Remove(T item)
    {
        int loc = IndexOf(item);

        if (loc != -1)
        {
            RemoveAt(loc);
        }

        return loc != -1;
    }

    public int RemoveAll(Predicate<T> match)
    {
        CheckMatch(match);
        int i = 0;
        int j = 0;

        // Find the first item to remove
        for (i = 0; i < _size; i++)
        {
            if (match(_items[i]))
            {
                break;
            }
        }

        if (i == _size)
        {
            return 0;
        }

        _version++;

        // Remove any additional items
        for (j = i + 1; j < _size; j++)
        {
            if (!match(_items[j]))
            {
                _items[i++] = _items[j];
            }
        }

        if (j - i > 0)
        {
            Array.Clear(_items, i, j - i);
        }

        _size = i;
        return (j - i);
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || (uint)index >= (uint)_size)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        Shift(index, -1);        
        _version++;
    }

    public T Pop()
    {
        if (_size <= 0)
        {
            throw new InvalidOperationException();
        }

        T poped = _items[--_size];
        _items[_size] = default(T);
        _version++;
        return poped;
    }

    public void RemoveRange(int index, int count)
    {
        CheckRange(index, count);

        if (count > 0)
        {
            Shift(index, -count);            
            _version++;
        }
    }

    public void Reverse()
    {
        Array.Reverse(_items, 0, _size);
        _version++;
    }

    public void Reverse(int index, int count)
    {
        CheckRange(index, count);
        Array.Reverse(_items, index, count);
        _version++;
    }

    public void Sort()
    {
        Array.Sort<T>(_items, 0, _size, Comparer<T>.Default);
        _version++;
    }

    public void Sort(IComparer<T> comparer)
    {
        Array.Sort<T>(_items, 0, _size, comparer);
        _version++;
    }

    public void Sort(Comparison<T> comparison)
    {
        if (comparison == null)
        {
            throw new ArgumentNullException("comparison");
        }

        if (_size <= 1 || _items.Length <= 1)
        {
            return;
        }

        try
        {
            int low0 = 0;
            int high0 = _size - 1;
            qsort(_items, low0, high0, comparison);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Comparison threw an exception.", e);
        }
        
        _version++;
    }

    private static void qsort(T[] array, int low0, int high0, Comparison<T> comparison)
    {
        if (low0 >= high0)
        {
            return;
        }

        int low = low0;
        int high = high0;

        // Be careful with overflows
        int mid = low + ((high - low) / 2);
        T keyPivot = array[mid];

        while (true)
        {
            // Move the walls in
            while (low < high0 && comparison(array[low], keyPivot) < 0)
                ++low;
            while (high > low0 && comparison(keyPivot, array[high]) < 0)
                --high;

            if (low <= high)
            {
                swap(array, low, high);
                ++low;
                --high;
            }
            else
                break;
        }

        if (low0 < high)
            qsort(array, low0, high, comparison);
        if (low < high0)
            qsort(array, low, high0, comparison);
    }

    private static void swap(T[] array, int i, int j)
    {
        T tmp = array[i];
        array[i] = array[j];
        array[j] = tmp;
    }

    public void Sort(int index, int count, IComparer<T> comparer)
    {
        CheckRange(index, count);
        Array.Sort<T>(_items, index, count, comparer);
        _version++;
    }

    public T[] ToArray()
    {
        T[] t = new T[_size];
        Array.Copy(_items, t, _size);

        return t;
    }

    public T[] ToArray2()
    {
        if (_size < _items.Length / 2)
        {
            T[] newList = pool.Alloc(_size);            
            Array.Copy(_items, newList, _size);
            pool.Collect(_items);
            _items = newList;
        }

        return _items;
    }

    public bool TrueForAll(Predicate<T> match)
    {
        CheckMatch(match);

        for (int i = 0; i < _size; i++)
        {
            if (!match(_items[i]))
            {
                return false;
            }
        }

        return true;
    }

    public int Count
    {
        get { return _size; }
    }

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_size)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return _items[index];
        }
        set
        {
            CheckIndex(index);

            if ((uint)index == (uint)_size)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            _items[index] = value;
        }
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    void ICollection.CopyTo(Array array, int arrayIndex)
    {
        Array.Copy(_items, 0, array, arrayIndex, _size);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    int IList.Add(object item)
    {
        try
        {
            Add((T)item);
            return _size - 1;
        }
        catch (NullReferenceException)
        {
        }
        catch (InvalidCastException)
        {
        }
        throw new ArgumentException("item");
    }

    bool IList.Contains(object item)
    {
        try
        {
            return Contains((T)item);
        }
        catch (NullReferenceException)
        {
        }
        catch (InvalidCastException)
        {
        }
        return false;
    }

    int IList.IndexOf(object item)
    {
        try
        {
            return IndexOf((T)item);
        }
        catch (NullReferenceException)
        {
        }
        catch (InvalidCastException)
        {
        }
        return -1;
    }

    void IList.Insert(int index, object item)
    {
        CheckIndex(index);

        try
        {
            Insert(index, (T)item);
            return;
        }
        catch (NullReferenceException)
        {
        }
        catch (InvalidCastException)
        {
        }
        throw new ArgumentException("item");
    }

    void IList.Remove(object item)
    {
        try
        {
            Remove((T)item);
            return;
        }
        catch (NullReferenceException)
        {
        }
        catch (InvalidCastException)
        {
        }
    }

    bool ICollection<T>.IsReadOnly
    {
        get { return false; }
    }

    bool ICollection.IsSynchronized
    {
        get { return false; }
    }

    object ICollection.SyncRoot
    {
        get { return this; }
    }

    bool IList.IsFixedSize
    {
        get { return false; }
    }


    bool IList.IsReadOnly
    {
        get { return false; }
    }

    object IList.this[int index]
    {
        get { return this[index]; }
        set
        {
            try
            {
                this[index] = (T)value;
                return;
            }
            catch (NullReferenceException)
            {
                // can happen when 'value' is null and T is a valuetype
            }
            catch (InvalidCastException)
            {
            }
            throw new ArgumentException("value");
        }
    }

    [Serializable]
    public struct Enumerator : IEnumerator<T>, IDisposable
    {
        FreeList<T> l;
        int next;
        int ver;

        T current;

        internal Enumerator(FreeList<T> l)
            : this()
        {
            this.l = l;
            ver = l._version;
        }

        public void Dispose()
        {
            l = null;
        }

        void VerifyState()
        {
            if (l == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (ver != l._version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }
        }

        public bool MoveNext()
        {
            VerifyState();

            if (next < 0)
            {
                return false;
            }

            if (next < l._size)
            {
                current = l._items[next++];
                return true;
            }

            next = -1;
            return false;
        }

        public T Current
        {
            get
            {
                return current;
            }
        }

        void IEnumerator.Reset()
        {
            VerifyState();
            next = 0;
        }

        object IEnumerator.Current
        {
            get
            {
                VerifyState();

                if (next <= 0)
                {
                    throw new InvalidOperationException();
                }

                return current;
            }
        }
    }
}
