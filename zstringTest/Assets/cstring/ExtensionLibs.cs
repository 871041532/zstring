using System;
using System.Globalization;

public static partial class ExtensionLibs
{
    public static CString ToCString(this System.Boolean boolean)
    {        
        return boolean ? "True" : "False";
    }

    public static CString ToCString(this System.SByte number)
    {
        CString str = CString.Alloc(8);
        str.Append(number);
        return str;
    }

    public static CString ToCString(this System.SByte number, string format)
    {
        CString str = CString.Alloc(8);
        str.AppendFormat(format, number);
        return str;
    }

    //public static CString ToCString(this System.SByte number, string format, IFormatProvider provider)
    //{
    //    CString str = CString.Alloc(8);
    //    str.NumberToString(format, (int)number, provider);
    //    return str;
    //}

    public static CString ToCString(this System.Byte number)
    {
        CString str = CString.Alloc(8);
        str.Append(number);
        return str;
    }

    public static CString ToCString(this System.Byte number, string format)
    {
        CString str = CString.Alloc(8);
        str.AppendFormat(format, number);
        return str;
    }

    //public static CString ToCString(this System.Byte number, string format, IFormatProvider provider)
    //{
    //    CString str = CString.Alloc(8);
    //    str.NumberToString(format, (int)number, provider);
    //    return str;
    //}

    public static CString ToCString(this System.Int16 number)
    {
        CString str = CString.Alloc(8);
        str.Append(number);
        return str;
    }

    public static CString ToCString(this System.Int16 number, string format)
    {
        CString str = CString.Alloc(16);
        str.AppendFormat(format, number);
        return str;
    }

    //public static CString ToCString(this System.Int16 number, string format, IFormatProvider provider)
    //{
    //    CString str = CString.Alloc(16);
    //    str.NumberToString(format, (int)number, provider);
    //    return str;
    //}

    public static CString ToCString(this System.UInt16 number)
    {
        CString str = CString.Alloc(8);
        str.Append(number);
        return str;
    }

    public static CString ToCString(this System.UInt16 number, string format)
    {
        CString str = CString.Alloc(16);
        str.AppendFormat(format, number);
        return str;
    }

    //public static CString ToCString(this System.UInt16 number, string format, IFormatProvider provider)
    //{
    //    CString str = CString.Alloc(16);
    //    str.NumberToString(format, (int)number, provider);
    //    return str;
    //}

    public static CString ToCString(this System.Int32 number)
    {
        CString str = CString.Alloc(16);
        str.Append(number);
        return str;
    }

    public static CString ToCString(this System.Int32 number, string format)
    {
        CString str = CString.Alloc(32);
        str.AppendFormat(format, number);
        return str;
    }

    //public static CString ToCString(this System.Int32 number, string format, IFormatProvider provider)
    //{
    //    CString str = CString.Alloc(32);
    //    str.NumberToString(format, number, provider);
    //    return str;
    //}

    public static CString ToCString(this System.UInt32 number)
    {
        CString str = CString.Alloc(16);
        str.Append(number);
        return str;
    }

    public static CString ToCString(this System.UInt32 number, string format)
    {
        CString str = CString.Alloc(32);
        str.AppendFormat(format, number);
        return str;
    }

    //public static CString ToCString(this System.UInt32 number, string format, IFormatProvider provider)
    //{
    //    CString str = CString.Alloc(32);
    //    str.NumberToString(format, number, provider);
    //    return str;
    //}

    public static CString ToCString(this System.Int64 number)
    {
        CString str = CString.Alloc(32);
        str.Append(number);
        return str;
    }

    public static CString ToCString(this System.Int64 number, string format)
    {
        CString str = CString.Alloc(64);
        str.AppendFormat(format, number);
        return str;
    }

    //public static CString ToCString(this System.Int64 number, string format, IFormatProvider provider)
    //{
    //    CString str = CString.Alloc(64);
    //    str.NumberToString(format, number, provider);
    //    return str;
    //}

    public static CString ToCString(this System.UInt64 number)
    {        
        CString str = CString.Alloc(32);
        str.Append(number);
        return str;
    }

    public static CString ToCString(this System.UInt64 number, string format)
    {
        CString str = CString.Alloc(64);
        str.AppendFormat(format, number);
        return str;
    }

    //public static CString ToCString(this System.UInt64 number, string format, IFormatProvider provider)
    //{
    //    CString str = CString.Alloc(64);
    //    str.NumberToString(format, number, provider);
    //    return str;
    //}

    public static CString ToCString(this System.Single number)
    {
        CString str = CString.Alloc(32);
        str.Append(number);
        return str;
    }

    public static CString ToCString(this System.Single number, string format)
    {
        CString str = CString.Alloc(64);
        str.AppendFormat(format, number);
        return str;
    }

    //public static CString ToCString(this System.Single number, string format, IFormatProvider provider)
    //{
    //    CString str = CString.Alloc(64);
    //    str.NumberToString(format, number, provider);
    //    return str;
    //}

    public static CString ToCString(this System.Double number)
    {
        CString str = CString.Alloc(64);
        str.Append(number);
        return str;
    }

    public static CString ToCString(this System.Double number, string format)
    {
        CString str = CString.Alloc(64);
        str.AppendFormat(format, number);
        return str;
    }

    //public static CString ToCString(this System.Double number, string format, IFormatProvider provider)
    //{
    //    CString str = CString.Alloc(64);
    //    str.NumberToString(format, number, provider);
    //    return str;
    //}

