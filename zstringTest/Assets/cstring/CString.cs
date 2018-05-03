using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

[Serializable]
public class CString : IDisposable
{
    const char DEFAULT_ALLOC_CHAR = (char)0xCCCC;
    const int DEFAULT_CAPACITY = 256;

    static ArrayPool<char> pool = new ArrayPool<char>();
    static Queue<CString> queue = new Queue<CString>();
    static Queue<CStringBlock> blocks = new Queue<CStringBlock>();
    static Stack<CStringBlock> stack = new Stack<CStringBlock>();

    static CStringBlock currentBlock = null;
    static string NewLine = Environment.NewLine;

    [NonSerialized]
    char[] _buffer;
    [NonSerialized]
    int length = 0;

    bool beDisposed = false;    

    internal class CStringBlock : IStringBlock
    {
        List<CString> list;
        bool beDisposed = false;

        public CStringBlock()
        {
            list = new List<CString>();            
        }

        public void Init()
        {
            beDisposed = false;
        }

        public void Push(CString str)
        {
            list.Add(str);
        }

        public bool Remove(CString str)
        {
            return list.Remove(str);
        }

        public void Dispose()
        {
            if (beDisposed)
            {
                return;
            }

            if (this != currentBlock)
            {
                throw new Exception("dispose in it's own block");
            }

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Dispose();
            }

