using System;


    public class wstring
    {
        private int m_length;
        private int m_maxLength;
        private string m_myString;
        public byte m_referenceCount;

        public int Length
        {
            get
            {
                return m_length;
            }
            set
            {
                if (value > 0 && value <= m_maxLength)
                {
                    m_length = value;
                }
                else
                {
                    ClearString();
                }
            }
        }

        public int MaxLength
        {
            get
            {
                return m_maxLength;
            }
        }

        public char this[int index]
        {
            get
            {
                if (index < MaxLength)
                {
                    return m_myString[index];
                }
                throw new IndexOutOfRangeException();
            }
        }

        public wstring(int Capacity)
        {
            m_maxLength = Capacity;
            m_myString = new string('\0', m_maxLength);
        }

        ~wstring()
        {
            m_myString = null;
        }

        public override string ToString()
        {
            return m_myString;
        }

        public unsafe void SetLength(int length)
        {
            fixed (char* ptr = m_myString)
            {
                int* ptr2 = (int*)ptr;
                if (length < 0 || length > m_maxLength)
                {
                    return;
                }
                ptr2[-1] = length;
                m_length = length;
                return;
            }
        }

        public unsafe void ClearString()
        {
            if (m_myString == null)
            {
                return;
            }
            m_length = 0;
            fixed (char* ptr = m_myString)
            {
                *ptr = '\0';
                return;
            }
        }

        public unsafe void SetChar(int index, char ch)
        {
            if (m_myString == null)
            {
                return;
            }
            if (index < 0 || index >= m_maxLength)
            {
                return;
            }
            fixed (char* ptr = m_myString)
            {
                if (index < m_length)
                {
                    ptr[index] = ch;
                }
                return;
            }
        }

        private unsafe void InternalInsert(int StartIndex, string textS, int SLength)
        {
            if (textS == null || m_myString == null)
            {
                return;
            }
            if (StartIndex < 0 || StartIndex >= m_maxLength)
            {
                return;
            }
            if (StartIndex + SLength > m_maxLength)
            {
                return;
            }
            if (m_length + SLength > m_maxLength)
            {
                return;
            }

            fixed (char* ptr = m_myString)
            {
                int i;
                for (i = m_length - 1; i >= StartIndex; i--)
                {
                    ptr[i + SLength] = m_myString[i];
                }
                for (i = 0; i < SLength; i++)
                {
                    ptr[StartIndex + i] = textS[i];
                }
                m_length += SLength;
                if (i < m_maxLength)
                {
                    ptr[m_length] = '\0';
                }
                return;
            }
        }

        public void Insert(int StartIndex, string textS, int SLength = -1)
        {
            if (textS == null)
            {
                return;
            }
            InternalInsert(StartIndex, textS, (SLength != -1) ? SLength : textS.Length);
        }

        public void Insert(int StartIndex, wstring textS, int SLength = -1)
        {
            if (textS == null)
            {
                return;
            }
            InternalInsert(StartIndex, textS.ToString(), (SLength != -1) ? SLength : textS.Length);
        }

        private unsafe void InternalAppend(string value, int Lengthv)
        {
            if (m_myString == null || value == null || Lengthv == 0)
            {
                return;
            }
            fixed (char* ptr = m_myString)
            {
                int num = 0;
                while (num < Lengthv && num + m_length < m_maxLength)
                {
                    ptr[num + m_length] = value[num];
                    if (value[num] == '\0')
                    {
                        break;
                    }
                    num++;
                }
                m_length = num + m_length;
                if (m_length < m_maxLength)
                {
                    ptr[m_length] = '\0';
                }
                return;
            }
        }

        public void Append(string value)
        {
            if (value == null)
            {
                return;
            }
            InternalAppend(value, value.Length);
        }

        public void Append(wstring value)
        {
            if (value == null)
            {
                return;
            }
            InternalAppend(value.ToString(), value.Length);
        }

        public unsafe void Append(char value)
        {
            if (m_myString == null)
            {
                return;
            }
            if (m_length < m_maxLength)
            {
                fixed (char* ptr = m_myString)
                {
                    ptr[m_length++] = value;
                    if (m_length < m_maxLength)
                    {
                        ptr[m_length] = '\0';
                    }
                }
            }
        }

        public unsafe void Append(char value, int repeatCount)
        {
            if (m_myString == null || repeatCount <= 0)
            {
                return;
            }

            fixed (char* ptr = m_myString)
            {
                while (repeatCount > 0)
                {
                    if (m_length < m_maxLength)
                    {
                        ptr[m_length++] = value;
                        repeatCount--;
                    }
                    else
                    {
                        repeatCount = 0;
                    }
                }
                if (m_length < m_maxLength)
                {
                    ptr[m_length] = '\0';
                }
                return;
            }
        }

        private void InternalAppendFormat(string format, int lengthf)
        {
            if (m_myString == null || format == null)
            {
                return;
            }
            wstringManager instance = wstringManager.Instance;
            int num = 0;
            while (true)
            {
                char c;
                if (num < lengthf)
                {
                    c = format[num];
                    num++;
                    if (c == '}')
                    {
                        if (num >= lengthf || format[num] != '}')
                        {
                            break;
                        }
                        num++;
                    }
                    if (c == '{')
                    {
                        if (num >= lengthf || format[num] != '{')
                        {
                            num--;
                            goto IL_8C;
                        }
                        num++;
                    }
                    Append(c);
                    continue;
                }
                IL_8C:
                if (num == lengthf)
                {
                    return;
                }
                num++;
                if (num == lengthf || (c = format[num]) < '0' || c > '9')
                {
                    return;
                }
                int num2 = 0;
                do
                {
                    num2 = num2 * 10 + (int)c - 48;
                    num++;
                    if (num == lengthf)
                    {
                        return;
                    }
                    c = format[num];
                }
                while (c >= '0' && c <= '9' && num2 < 1000000);
                wstring[] formatS = instance.m_formatString;
                if (num2 >= formatS.Length)
                {
                    return;
                }
                while (num < lengthf && (c = format[num]) == ' ')
                {
                    num++;
                }
                bool flag = false;
                int num3 = 0;
                if (c == ',')
                {
                    num++;
                    while (num < lengthf && format[num] == ' ')
                    {
                        num++;
                    }
                    if (num == lengthf)
                    {
                        return;
                    }
                    c = format[num];
                    if (c == '-')
                    {
                        flag = true;
                        num++;
                        if (num == lengthf)
                        {
                            return;
                        }
                        c = format[num];
                    }
                    if (c < '0' || c > '9')
                    {
                        return;
                    }
                    do
                    {
                        num3 = num3 * 10 + (int)c - 48;
                        num++;
                        if (num == lengthf)
                        {
                            return;
                        }
                        c = format[num];
                        if (c < '0' || c > '9')
                        {
                            break;
                        }
                    }
                    while (num3 < 1000000);
                }
                while (num < lengthf && (c = format[num]) == ' ')
                {
                    num++;
                }
                wstring cString = instance.StaticString1024();
                if (c == ':')
                {
                    num++;
                    while (num != lengthf)
                    {
                        c = format[num];
                        num++;
                        if (c == '{')
                        {
                            if (num >= lengthf || format[num] != '{')
                            {
                                return;
                            }
                            num++;
                        }
                        else if (c == '}')
                        {
                            if (num >= lengthf || format[num] != '}')
                            {
                                num--;
                                goto IL_286;
                            }
                            num++;
                        }
                        cString.Append(c);
                    }
                    return;
                }
                IL_286:
                if (c != '}')
                {
                    return;
                }
                num++;
                wstring cString2 = null;
                if (formatS[num2] != null)
                {
                    cString2 = formatS[num2];
                }
                if (cString2 == null)
                {
                    cString2 = instance.StaticString1024();
                }
                int num4 = num3 - cString2.Length;
                if (!flag && num4 > 0)
                {
                    Append(' ', num4);
                }
                Append(cString2);
                if (flag && num4 > 0)
                {
                    Append(' ', num4);
                }
            }
        }

        public void AppendFormat(string format)
        {
            wstringManager.Instance.m_formatStringCount = 0;
            if (format == null)
            {
                return;
            }
            InternalAppendFormat(format, format.Length);
        }

        public void AppendFormat(wstring format)
        {
            wstringManager.Instance.m_formatStringCount = 0;
            if (format == null)
            {
                return;
            }
            InternalAppendFormat(format.ToString(), format.Length);
        }

        public bool IntToFormat(long x, int digits = 1, bool bNumber = false)
        {
            return wstringManager.Instance.IntToFormat(x, digits, bNumber);
        }

        public bool uLongToFormat(ulong x, int digits = 1, bool bNumber = false)
        {
            return wstringManager.Instance.uLongToFormat(x, digits, bNumber);
        }

        public bool FloatToFormat(float f, int afterpoint = -1, bool bAfterPointShowZero = true)
        {
            return wstringManager.Instance.FloatToFormat(f, afterpoint, bAfterPointShowZero);
        }

        public bool DoubleToFormat(double f, int afterpoint = -1, bool bAfterPointShowZero = true)
        {
            return wstringManager.Instance.DoubleToFormat(f, afterpoint, bAfterPointShowZero);
        }

        public bool StringToFormat(wstring tmpS)
        {
            return wstringManager.Instance.StringToFormat(tmpS);
        }

        public bool StringToFormat(string tmpS)
        {
            return wstringManager.Instance.StringToFormat(tmpS);
        }

        public unsafe void ToUpper()
        {
            fixed (char* ptr = m_myString)
            {
                for (int i = 0; i < m_length; i++)
                {
                    if ('a' <= m_myString[i] && m_myString[i] <= 'z')
                    {
                        ptr[i] = (char)((int)m_myString[i] & -33);
                    }
                    else
                    {
                        ptr[i] = m_myString[i];
                    }
                }
            }
        }

        public unsafe int GetHashCode(bool bToUpper = false)
        {
            int hashCode;
            if (bToUpper)
            {
                wstring cString = wstringManager.Instance.StaticString1024();
                fixed (char* ptr = cString.ToString())
                {
                    int num = 0;
                    while (num < m_length && num < cString.MaxLength)
                    {
                        if ('a' <= m_myString[num] && m_myString[num] <= 'z')
                        {
                            ptr[num] = (char)((int)m_myString[num] & -33);
                        }
                        else
                        {
                            ptr[num] = m_myString[num];
                        }
                        num++;
                    }
                    ptr[num] = '\0';
                    cString.SetLength(num);
                    hashCode = cString.ToString().GetHashCode();
                    cString.SetLength(cString.MaxLength);
                }
            }
            else
            {
                fixed (char* ptr2 = m_myString)
                {
                    SetLength(m_length);
                    hashCode = m_myString.GetHashCode();
                    SetLength(m_maxLength);
                }
            }
            return hashCode;
        }

        public void Substring(wstring s, int startIndex)
        {
            if (s == null || startIndex <= 0 || startIndex >= s.Length)
            {
                return;
            }
            Substring(s.ToString(), startIndex, s.Length - startIndex);
        }

        public void Substring(string s, int startIndex)
        {
            if (s == null || startIndex <= 0 || startIndex >= s.Length)
            {
                return;
            }
            Substring(s, startIndex, s.Length - startIndex);
        }

        public void Substring(string s, int startIndex, int length)
        {
            if (length == 0)
            {
                return;
            }
            InternalSubString(s, startIndex, length);
        }

        private unsafe void InternalSubString(string s, int startIndex, int length)
        {
            if (m_myString == null || s == null)
            {
                return;
            }

            fixed (char* ptr = m_myString)
            {
                int num = 0;
                while (num < length && num < m_maxLength && num + startIndex < s.Length)
                {
                    ptr[num] = s[num + startIndex];
                    if (s[num + startIndex] == '\0')
                    {
                        break;
                    }
                    num++;
                }
                m_length = num;
                if (m_length < m_maxLength)
                {
                    ptr[m_length] = '\0';
                }
                return;
            }
        }
    }