    /// <summary>
    /// 使用CString src覆盖dest字符串len长度内容
    /// </summary>
    /// <returns></returns>
    public static string ReplaceEx(this string dest, CString src, int len = -1)
    {
        if (len <= -1)
        {
            len = src.Length;
        }
        else if (len > src.Length)
        {
            throw new ArgumentOutOfRangeException("len > src.Length");
        }

        if (len > dest.Length)
        {
            throw new ArgumentOutOfRangeException("len > dest.Length");
        }

        return src.CopyToString(dest, len);
    }

    public static unsafe string ReplaceEx(this string dest, int offset, string src)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset", "Cannot be negative.");
        }

        if (offset >= dest.Length)
        {
            throw new ArgumentOutOfRangeException("offset >= dest.Length");
        }

        if (offset + src.Length > dest.Length)
        {
            throw new ArgumentOutOfRangeException("offset + src.Length > dest.Length");
        }

        fixed (char* dst = dest, s = src)
        {            
            CString.CharCopy(dst + offset, s, src.Length);
        }

        return dest;
    }

    public static unsafe string ReplaceEx(this string str, char oldChar, char newChar)
    {
        if (str.Length == 0 || oldChar == newChar)
        {
            return str;
        }

        fixed(char* p = str)
        {            
            for (int i = 0; i < str.Length; i++)
            {
                if (p[i] == oldChar)
                {
                    p[i] = newChar;
                }
            }
        }

        return str;
    }

    public static unsafe string ReplaceEx(this string str, string oldStr, string newStr)
    {
        if (oldStr.Length != newStr.Length)
        {
            throw new ArgumentOutOfRangeException("oldStr.Length != newStr.Length");
        }

        if (oldStr == null)
        {
            throw new ArgumentNullException("oldStr");
        }

        if (oldStr.Length == 0)
        {
            throw new ArgumentException("oldStr is the empty string.");
        }

        if (str.Length == 0 || oldStr == newStr || oldStr.Length > str.Length)
        {
            return str;
        }

        if (oldStr.Length == 1 && newStr.Length == 1)
        {
            return ReplaceEx(str, oldStr[0], newStr[0]);
        }

        int length = str.Length;
        int step = oldStr.Length;

        fixed (char* dst = str, s = newStr)
        {
            for (int i = 0; i < length;)
            {
                int found = str.IndexOf(oldStr, i, length - i);

                if (found < 0)
                {
                    break;
                }
                
                CString.CharCopy(dst + i, s, newStr.Length);                
                i = found + step;
            }
        }

        return str;
    }

    public static string ToLowerEx(this string str)
    {
        return ToLowerEx(str, CultureInfo.CurrentCulture);
    }

    public static string ToLowerEx(this string str, CultureInfo culture)
    {
        if (culture == null)
        {
            throw new ArgumentNullException("culture");
        }

        if (culture.LCID == 0x007F)
        {
            return ToLowerInvariant(str);
        }

        return ToLower(str, culture.TextInfo);
    }

    internal static unsafe string ToLowerInvariant(string str)
    {
        if (str.Length == 0)
        {
            return str;
        }

        fixed (char* dest = str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                dest[i] = Char.ToLowerInvariant(dest[i]);
            }
        }

        return str;
    }

    internal static unsafe string ToLower(string str, TextInfo text)
    {
        if (str.Length == 0)
        {
            return str;
        }

        fixed (char* dest = str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                dest[i] = text.ToLower(dest[i]);
            }
        }

        return str;
    }

    public static string ToUpperEx(this string str)
    {
        return ToUpperEx(str, CultureInfo.CurrentCulture);
    }

    public static string ToUpperEx(this string str, CultureInfo culture)
    {
        if (culture == null)
        {
            throw new ArgumentNullException("culture");
        }

        if (culture.LCID == 0x007F)
        {
            return ToUpperExInvariant(str);
        }

        return ToUpper(str, culture.TextInfo);
    }

    internal static unsafe string ToUpperExInvariant(string str)
    {
        if (str.Length == 0)
        {
            return str;
        }

        fixed (char* dest = str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                dest[i] = Char.ToUpperInvariant(dest[i]);
            }
        }

        return str;
    }

    internal static unsafe string ToUpper(string str, TextInfo text)
    {
        if (str.Length == 0)
        {
            return str;
        }

        fixed (char* dest = str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                dest[i] = text.ToUpper(dest[i]);
            }
        }

        return str;
    }

    public static unsafe CString SubStringEx(this string str, int startIndex)
    {
        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException("startIndex", "Cannot be negative.");
        }

        if (startIndex > str.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex", "Cannot exceed length of string.");
        }

        int len = str.Length - startIndex;

        if (len == 0)
        {
            return CString.Alloc(0);
        }

        if (startIndex == 0 && len == str.Length)
        {
            return str;
        }

        CString cstr = CString.Alloc(len);
        cstr.Append(str, startIndex, len);
        return cstr;
    }

    public static unsafe void CopyToEx(this string str, int startIndex, string outStr)
    {
        if (startIndex < 0)
        {
            throw new ArgumentOutOfRangeException("startIndex", "Cannot be negative.");
        }

        if (startIndex + outStr.Length > str.Length)
        {
            throw new ArgumentOutOfRangeException("startIndex", "Cannot exceed length of string.");
        }

        fixed (char* src = str, dest = outStr)
        {
            CString.CharCopy(dest, src + startIndex, outStr.Length);
        }
    }
}