            list.Clear();
            blocks.Enqueue(this);
            stack.Pop();
            currentBlock = stack.Count > 0 ? stack.Peek() : null;      
            beDisposed = true;
        }
    }

    private static readonly char[] WhiteChars =
    {
        (char) 0x9, (char) 0xA, (char) 0xB, (char) 0xC, (char) 0xD,
        (char) 0x85, (char) 0x1680, (char) 0x2028, (char) 0x2029,
        (char) 0x20, (char) 0xA0, (char) 0x2000, (char) 0x2001, (char) 0x2002, (char) 0x2003, (char) 0x2004,
        (char) 0x2005, (char) 0x2006, (char) 0x2007, (char) 0x2008, (char) 0x2009, (char) 0x200A, (char) 0x200B,
        (char) 0x3000, (char) 0xFEFF,
    };

    public int Capacity
    {
        get
        {
            return _buffer.Length;
        }
    }

    public CString(int count)
    {        
        if (currentBlock != null)
        {
            _buffer = pool.Alloc(count);
            currentBlock.Push(this);
        }
        else
        {
            _buffer = new char[pool.NextPowerOfTwo(count)];
        }

        ClearBuffer(_buffer);
    }

    public CString(string str)
    {
        if (currentBlock != null)
        {
            _buffer = pool.Alloc(str.Length);
            currentBlock.Push(this);
        }
        else
        {
            _buffer = new char[pool.NextPowerOfTwo(str.Length)];
        }

        CopyFromString(str);        
    }

    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException("Capacity must be greater than 0.");
        }

        if (capacity <= _buffer.Length)
        {
            return _buffer.Length;
        }

        char[] tmp = pool.Alloc(capacity);
        CharCopy(tmp, _buffer, length);
        pool.Collect(_buffer);
        _buffer = tmp;
        return capacity;
    }

    CString()
    {
        _buffer = null;
        length = 0;
    }

    static public CString Alloc(int size)
    {
        CString str = null;

        if (queue.Count > 0 && currentBlock != null)
        {
            str = queue.Dequeue();
        }
        else
        {
            str = new CString();            
        }

        if (currentBlock != null)
        {
            str._buffer = pool.Alloc(size);
            currentBlock.Push(str);
        }
        else
        {
            str._buffer = new char[pool.NextPowerOfTwo(size)];
        }

        str.beDisposed = false;
        str.length = 0;
        return str;
    }

    public void Dispose()
    {
        if (beDisposed)
        {
            return;
        }

        beDisposed = true;

        if (currentBlock != null)        
        {
            pool.Collect(_buffer);
            queue.Enqueue(this);
        }
        
        _buffer = null;
        length = 0;        
    }

    static public IStringBlock Block()
    {
        CStringBlock cb = null;

        if (blocks.Count != 0)
        {
            cb = blocks.Dequeue();
        }
        else
        {
            cb = new CStringBlock();
        }

        cb.Init();
        stack.Push(cb);
        currentBlock = cb;
        return cb;
    }

    static public bool IsBlocking()
    {
        return currentBlock != null;
    }

    unsafe void CopyFromString(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        fixed (char* s1 = _buffer, s2 = str)
        {
            CharCopy(s1, s2, str.Length);
            length = str.Length;
        }
    }

    unsafe void ClearBuffer(char[] buffer)
    {
        fixed (char* p = buffer)
        {
            memset((byte*)p, 0xcc, sizeof(char) * buffer.Length);
        }
    }

    public void Clear()
    {
        length = 0;
    }

    public static unsafe bool Equals(CString a, CString b)
    {        
        object l = a as object;
        object r = b as object;

        if (l == r)
        {
            return true;
        }

        if (l == null || r == null)
        {
            return false;
        }

        int len = a.length;

        if (len != b.length)
        {
            return false;
        }

        fixed (char* s1 = a._buffer, s2 = b._buffer)
        {
            char* s1_ptr = s1;
            char* s2_ptr = s2;

            while (len >= 8)
            {
                if (((int*)s1_ptr)[0] != ((int*)s2_ptr)[0] || ((int*)s1_ptr)[1] != ((int*)s2_ptr)[1] ||
                    ((int*)s1_ptr)[2] != ((int*)s2_ptr)[2] || ((int*)s1_ptr)[3] != ((int*)s2_ptr)[3])
                {
                    return false;
                }

                s1_ptr += 8;
                s2_ptr += 8;
                len -= 8;
            }

            if (len >= 4)
            {
                if (((int*)s1_ptr)[0] != ((int*)s2_ptr)[0] || ((int*)s1_ptr)[1] != ((int*)s2_ptr)[1])
                {
                    return false;
                }

                s1_ptr += 4;
                s2_ptr += 4;
                len -= 4;
            }

            if (len > 1)
            {
                if (((int*)s1_ptr)[0] != ((int*)s2_ptr)[0])
                {
                    return false;
                }

                s1_ptr += 2;
                s2_ptr += 2;
                len -= 2;
            }

            return len == 0 || *s1_ptr == *s2_ptr;
        }
    }

    public static bool operator ==(CString a, CString b)
    {
        return Equals(a, b);
    }

    public static bool operator !=(CString a, CString b)
    {
        return !Equals(a, b);
    }

    public override bool Equals(Object obj)
    {
        return Equals(this, obj as CString);
    }

    public bool Equals(CString value)
    {
        return Equals(this, value);
    }

    public static unsafe bool Equals(CString a, string b)
    {        
        object l = a as object;
        object r = b as object;

        if (l == r)
        {
            return true;
        }

        if (l == null || r == null)
        {
            return false;
        }

        int len = a.length;

        if (len != b.Length)
        {
            return false;
        }

        fixed (char* s1 = a._buffer, s2 = b)
        {
            char* s1_ptr = s1;
            char* s2_ptr = s2;

            while (len >= 8)
            {
                if (((int*)s1_ptr)[0] != ((int*)s2_ptr)[0] || ((int*)s1_ptr)[1] != ((int*)s2_ptr)[1] ||
                    ((int*)s1_ptr)[2] != ((int*)s2_ptr)[2] || ((int*)s1_ptr)[3] != ((int*)s2_ptr)[3])
                {
                    return false;
                }

                s1_ptr += 8;
                s2_ptr += 8;
                len -= 8;
            }

            if (len >= 4)
            {
                if (((int*)s1_ptr)[0] != ((int*)s2_ptr)[0] || ((int*)s1_ptr)[1] != ((int*)s2_ptr)[1])
                {
                    return false;
                }

                s1_ptr += 4;
                s2_ptr += 4;
                len -= 4;
            }

            if (len > 1)
            {
                if (((int*)s1_ptr)[0] != ((int*)s2_ptr)[0])
                {
                    return false;
                }

                s1_ptr += 2;
                s2_ptr += 2;
                len -= 2;
            }

            return len == 0 || *s1_ptr == *s2_ptr;
        }
    }

    public static bool operator ==(CString a, string b)
    {
        return Equals(a, b);
    }

    public static bool operator !=(CString a, string b)
    {
        return !Equals(a, b);
    }

    public bool Equals(string value)
    {
        return Equals(this, value);
    }

    public static bool operator ==(string a, CString b)
    {
        return Equals(b, a);
    }

    public static bool operator !=(string a, CString b)
    {
        return !Equals(b, a);
    }

    public unsafe override int GetHashCode()
    {
        fixed (char* c = _buffer)
        {
            char* cc = c;
            char* end = cc + length - 1;
            int h = 0;

            for (; cc < end; cc += 2)
            {
                h = (h << 5) - h + *cc;
                h = (h << 5) - h + cc[1];
            }

            ++end;

            if (cc < end)
            {
                h = (h << 5) - h + *cc;
            }

            return h;
        }
    }

    /*unsafe internal void NumberToString(string format, int value, IFormatProvider provider)
    {
        fixed (char* p = _buffer)
        {
            length = NumberFormatter.NumberToString(p, format, value, provider);
        }
    }

    unsafe internal void NumberToString(string format, uint value, IFormatProvider provider)
    {
        fixed (char* p = _buffer)
        {
            length = NumberFormatter.NumberToString(p, format, value, provider);
        }
    }

    unsafe internal void NumberToString(string format, long value, IFormatProvider provider)
    {
        fixed (char* p = _buffer)
        {
            length = NumberFormatter.NumberToString(p, format, value, provider);
        }
    }

    unsafe internal void NumberToString(string format, ulong value, IFormatProvider provider)
    {
        fixed (char* p = _buffer)
        {
            length = NumberFormatter.NumberToString(p, format, value, provider);
        }
    }

    unsafe internal void NumberToString(string format, float value, IFormatProvider provider)
    {
        fixed (char* p = _buffer)
        {
            length = NumberFormatter.NumberToString(p, format, value, provider);
        }
    }

    unsafe internal void NumberToString(string format, double value, IFormatProvider provider)
    {
        fixed (char* p = _buffer)
        {
            length = NumberFormatter.NumberToString(p, format, value, provider);
        }
    }*/

    public static implicit operator CString(string str)
    {
        CString temp = Alloc(str.Length);
        temp.CopyFromString(str);        
        return temp;
    }

    public unsafe static CString operator +(CString left, CString right)
    {
        int count = left.length + right.length;
        CString str = Alloc(count);

        fixed (char* c1 = left._buffer, c2 = right._buffer, dest = str._buffer)
        {
            CharCopy(dest, c1, left.length);
            CharCopy(dest + left.length, c2, right.length);
            str.length = count;
        }

        return str;
    }

    public unsafe static CString operator +(CString left, int rhl)
    {
        CString right = rhl.ToCString();
        int count = left.length + right.length;
        CString str = Alloc(count);

        fixed (char* c1 = left._buffer, c2 = right._buffer, dest = str._buffer)
        {
            CharCopy(dest, c1, left.length);
            CharCopy(dest + left.length, c2, right.length);
            str.length = count;
        }

        right.Dispose();
        return str;
    }

    public unsafe static CString operator +(CString left, uint rhl)
    {
        CString right = rhl.ToCString();
        int count = left.length + right.length;
        CString str = Alloc(count);

        fixed (char* c1 = left._buffer, c2 = right._buffer, dest = str._buffer)
        {
            CharCopy(dest, c1, left.length);
            CharCopy(dest + left.length, c2, right.length);
            str.length = count;
        }

        right.Dispose();
        return str;
    }

    public unsafe static CString operator +(CString left, float rhl)
    {
        CString right = rhl.ToCString();
        int count = left.length + right.length;
        CString str = Alloc(count);

        fixed (char* c1 = left._buffer, c2 = right._buffer, dest = str._buffer)
        {
            CharCopy(dest, c1, left.length);
            CharCopy(dest + left.length, c2, right.length);
            str.length = count;
        }

        right.Dispose();
        return str;
    }

    public unsafe static CString operator +(CString left, double rhl)
    {
        CString right = rhl.ToCString();
        int count = left.length + right.length;
        CString str = Alloc(count);

        fixed (char* c1 = left._buffer, c2 = right._buffer, dest = str._buffer)
        {
            CharCopy(dest, c1, left.length);
            CharCopy(dest + left.length, c2, right.length);
            str.length = count;
        }

        right.Dispose();
        return str;
    }

    unsafe void SetIndex(int index, char src)
    {
        fixed (char* p = _buffer)
        {
            p[index] = src;
        }
    }

    public char this[int index]
    {
        get
        {
            return _buffer[index];
        }

        set
        {
            SetIndex(index, value);
        }
    }

    public int Length
    {
        get
        {
            return length;
        }
    }

    public int Count
    {
        get
        {
            return _buffer.Length;
        }
    }

    public CString Clone()
    {
        return SubstringUnchecked(0, length);
    }

    public char[] ToCharArray()
    {
        return ToCharArray(0, length);
    }

    public unsafe char[] ToCharArray(int startIndex, int len)
    {
        if (len < 0)
        {
            throw new ArgumentOutOfRangeException("length", "len < 0");
        }

        if (startIndex < 0 || startIndex > this.length - len)
        {
            throw new ArgumentOutOfRangeException("startIndex", "startIndex out of range");
        }

        char[] tmp = new char[len];

        fixed (char* dest = tmp, src = _buffer)
        {
            CharCopy(dest, src + startIndex, len);
        }

        return tmp;
    }

    public CString[] Split(params char[] separator)
    {
        return Split(separator, Int32.MaxValue);
    }

    public CString[] Split(char[] separator, int count)
    {
        if (separator == null || separator.Length == 0)
        {
            separator = WhiteChars;
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        if (count == 0)
        {
            return new CString[0];
        }

        if (count == 1)
        {
            return new CString[1] { this };
        }

        return Split(separator, count, StringSplitOptions.None);        
    }

    static List<CString> splitList = new List<CString>();

    public CString[] Split(char[] separator, int count, StringSplitOptions options)
    {
        if (separator == null || separator.Length == 0)
        {
            return Split(WhiteChars, count, options);
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count", "Count cannot be less than zero.");
        }

        if ((options != StringSplitOptions.None) && (options != StringSplitOptions.RemoveEmptyEntries))
        {
            throw new ArgumentException("Illegal enum value: " + options + ".");
        }

        bool removeEmpty = (options & StringSplitOptions.RemoveEmptyEntries) == StringSplitOptions.RemoveEmptyEntries;

        if (count == 0 || (CString.IsNullOrEmpty(this) && removeEmpty))
        {
            return new CString[0];
        }
        
        int pos = 0;
        int matchCount = 0;
        splitList.Clear();

        while (pos < this.Length)
        {
            int matchIndex = -1;
            int matchPos = Int32.MaxValue;

            // Find the first position where any of the separators matches
            for (int i = 0; i < separator.Length; ++i)
            {
                char sep = separator[i];
                int match = IndexOf(sep, pos);

                if (match > -1 && match < matchPos)
                {
                    matchIndex = i;
                    matchPos = match;
                }
            }

            if (matchIndex == -1)
            {
                break;
            }

            if (!(matchPos == pos && removeEmpty))
            {
                if (splitList.Count == count - 1)
                {
                    break;
                }

                splitList.Add(Substring(pos, matchPos - pos));
            }

            pos = matchPos + 1;
            matchCount++;
        }

        if (matchCount == 0)
        {
            return new CString[] { this };
        }

        // string contained only separators
        if (removeEmpty && matchCount != 0 && pos == this.Length && splitList.Count == 0)
        {
            return new CString[0];
        }

        if (!(removeEmpty && pos == this.Length))
        {
            splitList.Add(this.Substring(pos));
        }

        return splitList.ToArray();                
    }

    public CString[] Split(string[] separator, int count, StringSplitOptions options)
    {
        if (separator == null || separator.Length == 0)
        {
            return Split(WhiteChars, count, options);
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count", "Count cannot be less than zero.");
        }

        if (options != StringSplitOptions.None && options != StringSplitOptions.RemoveEmptyEntries)
        {
            throw new ArgumentException("Illegal enum value: " + options + ".");
        }

        if (count == 1)
        {
            return new CString[] { this };
        }

        bool removeEmpty = (options & StringSplitOptions.RemoveEmptyEntries) == StringSplitOptions.RemoveEmptyEntries;

        if (count == 0 || (this == String.Empty && removeEmpty))
        {
            return new CString[0];
        }
        
        int pos = 0;
        int matchCount = 0;
        splitList.Clear();

        while (pos < this.Length)
        {
            int matchIndex = -1;
            int matchPos = Int32.MaxValue;

            // Find the first position where any of the separators matches
            for (int i = 0; i < separator.Length; ++i)
            {
                string sep = separator[i];

                if (sep == null || sep == String.Empty)
                {
                    continue;
                }

                int match = IndexOf(sep, pos);

                if (match > -1 && match < matchPos)
                {
                    matchIndex = i;
                    matchPos = match;
                }
            }

            if (matchIndex == -1)
            {
                break;
            }

            if (!(matchPos == pos && removeEmpty))
            {
                if (splitList.Count == count - 1)
                {
                    break;
                }

                splitList.Add(Substring(pos, matchPos - pos));
            }

            pos = matchPos + separator[matchIndex].Length;
            matchCount++;
        }

        if (matchCount == 0)
        {
            return new CString[] { this };
        }

        // string contained only separators
        if (removeEmpty && matchCount != 0 && pos == this.Length && splitList.Count == 0)
        {
            return new CString[0];
        }

        if (!(removeEmpty && pos == this.Length))
        {
            splitList.Add(this.Substring(pos));
        }

        return splitList.ToArray();
    }
    
    public CString[] Split(char[] separator, StringSplitOptions options)
    {
        return Split(separator, Int32.MaxValue, options);
    }
    
    public CString[] Split(String[] separator, StringSplitOptions options)
    {
        return Split(separator, Int32.MaxValue, options);
    }

    /// <summary>
    /// 分配新的CString为当前字符串从startIndex开始的一部分
    /// </summary>        
    public CString Substring(int startIndex)
    {
        if (startIndex == 0)
        {
            return this;
        }

        if (startIndex < 0 || startIndex > this.length)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        return SubstringUnchecked(startIndex, length - startIndex);
    }

    public CString Substring(int startIndex, int len)
    {
        if (len < 0)
        {
            throw new ArgumentOutOfRangeException("length", "Cannot be negative.");
        }

        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException("startIndex", "Cannot be negative.");
        }

        if (startIndex > this.length)
        {
            throw new ArgumentOutOfRangeException("startIndex", "Cannot exceed length of string.");
        }

        if (startIndex > this.length - len)
        {
            throw new ArgumentOutOfRangeException("length", "startIndex + length > this.length");
        }

        if (startIndex == 0 && len == this.length)
        {
            return this;
        }

        return SubstringUnchecked(startIndex, len);
    }

    internal unsafe CString SubstringUnchecked(int startIndex, int len)
    {
        if (len == 0)
        {
            return Alloc(0);
        }

        CString tmp = Alloc(len);

        fixed (char* dest = tmp._buffer, src = _buffer)
        {
            CharCopy(dest, src + startIndex, len);
            tmp.length = len;
        }

        return tmp;
    }

    internal unsafe CString TrimStringUnchecked(int startIndex, int len)
    {
        if (len == 0)
        {
            Clear();
            return this;
        }

        fixed (char* dest = _buffer)
        {
            CharCopy(dest, dest + startIndex, len);
            this.length = len;
        }

        return this;
    }

    /// <summary>
    /// 去除字符串前后的空格, 注意不同于string, 这个函数修改的是字符串自身.
    /// </summary>
    /// <returns>返回当前字符串</returns>
    public CString Trim()
    {
        if (length == 0)
        {
            return this;
        }

        int start = FindNotWhiteSpace(0, length, 1);

        if (start == length)
        {
            Clear();
            return this;
        }

        int end = FindNotWhiteSpace(length - 1, start, -1);
        int newLength = end - start + 1;

        if (newLength == length)
        {
            return this;
        }

        return TrimStringUnchecked(start, newLength);
    }

    /// <summary>
    /// 从字符串两端去除trimChars包含的单个字符, 注意不同于string, 这个函数修改的是字符串自身.
    /// </summary>
    /// <returns>返回当前字符串</returns>
    public CString Trim(params char[] trimChars)
    {
        if (trimChars == null || trimChars.Length == 0)
        {
            return Trim();
        }

        if (length == 0)
        {
            return this;
        }

        int start = FindNotInTable(0, length, 1, trimChars);

        if (start == length)
        {
            Clear();
            return this;
        }

        int end = FindNotInTable(length - 1, start, -1, trimChars);
        int newLength = end - start + 1;

        if (newLength == length)
        {
            return this;
        }

        return TrimStringUnchecked(start, newLength);
    }

    public CString TrimStart(params char[] trimChars)
    {
        if (length == 0)
        {
            return this;
        }

        int start;

        if (trimChars == null || trimChars.Length == 0)
        {
            start = FindNotWhiteSpace(0, length, 1);
        }
        else
        {
            start = FindNotInTable(0, length, 1, trimChars);
        }

        if (start == 0)
        {
            return this;
        }

        return TrimStringUnchecked(start, length - start);
    }

    public CString TrimEnd(params char[] trimChars)
    {
        if (length == 0)
        {
            return this;
        }

        int end;

        if (trimChars == null || trimChars.Length == 0)
        {
            end = FindNotWhiteSpace(length - 1, -1, -1);
        }
        else
        {
            end = FindNotInTable(length - 1, -1, -1, trimChars);
        }

        end++;

        if (end == length)
        {
            return this;
        }

        return SubstringUnchecked(0, end);
    }


    private int FindNotWhiteSpace(int pos, int target, int change)
    {
        while (pos != target)
        {
            char c = this[pos];

            if (Char.IsWhiteSpace(c))
            {
                pos += change;
            }
            else
            {
                return pos;
            }
        }
        return pos;
    }

    private unsafe int FindNotInTable(int pos, int target, int change, char[] table)
    {
        fixed (char* tablePtr = table, thisPtr = _buffer)
        {
            while (pos != target)
            {
                char c = thisPtr[pos];
                int x = 0;

                while (x < table.Length)
                {
                    if (c == tablePtr[x])
                    {
                        break;
                    }

                    x++;
                }

                if (x == table.Length)
                {
                    return pos;
                }

                pos += change;
            }
        }

        return pos;
    }

    public static int Compare(CString strA, CString strB)
    {       
        return CompareOrdinal(strA, strB);        
    }

    public static int Compare(CString strA, CString strB, bool ignoreCase)
    {
        if (ignoreCase)
        {
            return CompareOrdinalCaseInsensitiveUnchecked(strA, 0, Int32.MaxValue, strB, 0, Int32.MaxValue);
        }
        else
        {
            return CompareOrdinalUnchecked(strA, 0, Int32.MaxValue, strB, 0, Int32.MaxValue);
        }        
    }


    /*public unsafe static int Compare(CString strA, CString strB, StringComparison comparisonType)
    {        
        switch (comparisonType)
        {
            case StringComparison.Ordinal:
                return CompareOrdinalUnchecked(strA, 0, Int32.MaxValue, strB, 0, Int32.MaxValue);
            case StringComparison.OrdinalIgnoreCase:
                return CompareOrdinalCaseInsensitiveUnchecked(strA, 0, Int32.MaxValue, strB, 0, Int32.MaxValue);
            default:                
                throw new ArgumentException("Invalid value for StringComparison", "comparisonType");
        }
    }

    public static int Compare(CString strA, int indexA, CString strB, int indexB, int length, StringComparison comparisonType)
    {
        switch (comparisonType)
        {
            case StringComparison.Ordinal:
                return CompareOrdinal(strA, indexA, strB, indexB, length);
            case StringComparison.OrdinalIgnoreCase:
                return CompareOrdinalCaseInsensitive(strA, indexA, strB, indexB, length);
            default:                
                throw new ArgumentException("Invalid value for StringComparison", "comparisonType");
        }
    }

    public static bool Equals(CString a, CString b, StringComparison comparisonType)
    {
        return CString.Compare(a, b, comparisonType) == 0;
    }

    public bool Equals(CString value, StringComparison comparisonType)
    {
        return CString.Compare(value, this, comparisonType) == 0;
    }*/

    public static bool Equals(CString a, CString b, bool ignoreCase)
    {
        return CString.Compare(a, b, ignoreCase) == 0;
    }

    public bool Equals(CString value, bool ignoreCase)
    {
        return CString.Compare(value, this, ignoreCase) == 0;
    }

    public static int CompareOrdinal(CString strA, CString strB)
    {
        return CompareOrdinalUnchecked(strA, 0, Int32.MaxValue, strB, 0, Int32.MaxValue);
    }


    unsafe public static int CompareOrdinal(CString strA, int indexA, CString strB, int indexB, int len)
    {
        if ((indexA > strA.Length) || (indexB > strB.Length) || (indexA < 0) || (indexB < 0) || (len < 0))
        {
            throw new ArgumentOutOfRangeException();
        }

        return CompareOrdinalUnchecked(strA, indexA, len, strB, indexB, len);
    }

    internal static unsafe int CompareOrdinalUnchecked(CString strA, int indexA, int lenA, CString strB, int indexB, int lenB)
    {
        if (strA == null)
        {
            return strB == null ? 0 : -1;
        }
        else if (strB == null)
        {
            return 1;
        }

        int lengthA = Math.Min(lenA, strA.Length - indexA);
        int lengthB = Math.Min(lenB, strB.Length - indexB);

        if (lengthA == lengthB && Object.ReferenceEquals(strA, strB))
        {
            return 0;
        }

        fixed (char* aptr = strA._buffer, bptr = strB._buffer)
        {
            char* ap = aptr + indexA;
            char* end = ap + Math.Min(lengthA, lengthB);
            char* bp = bptr + indexB;
            while (ap < end)
            {
                if (*ap != *bp)
                {
                    return *ap - *bp;
                }

                ap++;
                bp++;
            }
            return lengthA - lengthB;
        }
    }

    internal static int CompareOrdinalCaseInsensitive(CString strA, int indexA, CString strB, int indexB, int length)
    {
        if ((indexA > strA.Length) || (indexB > strB.Length) || (indexA < 0) || (indexB < 0) || (length < 0))
        {
            throw new ArgumentOutOfRangeException();
        }

        return CompareOrdinalCaseInsensitiveUnchecked(strA, indexA, length, strB, indexB, length);
    }

    internal static unsafe int CompareOrdinalCaseInsensitiveUnchecked(CString strA, int indexA, int lenA, CString strB, int indexB, int lenB)
    {
        if (strA == null)
        {
            return strB == null ? 0 : -1;
        }
        else if (strB == null)
        {
            return 1;
        }

        int lengthA = Math.Min(lenA, strA.Length - indexA);
        int lengthB = Math.Min(lenB, strB.Length - indexB);

        if (lengthA == lengthB && Object.ReferenceEquals(strA, strB))
        {
            return 0;
        }

        fixed (char* aptr = strA._buffer, bptr = strB._buffer)
        {
            char* ap = aptr + indexA;
            char* end = ap + Math.Min(lengthA, lengthB);
            char* bp = bptr + indexB;
            while (ap < end)
            {
                if (*ap != *bp)
                {
                    char c1 = Char.ToUpperInvariant(*ap);
                    char c2 = Char.ToUpperInvariant(*bp);

                    if (c1 != c2)
                    {
                        return c1 - c2;
                    }
                }
                ap++;
                bp++;
            }
            return lengthA - lengthB;
        }
    }


    internal unsafe static void memcpy(CString dest, CString src, int size, int src_offset)
    {
        fixed (char* _dest = dest._buffer)
        {
            fixed (char* _src = src._buffer)
            {
                src += src_offset;
                CharCopy(_dest, _src, size);
            }
        }
    }

    public bool EndsWith(CString value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (length < value.Length)
        {
            return false;
        }

        for (int i = _buffer.Length - 1, j = value.Length - 1; j >= 0; i--, j--)
        {
            if (_buffer[i] != value._buffer[j])
            {
                return false;
            }
        }

        return true;
    }

    public int IndexOfAny(char[] anyOf)
    {
        if (anyOf == null)
        {
            throw new ArgumentNullException();
        }

        if (this.length == 0)
        {
            return -1;
        }

        return IndexOfAnyUnchecked(anyOf, 0, this.length);
    }

    public int IndexOfAny(char[] anyOf, int startIndex)
    {
        if (anyOf == null)
        {
            throw new ArgumentNullException();
        }

        if (startIndex < 0 || startIndex > this.length)
        {
            throw new ArgumentOutOfRangeException();
        }

        return IndexOfAnyUnchecked(anyOf, startIndex, this.length - startIndex);
    }

    public int IndexOfAny(char[] anyOf, int startIndex, int count)
    {
        if (anyOf == null)
        {
            throw new ArgumentNullException();
        }

        if (startIndex < 0 || startIndex > this.length)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (count < 0 || startIndex > this.length - count)
        {
            throw new ArgumentOutOfRangeException("count", "Count cannot be negative, and startIndex + count must be less than length of the string.");
        }

        return IndexOfAnyUnchecked(anyOf, startIndex, count);
    }

    private unsafe int IndexOfAnyUnchecked(char[] anyOf, int startIndex, int count)
    {
        if (anyOf.Length == 0)
        {
            return -1;
        }

        if (anyOf.Length == 1)
        {
            return IndexOfUnchecked(anyOf[0], startIndex, count);
        }

        fixed (char* any = anyOf)
        {
            int highest = *any;
            int lowest = *any;

            char* end_any_ptr = any + anyOf.Length;
            char* any_ptr = any;

            while (++any_ptr != end_any_ptr)
            {
                if (*any_ptr > highest)
                {
                    highest = *any_ptr;
                    continue;
                }

                if (*any_ptr < lowest)
                {
                    lowest = *any_ptr;
                }
            }

            fixed (char* start = _buffer)
            {
                char* ptr = start + startIndex;
                char* end_ptr = ptr + count;

                while (ptr != end_ptr)
                {
                    if (*ptr > highest || *ptr < lowest)
                    {
                        ptr++;
                        continue;
                    }

                    if (*ptr == *any)
                    {
                        return (int)(ptr - start);
                    }

                    any_ptr = any;

                    while (++any_ptr != end_any_ptr)
                    {
                        if (*ptr == *any_ptr)
                        {
                            return (int)(ptr - start);
                        }
                    }

                    ptr++;
                }
            }
        }

        return -1;
    }

    public int IndexOf(char value)
    {
        if (length == 0)
        {
            return -1;
        }

        return IndexOfUnchecked(value, 0, length);
    }

    public int IndexOf(char value, int startIndex)
    {
        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException("startIndex", "< 0");
        }

        if (startIndex > length)
        {
            throw new ArgumentOutOfRangeException("startIndex", "startIndex > this.length");
        }

        if ((startIndex == 0 && length == 0) || (startIndex == length))
        {
            return -1;
        }

        return IndexOfUnchecked(value, startIndex, length - startIndex);
    }

    public int IndexOf(char value, int startIndex, int count)
    {
        if (startIndex < 0 || startIndex > length)
        {
            throw new ArgumentOutOfRangeException("startIndex", "Cannot be negative and must be< 0");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count", "< 0");
        }

        if (startIndex > length - count)
        {
            throw new ArgumentOutOfRangeException("count", "startIndex + count > this.length");
        }

        if ((startIndex == 0 && length == 0) || (startIndex == length) || (count == 0))
        {
            return -1;
        }

        return IndexOfUnchecked(value, startIndex, count);
    }

    internal unsafe int IndexOfUnchecked(char value, int startIndex, int count)
    {
        // It helps JIT compiler to optimize comparison
        int value_32 = (int)value;

        fixed (char* start = _buffer)
        {
            char* ptr = start + startIndex;
            char* end_ptr = ptr + (count >> 3 << 3);

            while (ptr != end_ptr)
            {
                if (*ptr == value_32)
                    return (int)(ptr - start);
                if (ptr[1] == value_32)
                    return (int)(ptr - start + 1);
                if (ptr[2] == value_32)
                    return (int)(ptr - start + 2);
                if (ptr[3] == value_32)
                    return (int)(ptr - start + 3);
                if (ptr[4] == value_32)
                    return (int)(ptr - start + 4);
                if (ptr[5] == value_32)
                    return (int)(ptr - start + 5);
                if (ptr[6] == value_32)
                    return (int)(ptr - start + 6);
                if (ptr[7] == value_32)
                    return (int)(ptr - start + 7);

                ptr += 8;
            }

            end_ptr += count & 0x07;

            while (ptr != end_ptr)
            {
                if (*ptr == value_32)
                {
                    return (int)(ptr - start);
                }

                ptr++;
            }
            return -1;
        }
    }

    public int IndexOf(string value)
    {
        return IndexOf(value, 0, length);
    }

    public int IndexOf(string value, int startIndex)
    {
        return IndexOf(value, startIndex, length - startIndex);
    }

    public unsafe int IndexOf(string value, int startIndex, int count)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (startIndex < 0 || startIndex > Length)
        {
            throw new ArgumentOutOfRangeException("startIndex", "Cannot be negative, and should not exceed length of string.");
        }

        if (count < 0 || startIndex > length - count)
        {
            throw new ArgumentOutOfRangeException("count", "Cannot be negative, and should point to location in string.");
        }

        if (value.Length == 0)
        {
            return startIndex;
        }

        if (startIndex == 0 && Length == 0)
        {
            return -1;
        }

        if (count == 0)
        {
            return -1;
        }

        int valueLen = count;

        fixed (char* thisptr = _buffer, valueptr = value)
        {
            char* ap = thisptr + startIndex;
            char* thisEnd = ap + count - valueLen + 1;

            while (ap != thisEnd)
            {
                if (*ap == *valueptr)
                {
                    for (int i = 1; i < valueLen; i++)
                    {
                        if (ap[i] != valueptr[i])
                        {
                            goto NextVal;
                        }
                    }
                    return (int)(ap - thisptr);
                }
            NextVal:
                ap++;
            }
        }

        return -1;
    }

    internal unsafe int IndexOfOrdinalUnchecked(string value, int startIndex, int count)
    {
        int valueLen = value.Length;

        if (count < valueLen)
        {
            return -1;
        }

        if (valueLen <= 1)
        {
            if (valueLen == 1)
            {
                return IndexOfUnchecked(value[0], startIndex, count);
            }

            return startIndex;
        }

        fixed (char* thisptr = _buffer, valueptr = value)
        {
            char* ap = thisptr + startIndex;
            char* thisEnd = ap + count - valueLen + 1;

            while (ap != thisEnd)
            {
                if (*ap == *valueptr)
                {
                    for (int i = 1; i < valueLen; i++)
                    {
                        if (ap[i] != valueptr[i])
                        {
                            goto NextVal;
                        }
                    }
                    return (int)(ap - thisptr);
                }

            NextVal:
                ap++;
            }
        }

        return -1;
    }

    public int LastIndexOfAny(char[] anyOf)
    {
        if (anyOf == null)
        {
            throw new ArgumentNullException();
        }

        return LastIndexOfAnyUnchecked(anyOf, this.length - 1, this.length);
    }

    public int LastIndexOfAny(char[] anyOf, int startIndex)
    {
        if (anyOf == null)
        {
            throw new ArgumentNullException();
        }

        if (startIndex < 0 || startIndex >= this.length)
        {
            throw new ArgumentOutOfRangeException("startIndex", "Cannot be negative, and should be less than length of string.");
        }

        if (this.length == 0)
        {
            return -1;
        }

        return LastIndexOfAnyUnchecked(anyOf, startIndex, startIndex + 1);
    }

    public int LastIndexOfAny(char[] anyOf, int startIndex, int count)
    {
        if (anyOf == null)
        {
            throw new ArgumentNullException();
        }

        if ((startIndex < 0) || (startIndex >= this.Length))
        {
            throw new ArgumentOutOfRangeException("startIndex", "< 0 || > this.Length");
        }

        if ((count < 0) || (count > this.Length))
        {
            throw new ArgumentOutOfRangeException("count", "< 0 || > this.Length");
        }

        if (startIndex - count + 1 < 0)
        {
            throw new ArgumentOutOfRangeException("startIndex - count + 1 < 0");
        }

        if (this.length == 0)
        {
            return -1;
        }

        return LastIndexOfAnyUnchecked(anyOf, startIndex, count);
    }

    private unsafe int LastIndexOfAnyUnchecked(char[] anyOf, int startIndex, int count)
    {
        if (anyOf.Length == 1)
        {
            return LastIndexOfUnchecked(anyOf[0], startIndex, count);
        }

        fixed (char* start = _buffer, testStart = anyOf)
        {
            char* ptr = start + startIndex;
            char* ptrEnd = ptr - count;
            char* test;
            char* testEnd = testStart + anyOf.Length;

            while (ptr != ptrEnd)
            {
                test = testStart;
                while (test != testEnd)
                {
                    if (*test == *ptr)
                    {
                        return (int)(ptr - start);
                    }
                    test++;
                }
                ptr--;
            }
            return -1;
        }
    }


    public int LastIndexOf(char value)
    {
        if (length == 0)
        {
            return -1;
        }

        return LastIndexOfUnchecked(value, this.length - 1, this.length);
    }

    public int LastIndexOf(char value, int startIndex)
    {
        return LastIndexOf(value, startIndex, startIndex + 1);
    }

    public int LastIndexOf(char value, int startIndex, int count)
    {
        if (startIndex == 0 && length == 0)
        {
            return -1;
        }

        if ((startIndex < 0) || (startIndex >= this.Length))
        {
            throw new ArgumentOutOfRangeException("startIndex", "< 0 || >= this.Length");
        }

        if ((count < 0) || (count > this.Length))
        {
            throw new ArgumentOutOfRangeException("count", "< 0 || > this.Length");
        }

        if (startIndex - count + 1 < 0)
        {
            throw new ArgumentOutOfRangeException("startIndex - count + 1 < 0");
        }

        return LastIndexOfUnchecked(value, startIndex, count);
    }

    internal unsafe int LastIndexOfUnchecked(char value, int startIndex, int count)
    {
        // It helps JIT compiler to optimize comparison
        int value_32 = (int)value;

        fixed (char* start = _buffer)
        {
            char* ptr = start + startIndex;
            char* end_ptr = ptr - (count >> 3 << 3);

            while (ptr != end_ptr)
            {
                if (*ptr == value_32)
                    return (int)(ptr - start);
                if (ptr[-1] == value_32)
                    return (int)(ptr - start) - 1;
                if (ptr[-2] == value_32)
                    return (int)(ptr - start) - 2;
                if (ptr[-3] == value_32)
                    return (int)(ptr - start) - 3;
                if (ptr[-4] == value_32)
                    return (int)(ptr - start) - 4;
                if (ptr[-5] == value_32)
                    return (int)(ptr - start) - 5;
                if (ptr[-6] == value_32)
                    return (int)(ptr - start) - 6;
                if (ptr[-7] == value_32)
                    return (int)(ptr - start) - 7;

                ptr -= 8;
            }

            end_ptr -= count & 0x07;

            while (ptr != end_ptr)
            {
                if (*ptr == value_32)
                    return (int)(ptr - start);

                ptr--;
            }
            return -1;
        }
    }

    // Following methods are culture-sensitive
    public int LastIndexOf(string value)
    {
        if (this.length == 0)
        {
            // This overload does additional checking
            return LastIndexOf(value, 0, 0);
        }
        else
        {
            return LastIndexOf(value, length - 1, length);
        }
    }

    public int LastIndexOf(string value, int startIndex)
    {
        int max = startIndex;

        if (max < length)
        {
            max++;
        }
        return LastIndexOf(value, startIndex, max);
    }

    public unsafe int LastIndexOf(string value, int startIndex, int count)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        // -1 > startIndex > for string (0 > startIndex >= for char)
        if ((startIndex < -1) || (startIndex > length))
        {
            throw new ArgumentOutOfRangeException("startIndex", "< 0 || > this.Length");
        }

        if ((count < 0) || (count > length))
        {
            throw new ArgumentOutOfRangeException("count", "< 0 || > this.Length");
        }

        if (startIndex - count + 1 < 0)
        {
            throw new ArgumentOutOfRangeException("startIndex - count + 1 < 0");
        }

        int valueLen = value.Length;

        if (valueLen == 0)
        {
            return startIndex;
        }

        if (startIndex == 0 && length == 0)
        {
            return -1;
        }

        // This check is needed to match undocumented MS behaviour
        if (length == 0 && valueLen > 0)
        {
            return -1;
        }

        if (count == 0)
        {
            return -1;
        }

        if (startIndex == length)
        {
            startIndex--;
        }

        fixed (char* thisptr = _buffer, valueptr = value)
        {
            char* ap = thisptr + startIndex - valueLen + 1;
            char* thisEnd = ap - count + valueLen - 1;

            while (ap != thisEnd)
            {
                if (*ap == *valueptr)
                {
                    for (int i = 1; i < valueLen; i++)
                    {
                        if (ap[i] != valueptr[i])
                            goto NextVal;
                    }
                    return (int)(ap - thisptr);
                }
            NextVal:
                ap--;
            }
        }

        return -1;        
    }

    public bool Contains(String value)
    {
        return IndexOf(value) != -1;
    }

    public static bool IsNullOrEmpty(CString value)
    {
        object obj = value;
        if (obj == null) return true;

        return value.length == 0;
    }

    public static bool IsNullOrWhiteSpace(CString value)
    {
        object obj = value;

        if (obj == null)
        {
            return true;
        }

        for (int i = 0; i < value.Length; i++)
        {
            if (!Char.IsWhiteSpace(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    public CString Remove(int startIndex)
    {
        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException("startIndex", "StartIndex can not be less than zero");
        }

        if (startIndex >= this.length)
        {
            throw new ArgumentOutOfRangeException("startIndex", "StartIndex must be less than the length of the string");
        }

        length = startIndex;
        return this;
    }

    public unsafe CString Remove(int startIndex, int len)
    {
        if (startIndex < 0 || len < 0 || startIndex > length - len)
        {
            throw new ArgumentOutOfRangeException();
        }

        fixed (char* dest = _buffer)
        {
            CharCopy(dest + startIndex, dest + startIndex + len, length - startIndex - len);            
        }

        length -= len;
        return this;
    }

    public CString PadLeft(int totalWidth)
    {
        return PadLeft(totalWidth, ' ');
    }

    /// <summary>
    /// 在当前字符串前插入totalWidth个paddingChar
    /// </summary>
    /// <param name="totalWidth"></param>
    /// <param name="paddingChar"></param>
    /// <returns></returns>
    public unsafe CString PadLeft(int totalWidth, char paddingChar)
    {
        //LAMESPEC: MSDN Doc says this is reversed for RtL languages, but this seems to be untrue
        if (totalWidth < 0)
        {
            throw new ArgumentOutOfRangeException("totalWidth", "< 0");
        }

        if (totalWidth < length)
        {
            return this;
        }

        EnsureCapacity(totalWidth);

        fixed (char* dest = _buffer)
        {
            for (int i = length - 1; i >= 0; i--)
            {
                _buffer[i + totalWidth - length] = _buffer[i];
            }

            for (int i = 0; i < totalWidth - length; i++)
            {
                dest[i] = paddingChar;
            }
        }

        length = totalWidth;
        return this;
    }

    public CString PadRight(int totalWidth)
    {
        return PadRight(totalWidth, ' ');
    }

    public unsafe CString PadRight(int totalWidth, char paddingChar)
    {
        //LAMESPEC: MSDN Doc says this is reversed for RtL languages, but this seems to be untrue
        if (totalWidth < 0)
        {
            throw new ArgumentOutOfRangeException("totalWidth", "< 0");
        }

        if (totalWidth < length)
        {
            return this;
        }

        if (totalWidth == 0)
        {
            length = 0;
            return this;
        }

        EnsureCapacity(totalWidth);

        for (int i = length; i < totalWidth; i++)
        {
            _buffer[i] = paddingChar;
        }

        length = totalWidth;
        return this;
    }

    public bool StartsWith(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (length < value.Length)
        {
            return false;
        }

        for (int i = 0; i < value.Length; i++)
        {
            if (_buffer[i] != value[i])
            {
                return false;
            }
        }

        return true;
    }


    public CString Replace(char oldChar, char newChar)
    {
        if (length == 0 || oldChar == newChar)
        {
            return this;
        }

        int start_pos = IndexOfUnchecked(oldChar, 0, length);

        if (start_pos == -1)
        {
            return this;
        }

        for (int i = start_pos; i < length; i++)
        {
            if (_buffer[i] == oldChar)
            {
                _buffer[i] = newChar;
            }
        }

        return this;
    }

    public CString Replace(char oldChar, char newChar, int startIndex, int count)
    {        
        if (startIndex < 0 || count < 0 || startIndex > length - count)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (length == 0 || oldChar == newChar)
        {
            return this;
        }

        int start_pos = IndexOfUnchecked(oldChar, startIndex, count);

        if (start_pos == -1)
        {
            return this;
        }

        for (int i = start_pos; i < startIndex + count; i++)
        {
            if (_buffer[i] == oldChar)
            {
                _buffer[i] = newChar;
            }
        }

        return this;
    }

    public unsafe CString Replace(string oldValue, string newValue, int startIndex, int count)
    {
        if (oldValue == null)
        {
            throw new ArgumentNullException("The old value cannot be null.");
        }

        if (startIndex < 0 || count < 0 || startIndex > length - count)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (oldValue.Length == 0)
        {
            throw new ArgumentException("The old value cannot be zero length.");
        }

        CString substr = Substring(startIndex, count);
        CString replace = substr.Replace(oldValue, newValue);
        // return early if no oldValue was found
        if ((object)replace == (object)substr)
        {
            return this;
        }

        EnsureCapacity(replace.Length + length - count);

        fixed (char* dest = _buffer, src = replace._buffer)
        {            
            if (replace.Length < count)
            {
                CharCopy(dest + startIndex + replace.Length, dest + startIndex + count, length - startIndex - count);
            }
            else if (replace.Length > count)
            {
                CharCopyReverse(dest + startIndex + replace.Length, dest + startIndex + count, length - startIndex - count);
            }
            
            CharCopy(dest + startIndex, src, replace.Length);
        }

        length = replace.Length + (length - count);
        return this;
    }

    public CString Replace(String oldValue, String newValue)
    {                
        if (oldValue == null)
        {
            throw new ArgumentNullException("oldValue");
        }

        if (oldValue.Length == 0)
        {
            throw new ArgumentException("oldValue is the empty string.");
        }

        if (length == 0)
        {
            return this;
        }

        if (newValue == null)
        {
            newValue = string.Empty;
        }

        return ReplaceUnchecked(oldValue, newValue);
    }

    //好像有问题
    private unsafe CString ReplaceUnchecked(string oldValue, string newValue)
    {
        if (oldValue.Length > length)
        {
            return this;
        }

        if (oldValue.Length == 1 && newValue.Length == 1)
        {
            return Replace(oldValue[0], newValue[0]);
        }

        const int maxValue = 200; // Allocate 800 byte maximum
        int* dat = stackalloc int[maxValue];

        fixed (char* source = _buffer, replace = newValue)
        {
            int i = 0, count = 0;

            while (i < length)
            {
                int found = IndexOfOrdinalUnchecked(oldValue, i, length - i);

                if (found < 0)
                {
                    break;
                }
                else
                {
                    if (count < maxValue)
                    {
                        dat[count++] = found;
                    }
                    else
                    {
                        return ReplaceFallback(oldValue, newValue, maxValue);
                    }
                }

                i = found + oldValue.Length;
            }

            if (count == 0)
            {
                return this;
            }

            int nlen = this.length + (newValue.Length - oldValue.Length) * count;
            CString temp = Alloc(nlen);            
            int curPos = 0, lastReadPos = 0;

            fixed (char* dest = temp._buffer)
            {
                for (int j = 0; j < count; j++)
                {
                    int precopy = dat[j] - lastReadPos;
                    CharCopy(dest + curPos, source + lastReadPos, precopy);
                    curPos += precopy;
                    lastReadPos = dat[j] + oldValue.Length;
                    CharCopy(dest + curPos, replace, newValue.Length);
                    curPos += newValue.Length;
                }

                CharCopy(dest + curPos, source + lastReadPos, length - lastReadPos);                
            }

            temp.length = nlen;
            return temp;
        }
    }
    
    private CString ReplaceFallback(string oldValue, string newValue, int testedCount)
    {
        int lengthEstimate = length + ((newValue.Length - oldValue.Length) * testedCount);
        CString sb = Alloc(lengthEstimate);

        for (int i = 0; i < length;)
        {
            int found = IndexOfOrdinalUnchecked(oldValue, i, length - i);

            if (found < 0)
            {
                sb.Append(SubstringUnchecked(i, length - i));
                break;
            }

            sb.Append(SubstringUnchecked(i, found - i));
            sb.Append(newValue);
            i = found + oldValue.Length;
        }

        return sb;
    }

    public CString ToLower()
    {
        return ToLower(CultureInfo.CurrentCulture);
    }

    public CString ToLower(CultureInfo culture)
    {
        if (culture == null)
        {
            throw new ArgumentNullException("culture");
        }

        if (culture.LCID == 0x007F)
        {
            return ToLowerInvariant();
        }

        return ToLower(culture.TextInfo);
    }

    internal CString ToLowerInvariant()
    {
        if (length == 0)
        {
            return this;
        }

        for (int i = 0; i < length; i++)
        {
            _buffer[i] = Char.ToLowerInvariant(_buffer[i]);
        }

        return this;
    }

    internal CString ToLower(TextInfo text)
    {
        if (length == 0)
        {
            return this;
        }

        for (int i = 0; i < length; i++)
        {
            _buffer[i] = text.ToLower(_buffer[i]);
        }

        return this;
    }

    public CString ToUpper()
    {
        return ToUpper(CultureInfo.CurrentCulture);
    }

    public CString ToUpper(CultureInfo culture)
    {
        if (culture == null)
        {
            throw new ArgumentNullException("culture");
        }

        if (culture.LCID == 0x007F)
        {
            return ToUpperInvariant();
        }

        return ToUpper(culture.TextInfo);
    }

    internal CString ToUpperInvariant()
    {
        if (length == 0)
        {
            return this;
        }

        for (int i = 0; i < length; i++)
        {
            _buffer[i] = Char.ToUpperInvariant(_buffer[i]);
        }

        return this;
    }

    internal unsafe CString ToUpper(TextInfo text)
    {
        if (length == 0)
        {
            return this;
        }

        for (int i = 0; i < length; i++)
        {
            _buffer[i] = text.ToUpper(_buffer[i]);
        }

        return this;
    }

    public override string ToString()
    {
        if (length == 0)
        {
            return string.Empty;
        }

        return new string(_buffer, 0, length);
    }

    public string ToString(int startIndex, int len)
    {
        if (startIndex < 0 || len < 0 || startIndex > this.length - len)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (length == 0)
        {
            return string.Empty;
        }

        return new string(_buffer, startIndex, len);            
    }

    public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        if (Length - count < sourceIndex || destination.Length - count < destinationIndex || sourceIndex < 0 || destinationIndex < 0 || count < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        for (int i = 0; i < count; i++)
        {
            destination[destinationIndex + i] = _buffer[sourceIndex + i];
        }
    }

    public unsafe string CopyToString(string str)
    {
        if (str.Length == length)
        {
            fixed (char* src = _buffer, dest = str)
            {
                CharCopy(dest, src, length);
            }

            return str;
        }
        else
        {
            char[] buffer = new char[length];
            Buffer.BlockCopy(_buffer, 0, buffer, 0, length * sizeof(char));
            return new string(buffer, 0, length);
        }
    }

    public unsafe string CopyToString(string str, int len)
    {
        fixed (char* src = _buffer, dest = str)
        {
            CharCopy(dest, src, len);
        }

        return str;
    }

    public unsafe string CopyToString(int srcOffset, string dst, int destOffset, int count)
    {
        fixed (char* src = _buffer, dest = dst)
        {
            CharCopy(dest + destOffset, src + srcOffset, count);
        }

        return dst;
    }

    //internal static StringBuilder FormatHelper(StringBuilder result, IFormatProvider provider, string format, params object[] args)
    //{
    //    if (format == null)
    //    {
    //        throw new ArgumentNullException("format");
    //    }

    //    if (args == null)
    //    {
    //        throw new ArgumentNullException("args");
    //    }

    //    if (result == null)
    //    {
    //        /* Try to approximate the size of result to avoid reallocations */
    //        int i, len;
    //        len = 0;

    //        for (i = 0; i < args.Length; ++i)
    //        {
    //            string s = args[i] as string;

    //            if (s != null)
    //            {
    //                len += s.Length;
    //            }
    //            else
    //            {
    //                break;
    //            }
    //        }

    //        if (i == args.Length)
    //        {
    //            result = new StringBuilder(len + format.Length);
    //        }
    //        else
    //        {
    //            result = new StringBuilder();
    //        }
    //    }

    //    int ptr = 0;
    //    int start = ptr;

    //    while (ptr < format.Length)
    //    {
    //        char c = format[ptr++];

    //        if (c == '{')
    //        {
    //            result.Append(format, start, ptr - start - 1);

    //            // check for escaped open bracket

    //            if (format[ptr] == '{')
    //            {
    //                start = ptr++;
    //                continue;
    //            }

    //            // parse specifier

    //            int n, width;
    //            bool left_align;
    //            CString arg_format;
    //            ParseFormatSpecifier(format, ref ptr, out n, out width, out left_align, out arg_format);

    //            if (n >= args.Length)
    //            {
    //                throw new FormatException("Index (zero based) must be greater than or equal to zero and less than the size of the argument list.");
    //            }

    //            // format argument

    //            object arg = args[n];
    //            string str;
    //            ICustomFormatter formatter = null;

    //            if (provider != null)
    //            {
    //                formatter = provider.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;
    //            }
    //            if (arg == null)
    //            {
    //                str = String.Empty;
    //            }

    //            else if (formatter != null)
    //            {
    //                str = formatter.Format(arg_format.ToString(), arg, provider); //todo:fixed tostring
    //            }
    //            else if (arg is IFormattable)
    //            {
    //                str = ((IFormattable)arg).ToString(arg_format == , provider);                    
    //            }
    //            else
    //            {
    //                str = arg.ToString();
    //            }

    //            // pad formatted string and append to result

    //            if (width > str.Length)
    //            {
    //                const char padchar = ' ';
    //                int padlen = width - str.Length;

    //                if (left_align)
    //                {
    //                    result.Append(str);
    //                    result.Append(padchar, padlen);
    //                }
    //                else
    //                {
    //                    result.Append(padchar, padlen);
    //                    result.Append(str);
    //                }
    //            }
    //            else
    //            {
    //                result.Append(str);
    //            }

    //            start = ptr;
    //        }
    //        else if (c == '}' && ptr < format.Length && format[ptr] == '}')
    //        {
    //            result.Append(format, start, ptr - start - 1);
    //            start = ptr++;
    //        }
    //        else if (c == '}')
    //        {
    //            throw new FormatException("Input string was not in a correct format.");
    //        }
    //    }

    //    if (start < format.Length)
    //    {
    //        result.Append(format, start, format.Length - start);
    //    }

    //    return result;
    //}

    //public static CString Copy(CString str)
    //{
    //    if (str == null)
    //    {
    //        throw new ArgumentNullException("str");
    //    }

    //    return str.SubstringUnchecked(0, str.length);
    //}

    public unsafe static CString Concat(CString str0, CString str1)
    {
        if (str0 == null || str0.Length == 0)
        {
            if (str1 == null || str1.Length == 0)
            {
                return Alloc(0);
            }

            return str1;
        }

        if (str1 == null || str1.Length == 0)
        {
            return str0;
        }

        int count = str0.length + str1.length;
        CString tmp = Alloc(count);

        fixed (char* dest = tmp._buffer, src = str0._buffer)
        {
            CharCopy(dest, src, str0.length);
        }

        fixed (char* dest = tmp._buffer, src = str1._buffer)
        {
            CharCopy(dest + str0.Length, src, str1.length);
        }

        tmp.length = count;
        return tmp;
    }

    public unsafe static CString Concat(CString str0, CString str1, CString str2)
    {
        if (str0 == null || str0.Length == 0)
        {
            if (str1 == null || str1.Length == 0)
            {
                if (str2 == null || str2.Length == 0)
                    return Alloc(0);
                return str2;
            }
            else
            {
                if (str2 == null || str2.Length == 0)
                    return str1;
            }
            str0 = Alloc(0);
        }
        else
        {
            if (str1 == null || str1.Length == 0)
            {
                if (str2 == null || str2.Length == 0)
                    return str0;
                else
                    str1 = Alloc(0);
            }
            else
            {
                if (str2 == null || str2.Length == 0)
                    str2 = Alloc(0);
            }
        }

        CString tmp = Alloc(str0.length + str1.length + str2.length);

        if (str0.Length != 0)
        {
            fixed (char* dest = tmp._buffer, src = str0._buffer)
            {
                CharCopy(dest, src, str0.length);                
            }
        }
        if (str1.Length != 0)
        {
            fixed (char* dest = tmp._buffer, src = str1._buffer)
            {
                CharCopy(dest + str0.Length, src, str1.length);                
            }
        }
        if (str2.Length != 0)
        {
            fixed (char* dest = tmp._buffer, src = str2._buffer)
            {
                CharCopy(dest + str0.Length + str1.Length, src, str2.length);                
            }
        }

        tmp.length = str0.length + str1.length + str2.length;
        return tmp;
    }

    public unsafe static CString Concat(CString str0, CString str1, CString str2, CString str3)
    {
        if (str0 == null && str1 == null && str2 == null && str3 == null)
            return String.Empty;

        if (str0 == null)
            str0 = Alloc(0);
        if (str1 == null)
            str1 = Alloc(0);
        if (str2 == null)
            str2 = Alloc(0);
        if (str3 == null)
            str3 = Alloc(0);

        CString tmp = Alloc(str0.length + str1.length + str2.length + str3.length);

        if (str0.Length != 0)
        {
            fixed (char* dest = tmp._buffer, src = str0._buffer)
            {
                CharCopy(dest, src, str0.length);
            }
        }
        if (str1.Length != 0)
        {
            fixed (char* dest = tmp._buffer, src = str1._buffer)
            {
                CharCopy(dest + str0.Length, src, str1.length);
            }
        }
        if (str2.Length != 0)
        {
            fixed (char* dest = tmp._buffer, src = str2._buffer)
            {
                CharCopy(dest + str0.Length + str1.Length, src, str2.length);
            }
        }
        if (str3.Length != 0)
        {
            fixed (char* dest = tmp._buffer, src = str3._buffer)
            {
                CharCopy(dest + str0.Length + str1.Length + str2.Length, src, str3.length);
            }
        }

        tmp.length = str0.length + str1.length + str2.length + str3.length;
        return tmp;
    }

    public unsafe CString Append(CString right)
    {
        int count = length + right.length;
        EnsureCapacity(count);

        fixed (char* dest = _buffer, src = right._buffer)
        {
            CharCopy(dest + length, src, right.length);
        }

        length = count;
        return this;
    }

    public unsafe CString Append(char value)
    {
        EnsureCapacity(length + 1);
        _buffer[length++] = value;
        return this;
    }

    public unsafe CString Append(short value)
    {
        EnsureCapacity(length + 8);

        fixed (char* p = _buffer)
        {            
            length += NumberFormatter.NumberToString(p + length, (int)value, null);
        }

        return this;
    }

    public unsafe CString Append(ushort value)
    {
        EnsureCapacity(length + 8);

        fixed (char* p = _buffer)
        {            
            length += NumberFormatter.NumberToString(p + length, (int)value, null);
        }

        return this;
    }

    public unsafe CString Append(byte value)
    {
        EnsureCapacity(length + 8);

        fixed (char* p = _buffer)
        {            
            length += NumberFormatter.NumberToString(p + length, (int)value, null);
        }

        return this;
    }

    public unsafe CString Append(sbyte value)
    {
        EnsureCapacity(length + 8);

        fixed (char* p = _buffer)
        {            
            length += NumberFormatter.NumberToString(p + length, (int)value, null);
        }

        return this;
    }

    public unsafe CString Append(int value)
    {
        EnsureCapacity(length + 16);

        fixed (char* p = _buffer)
        {            
            length += NumberFormatter.NumberToString(p + length, value, null);
        }

        return this;
    }

    public unsafe CString Append(uint value)
    {
        EnsureCapacity(length + 16);

        fixed (char* p = _buffer)
        {            
            length += NumberFormatter.NumberToString(p + length, value, null);
        }

        return this;
    }

    public unsafe CString Append(long value)
    {
        EnsureCapacity(length + 32);

        fixed (char* p = _buffer)
        {            
            length += NumberFormatter.NumberToString(p + length, value, null);
        }

        return this;
    }

    public unsafe CString Append(ulong value)
    {
        EnsureCapacity(length + 32);

        fixed (char* p = _buffer)
        {            
            length += NumberFormatter.NumberToString(p + length, value, null);
        }

        return this;
    }

    public unsafe CString Append(float value)
    {
        EnsureCapacity(length + 16);

        fixed (char* p = _buffer)
        {            
            length += NumberFormatter.NumberToString(p + length, value, null);
        }

        return this;
    }

    public unsafe CString Append(double value)
    {
        EnsureCapacity(length + 32);

        fixed (char* p = _buffer)
        {            
            length += NumberFormatter.NumberToString(p + length, value, null);
        }

        return this;
    }

    public unsafe CString Append(bool value)
    {
        EnsureCapacity(length + 5);        
        
        if (value)
        {
            _buffer[length++] = 'T';
            _buffer[length++] = 'r';
            _buffer[length++] = 'u';
            _buffer[length++] = 'e';
        }
        else
        {
            _buffer[length++] = 'F';
            _buffer[length++] = 'a';
            _buffer[length++] = 'l';
            _buffer[length++] = 's';
            _buffer[length++] = 'e';
        }
        
        return this;
    }

    public unsafe CString Append(string value)
    {
        int count = length + value.Length;
        EnsureCapacity(count);

        fixed(char* dest = _buffer, src = value)
        {
            CharCopy(dest + length, src, value.Length);
        }

        length = count;
        return this;
    }

    public unsafe CString Append(char[] value)
    {
        if (value == null)
        {
            return this;
        }

        int size = length + value.Length;
        EnsureCapacity(size);

        fixed (char* dest = _buffer, src = value)
        {
            CharCopy(dest + length, src, value.Length);
        }
        
        length = size;
        return this;
    }

    public CString Append(char c, int repeatCount)
    {
        if (repeatCount < 0)
        {
            throw new ArgumentOutOfRangeException("count", "Cannot be negative.");
        }

        EnsureCapacity(length + repeatCount);

        for (int i = 0; i < repeatCount; i++)
        {
            _buffer[length + i] = c;
        }

        length += repeatCount;
        return this;
    }

    public CString Append(char[] value, int startIndex, int charCount)
    {
        int count = length + charCount;
        EnsureCapacity(count);

        for (int i = 0; i < charCount; i++)
        {
            _buffer[length + i] = value[startIndex + i];
        }

        length = count;
        return this;
    }

    public unsafe CString Append(string value, int startIndex, int count)
    {
        if (value == null)
        {
            if (startIndex != 0 && count != 0)
            {
                throw new ArgumentNullException("value");
            }

            return this;
        }

        if (count < 0 || startIndex < 0 || startIndex > value.Length - count)
        {
            throw new ArgumentOutOfRangeException();
        }

        int size = count + length;
        EnsureCapacity(size);

        fixed (char* dest = _buffer, src = value)
        {
            CharCopy(dest + length, src + startIndex, count);
        }

        length = size;
        return this;
    }

    public unsafe CString AppendFormat(string format, short value)
    {
        EnsureCapacity(length + 8);

        fixed (char* p = _buffer)
        {
            length += NumberFormatter.NumberToString(p + length, format, (int)value, null);
        }

        return this;
    }

    public unsafe CString AppendFormat(string format, ushort value)
    {
        EnsureCapacity(length + 8);

        fixed (char* p = _buffer)
        {
            length += NumberFormatter.NumberToString(p + length, format, (int)value, null);
        }

        return this;
    }

    public unsafe CString AppendFormat(string format, byte value)
    {
        EnsureCapacity(length + 8);

        fixed (char* p = _buffer)
        {
            length += NumberFormatter.NumberToString(p + length, format, (int)value, null);
        }

        return this;
    }

    public unsafe CString AppendFormat(string format, sbyte value)
    {
        EnsureCapacity(length + 8);

        fixed (char* p = _buffer)
        {
            length += NumberFormatter.NumberToString(p + length, format, (int)value, null);
        }

        return this;
    }

    public unsafe CString AppendFormat(string format, int value)
    {
        EnsureCapacity(length + 32);

        fixed (char* p = _buffer)
        {            
            length += NumberFormatter.NumberToString(p + length, format, value, null);
        }

        return this;
    }

    public unsafe CString AppendFormat(string format, uint value)
    {
        EnsureCapacity(length + 32);

        fixed (char* p = _buffer)
        {
            length += NumberFormatter.NumberToString(p + length, format, value, null);
        }

        return this;
    }

    public unsafe CString AppendFormat(string format, long value)
    {
        EnsureCapacity(length + 64);

        fixed (char* p = _buffer)
        {
            length += NumberFormatter.NumberToString(p + length, format, value, null);
        }

        return this;
    }

    public unsafe CString AppendFormat(string format, ulong value)
    {
        EnsureCapacity(length + 64);

        fixed (char* p = _buffer)
        {
            length += NumberFormatter.NumberToString(p + length, format, value, null);
        }

        return this;
    }

    public unsafe CString AppendFormat(string format, float value)
    {
        EnsureCapacity(length + 32);

        fixed (char* p = _buffer)
        {
            length += NumberFormatter.NumberToString(p + length, format, value, null);
        }

        return this;
    }

    public unsafe CString AppendFormat(string format, double value)
    {
        EnsureCapacity(length + 64);

        fixed (char* p = _buffer)
        {
            length += NumberFormatter.NumberToString(p + length, format, value, null);
        }

        return this;
    }


    public CString AppendLine()
    {
        return Append(NewLine);      
    }

    public CString AppendLine(string value)
    {        
        return Append(value).Append(NewLine);        
    }

    public CString Insert(int index, char[] value)
    {
        return Insert(index, value, 0, value.Length);
    }

    public unsafe CString Insert(int index, char[] value, int startIndex, int count)
    {
        if (value == null)
        {
            if (startIndex == 0 && count == 0)
            {
                return this;
            }

            throw new ArgumentNullException("value");
        }

        if (index > length || index < 0 || count < 0 || startIndex < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        EnsureCapacity(length + count);

        fixed (char* dest = _buffer, src = value)
        {
            CharCopyReverse(dest + index + count, dest + index, length - index);
            CharCopy(dest + index, src + startIndex, count);
        }

        length += count;
        return this;
    }

    public unsafe CString Insert(int index, string value)
    {
        if (index > length || index < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (value == null || value.Length == 0)
        {
            return this;
        }

        EnsureCapacity(length + value.Length);
        
        fixed(char* dest = _buffer, src = value)
        {
            CharCopyReverse(dest + index + value.Length, dest + index, length - index);            
            CharCopy(dest + index, src, value.Length);
        }

        length += value.Length;
        return this;
    }

    public CString Insert(int index, string value, int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (value != null && value != String.Empty)
        {
            for (int i = 0; i < count; i++)
            {
                Insert(index, value);
            }
        }

        return this;
    }

    public unsafe CString Insert(int index, CString value)
    {
        Insert(index, value._buffer, 0, value.length);
        return this;
    }

    static char[] numbuffer = new char[64];

    public CString Insert(int index, bool value)
    {
        return Insert(index, value ? "True" : "False");
    }

    public CString Insert(int index, byte value)
    {                
        return Insert(index, (int)value);
    }

    public CString Insert(int index, short value)
    {
        return Insert(index, (int)value);
    }

    public CString Insert(int index, sbyte value)
    {
        return Insert(index, (int)value);
    }

    public CString Insert(int index, ushort value)
    {
        return Insert(index, (int)value);
    }

    public unsafe CString Insert(int index, char value)
    {
        if (index > length || index < 0)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        EnsureCapacity(length + 1);

        fixed (char* dest = _buffer)
        {
            CharCopyReverse(dest + index + 1, dest + index, length - index);
        }

        _buffer[index] = value;        
        ++length;
        return this;
    }

    public unsafe CString Insert(int index, float value)
    {
        int len = -1;

        fixed (char* p = numbuffer)
        {
            len = NumberFormatter.NumberToString(p, value, null);
        }

        return Insert(index, numbuffer, 0, len);
    }

    public unsafe CString Insert(int index, double value)
    {
        int len = -1;

        fixed (char* p = numbuffer)
        {
            len = NumberFormatter.NumberToString(p, value, null);
        }

        return Insert(index, numbuffer, 0, len);
    }

    public unsafe CString Insert(int index, int value)
    {
        int len = -1;
         
        fixed (char* p = numbuffer)
        {            
            len = NumberFormatter.NumberToString(p, value, null);
        }
        
        return Insert(index, numbuffer, 0, len);
    }

    public unsafe CString Insert(int index, long value)
    {
        int len = -1;

        fixed (char* p = numbuffer)
        {
            len = NumberFormatter.NumberToString(p, value, null);
        }

        return Insert(index, numbuffer, 0, len);
    } 

    public unsafe CString Insert(int index, uint value)
    {
        int len = -1;

        fixed (char* p = numbuffer)
        {
            len = NumberFormatter.NumberToString(p, value, null);
        }

        return Insert(index, numbuffer, 0, len);        
    }
    
    public unsafe CString Insert(int index, ulong value)
    {
        int len = -1;

        fixed (char* p = numbuffer)
        {
            len = NumberFormatter.NumberToString(p, value, null);
        }

        return Insert(index, numbuffer, 0, len);
    }

    public CString Insert(int index, object value)
    {
        return Insert(index, value.ToString());
    }

    public static CString Join(string separator, CString[] value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (separator == null)
        {
            separator = String.Empty;
        }

        return JoinUnchecked(separator, value, 0, value.Length);
    }

    public static CString Join(string separator, CString[] value, int startIndex, int count)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException("startIndex", "< 0");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count", "< 0");
        }

        if (startIndex > value.Length - count)
        {
            throw new ArgumentOutOfRangeException("startIndex", "startIndex + count > value.length");
        }

        if (startIndex == value.Length)
        {
            return String.Empty;
        }

        if (separator == null)
        {
            separator = String.Empty;
        }

        return JoinUnchecked(separator, value, startIndex, count);
    }

    private static unsafe CString JoinUnchecked(string separator, CString[] value, int startIndex, int count)
    {
        int length = 0;
        int maxIndex = startIndex + count;
        // Precount the number of characters that the resulting string will have
        for (int i = startIndex; i < maxIndex; i++)
        {
            CString s = value[i];

            if (s != null)
            {
                length += s.length;
            }
        }

        length += separator.Length * (count - 1);

        if (length <= 0)
        {
            return String.Empty;
        }

        CString tmp = Alloc(length);
        maxIndex--;

        fixed (char* dest = tmp._buffer, sepsrc = separator)
        {
            // Copy each string from value except the last one and add a separator for each
            int pos = 0;
            for (int i = startIndex; i < maxIndex; i++)
            {
                CString source = value[i];

                if (source != null)
                {
                    if (source.Length > 0)
                    {
                        fixed (char* src = source._buffer)
                            CharCopy(dest + pos, src, source.Length);
                        pos += source.Length;
                    }
                }
                if (separator.Length > 0)
                {
                    CharCopy(dest + pos, sepsrc, separator.Length);
                    pos += separator.Length;
                }
            }
            // Append last string that does not get an additional separator
            CString sourceLast = value[maxIndex];

            if (sourceLast != null)
            {
                if (sourceLast.Length > 0)
                {
                    fixed (char* src = sourceLast._buffer)
                        CharCopy(dest + pos, src, sourceLast.Length);
                }
            }
        }

        return tmp;
    }


    //private static void ParseFormatSpecifier(string str, ref int ptr, out int n, out int width, out bool left_align, out CString format)
    //{
    //    try
    //    {
    //        // N = argument number (non-negative integer)
    //        n = ParseDecimal(str, ref ptr);

    //        if (n < 0)
    //        {
    //            throw new FormatException("Input string was not in a correct format.");
    //        }

    //        // M = width (non-negative integer)

    //        if (str[ptr] == ',')
    //        {
    //            // White space between ',' and number or sign.
    //            ++ptr;
    //            while (Char.IsWhiteSpace(str[ptr]))
    //            {
    //                ++ptr;
    //            }

    //            int start = ptr;
    //            format = new CString(str.Substring(start, ptr - start));
    //            left_align = (str[ptr] == '-');

    //            if (left_align)
    //            {
    //                ++ptr;
    //            }

    //            width = ParseDecimal(str, ref ptr);

    //            if (width < 0)
    //            {
    //                throw new FormatException("Input string was not in a correct format.");
    //            }
    //        }
    //        else
    //        {
    //            width = 0;
    //            left_align = false;
    //            format = new CString();
    //        }

    //        if (str[ptr] == ':')
    //        {
    //            int start = ++ptr;

    //            while (str[ptr] != '}')
    //            {
    //                ++ptr;
    //            }

    //            format.Append(str.Substring(start, ptr - start));
    //        }
    //        else
    //        {
    //            format = null;
    //        }

    //        if (str[ptr++] != '}')
    //        {
    //            throw new FormatException("Input string was not in a correct format.");
    //        }
    //    }
    //    catch (IndexOutOfRangeException)
    //    {
    //        throw new FormatException("Input string was not in a correct format.");
    //    }
    //}

    //private static void ParseFormatSpecifier(string str, ref int ptr, out int n, out int width, out bool left_align, out string format)
    //{
    //    try
    //    {
    //        // N = argument number (non-negative integer)
    //        n = ParseDecimal(str, ref ptr);

    //        if (n < 0)
    //        {
    //            throw new FormatException("Input string was not in a correct format.");
    //        }

    //        // M = width (non-negative integer)

    //        if (str[ptr] == ',')
    //        {
    //            // White space between ',' and number or sign.
    //            ++ptr;
    //            while (Char.IsWhiteSpace(str[ptr]))
    //            {
    //                ++ptr;
    //            }

    //            int start = ptr;
    //            format = str.Substring(start, ptr - start);
    //            left_align = (str[ptr] == '-');

    //            if (left_align)
    //            {
    //                ++ptr;
    //            }

    //            width = ParseDecimal(str, ref ptr);

    //            if (width < 0)
    //            {
    //                throw new FormatException("Input string was not in a correct format.");
    //            }
    //        }
    //        else
    //        {
    //            width = 0;
    //            left_align = false;
    //            format = string.Empty;
    //        }

    //        if (str[ptr] == ':')
    //        {
    //            int start = ++ptr;

    //            while (str[ptr] != '}')
    //            {
    //                ++ptr;
    //            }

    //            format += str.Substring(start, ptr - start);
    //        }
    //        else
    //        {
    //            format = null;
    //        }

    //        if (str[ptr++] != '}')
    //        {
    //            throw new FormatException("Input string was not in a correct format.");
    //        }
    //    }
    //    catch (IndexOutOfRangeException)
    //    {
    //        throw new FormatException("Input string was not in a correct format.");
    //    }
    //}


    //private static int ParseDecimal(string str, ref int ptr)
    //{
    //    int p = ptr;
    //    int n = 0;
    //    while (true)
    //    {
    //        char c = str[p];
    //        if (c < '0' || '9' < c) break;
    //        n = n * 10 + c - '0';
    //        ++p;
    //    }

    //    if (p == ptr)
    //    {
    //        return -1;
    //    }

    //    ptr = p;
    //    return n;
    //}

    internal static unsafe void memset(byte* dest, int val, int len)
    {
        if (len < 8)
        {
            while (len != 0)
            {
                *dest = (byte)val;
                ++dest;
                --len;
            }
            return;
        }

        if (val != 0)
        {
            val = val | (val << 8);
            val = val | (val << 16);
        }
        // align to 4
        int rest = (int)dest & 3;

        if (rest != 0)
        {
            rest = 4 - rest;
            len -= rest;
            do
            {
                *dest = (byte)val;
                ++dest;
                --rest;
            } while (rest != 0);
        }

        while (len >= 16)
        {
            ((int*)dest)[0] = val;
            ((int*)dest)[1] = val;
            ((int*)dest)[2] = val;
            ((int*)dest)[3] = val;
            dest += 16;
            len -= 16;
        }

        while (len >= 4)
        {
            ((int*)dest)[0] = val;
            dest += 4;
            len -= 4;
        }
        // tail bytes
        while (len > 0)
        {
            *dest = (byte)val;
            dest++;
            len--;
        }
    }

    static unsafe void memcpy4(byte* dest, byte* src, int size)
    {
        /*while (size >= 32) {
            // using long is better than int and slower than double
            // FIXME: enable this only on correct alignment or on platforms
            // that can tolerate unaligned reads/writes of doubles
            ((double*)dest) [0] = ((double*)src) [0];
            ((double*)dest) [1] = ((double*)src) [1];
            ((double*)dest) [2] = ((double*)src) [2];
            ((double*)dest) [3] = ((double*)src) [3];
            dest += 32;
            src += 32;
            size -= 32;
        }*/
        while (size >= 16)
        {
            ((int*)dest)[0] = ((int*)src)[0];
            ((int*)dest)[1] = ((int*)src)[1];
            ((int*)dest)[2] = ((int*)src)[2];
            ((int*)dest)[3] = ((int*)src)[3];
            dest += 16;
            src += 16;
            size -= 16;
        }
        while (size >= 4)
        {
            ((int*)dest)[0] = ((int*)src)[0];
            dest += 4;
            src += 4;
            size -= 4;
        }
        while (size > 0)
        {
            ((byte*)dest)[0] = ((byte*)src)[0];
            dest += 1;
            src += 1;
            --size;
        }
    }
    static unsafe void memcpy2(byte* dest, byte* src, int size)
    {
        while (size >= 8)
        {
            ((short*)dest)[0] = ((short*)src)[0];
            ((short*)dest)[1] = ((short*)src)[1];
            ((short*)dest)[2] = ((short*)src)[2];
            ((short*)dest)[3] = ((short*)src)[3];
            dest += 8;
            src += 8;
            size -= 8;
        }

        while (size >= 2)
        {
            ((short*)dest)[0] = ((short*)src)[0];
            dest += 2;
            src += 2;
            size -= 2;
        }

        if (size > 0)
        {
            ((byte*)dest)[0] = ((byte*)src)[0];
        }
    }
    static unsafe void memcpy1(byte* dest, byte* src, int size)
    {
        while (size >= 8)
        {
            ((byte*)dest)[0] = ((byte*)src)[0];
            ((byte*)dest)[1] = ((byte*)src)[1];
            ((byte*)dest)[2] = ((byte*)src)[2];
            ((byte*)dest)[3] = ((byte*)src)[3];
            ((byte*)dest)[4] = ((byte*)src)[4];
            ((byte*)dest)[5] = ((byte*)src)[5];
            ((byte*)dest)[6] = ((byte*)src)[6];
            ((byte*)dest)[7] = ((byte*)src)[7];
            dest += 8;
            src += 8;
            size -= 8;
        }
        while (size >= 2)
        {
            ((byte*)dest)[0] = ((byte*)src)[0];
            ((byte*)dest)[1] = ((byte*)src)[1];
            dest += 2;
            src += 2;
            size -= 2;
        }

        if (size > 0)
        {
            ((byte*)dest)[0] = ((byte*)src)[0];
        }
    }

    internal static unsafe void memcpy(byte* dest, byte* src, int size)
    {
        // FIXME: if pointers are not aligned, try to align them
        // so a faster routine can be used. Handle the case where
        // the pointers can't be reduced to have the same alignment
        // (just ignore the issue on x86?)
        if ((((int)dest | (int)src) & 3) != 0)
        {
            if (((int)dest & 1) != 0 && ((int)src & 1) != 0 && size >= 1)
            {
                dest[0] = src[0];
                ++dest;
                ++src;
                --size;
            }
            if (((int)dest & 2) != 0 && ((int)src & 2) != 0 && size >= 2)
            {
                ((short*)dest)[0] = ((short*)src)[0];
                dest += 2;
                src += 2;
                size -= 2;
            }
            if ((((int)dest | (int)src) & 1) != 0)
            {
                memcpy1(dest, src, size);
                return;
            }
            if ((((int)dest | (int)src) & 2) != 0)
            {
                memcpy2(dest, src, size);
                return;
            }
        }
        memcpy4(dest, src, size);
    }

    internal static unsafe void CharCopy(char* dest, char* src, int count)
    {
        if ((((int)(byte*)dest | (int)(byte*)src) & 3) != 0)
        {
            if (((int)(byte*)dest & 2) != 0 && ((int)(byte*)src & 2) != 0 && count > 0)
            {
                ((short*)dest)[0] = ((short*)src)[0];
                dest++;
                src++;
                count--;
            }
            if ((((int)(byte*)dest | (int)(byte*)src) & 2) != 0)
            {
                memcpy2((byte*)dest, (byte*)src, count * 2);
                return;
            }
        }
        memcpy4((byte*)dest, (byte*)src, count * 2);
    }

    internal static unsafe void CharCopy(char[] target, char[] source, int count)
    {
        fixed(char* dest = target, src = source)
        {
            CharCopy(dest, src, count);
        }
    }

    internal static unsafe void CharCopyReverse(char* dest, char* src, int count)
    {
        dest += count;
        src += count;

        for (int i = count; i > 0; i--)
        {
            dest--;
            src--;
            *dest = *src;
        }
    }

    public bool IsRootedPath()
    {
        if (length == 0)
        {
            return false;
        }

        if (IndexOfAny(Path.GetInvalidPathChars()) != -1)
        {
            throw new ArgumentException("Illegal characters in path.");
        }

        char c = _buffer[0];
        bool dirEqualsVolume = (Path.DirectorySeparatorChar == Path.VolumeSeparatorChar);

        return (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar || (!dirEqualsVolume && length > 1 && _buffer[1] == Path.VolumeSeparatorChar));
    }    
}



