using System;
using System.Collections.Generic;

    public class wstringManager
    {
        private static wstringManager _instance=new wstringManager();
        public static wstringManager Instance{ get { return _instance; } }

        public const int MAX_SSTRING = 50;

        private List<wstring> m_staticString = new List<wstring>();

        private int m_staticNowCount = -1;

        private int m_listCount = 10;

        private readonly int[] m_lengthArray = new int[] { 10, 50, 70, 100, 150, 300, 500, 800, 1200, 3500 };

        private readonly int[] m_countArray = new int[] { 150, 100, 50, 30, 20, 10, 10, 5, 3, 3 };

        private static readonly char[] m_numChar = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        private List<wstring> m_stringPool10 = new List<wstring>();

        private List<wstring> m_stringPool30 = new List<wstring>();

        private List<wstring> m_stringPool70 = new List<wstring>();

        private List<wstring> m_stringPool100 = new List<wstring>();

        private List<wstring> m_stringPool150 = new List<wstring>();

        private List<wstring> m_stringPool300 = new List<wstring>();

        private List<wstring> m_stringPool500 = new List<wstring>();

        private List<wstring> m_stringPool800 = new List<wstring>();

        private List<wstring> m_stringPool1200 = new List<wstring>();

        private List<wstring> m_stringPool3500 = new List<wstring>();

        public int m_formatStringCount;

        public wstring[] m_formatString = new wstring[50];

        public static string m_inputTemp = "1";

        public wstringManager()
        {
            for (int i = 0; i < 50; i++)
            {
                wstring item = new wstring(1024);
                m_staticString.Add(item);
            }
            for (int j = 0; j < m_listCount; j++)
            {
                List<wstring> list = GetList(j);
                if (list != null)
                {
                    for (int k = 0; k < m_countArray[j]; k++)
                    {
                        wstring item2 = new wstring(m_lengthArray[j]);
                        list.Add(item2);
                    }
                }
            }
            for (int l = 0; l < m_formatString.Length; l++)
            {
                wstring cString = new wstring(1024);
                m_formatString[l] = cString;
            }
        }

        ~wstringManager()
        {
        }

        public wstring StaticString1024()
        {
            m_staticNowCount++;
            if (m_staticNowCount >= 50)
            {
                m_staticNowCount = 0;
            }
            m_staticString[m_staticNowCount].ClearString();
            return m_staticString[m_staticNowCount];
        }

        private List<wstring> GetList(int Index)
        {
            switch (Index)
            {
                case 0:
                    return m_stringPool10;
                case 1:
                    return m_stringPool30;
                case 2:
                    return m_stringPool70;
                case 3:
                    return m_stringPool100;
                case 4:
                    return m_stringPool150;
                case 5:
                    return m_stringPool300;
                case 6:
                    return m_stringPool500;
                case 7:
                    return m_stringPool800;
                case 8:
                    return m_stringPool1200;
                case 9:
                    return m_stringPool3500;
                default:
                    return null;
            }
        }

        private int CalculateIndex(int StringLength)
        {
            int result = -1;
            if (StringLength <= m_lengthArray[0])
            {
                return 0;
            }
            for (int i = 1; i < m_listCount; i++)
            {
                if (StringLength > m_lengthArray[i - 1] && StringLength <= m_lengthArray[i])
                {
                    result = i;
                    break;
                }
            }
            return result;
        }

        private int DeSpawnFindIndex(int StringLength)
        {
            for (int i = 0; i < m_listCount; i++)
            {
                if (StringLength == m_lengthArray[i])
                {
                    return i;
                }
            }
            return -1;
        }

        public wstring SpawnString(int StringLength = 30)
        {
            wstring cString = null;
            int num = CalculateIndex(StringLength);
            if (num == -1)
            {
                return cString;
            }
            List<wstring> list = GetList(num);
            if (list == null)
            {
                return cString;
            }
            if (list.Count <= 0)
            {
                for (int i = 0; i < m_countArray[num]; i++)
                {
                    wstring item = new wstring(m_lengthArray[num]);
                    list.Add(item);
                }
            }
            cString = list[list.Count - 1];
            wstring expr_6F = cString;
            expr_6F.m_referenceCount += 1;
            cString.ClearString();
            list.RemoveAt(list.Count - 1);
            return cString;
        }

        public bool DeSpawnString(wstring str)
        {
            if (str == null)
            {
                return false;
            }
            if (str.m_referenceCount == 0)
            {
            }
            int num = DeSpawnFindIndex(str.MaxLength);
            if (num == -1)
            {
                return false;
            }
            List<wstring> list = GetList(num);
            if (list != null)
            {
                str.m_referenceCount -= 1;
                list.Add(str);
                return true;
            }
            return false;
        }

        private unsafe static void reverse(wstring s, int len)
        {
            if (s == null)
            {
                return;
            }
            int i = 0;
            int num = len - 1;
            while (i < num)
            {
                fixed (char* ptr = s.ToString())
                {
                    char c = ptr[i];
                    ptr[i] = ptr[num];
                    ptr[num] = c;
                    i++;
                    num--;
                }
            }
        }

        public unsafe static int IntToStr(wstring s, long x, int digits = 1, bool bNumber = false)
        {
            if (s == null)
            {
                return -1;
            }
            int i = 0;
            int num = 0;
            int num2 = (x >= 0L) ? 1 : -1;
            if (num2 < 0)
            {
                x *= -1L;
            }
            fixed (char* ptr = s.ToString())
            {
                while (x != 0L)
                {
                    if (bNumber && num == 3)
                    {
                        ptr[i++] = ',';
                        num = 0;
                    }
                    ptr[i++] = m_numChar[(int)(checked((IntPtr)(x % 10L)))];
                    x = (long)((double)x * 0.1);
                    if (bNumber)
                    {
                        num++;
                    }
                }
                while (i < digits)
                {
                    ptr[i++] = m_numChar[0];
                }
                if (num2 < 0)
                {
                    ptr[i++] = '-';
                }
                wstringManager.reverse(s, i);
                ptr[i] = '\0';
                s.Length = i;
                return i;
            }
        }

        public unsafe static int ulongToStr(wstring s, ulong x, int digits = 1, bool bNumber = false)
        {
            if (s == null)
            {
                return -1;
            }
            int i = 0;
            int num = 0;

            fixed (char* ptr = s.ToString())
            {
                while (x != 0uL)
                {
                    if (bNumber && num == 3)
                    {
                        ptr[i++] = ',';
                        num = 0;
                    }
                    ptr[i++] = m_numChar[(int)(checked((IntPtr)(x % 10uL)))];
                    x = (ulong)(x * 0.1);
                    if (bNumber)
                    {
                        num++;
                    }
                }
                while (i < digits)
                {
                    ptr[i++] = m_numChar[0];
                }
                wstringManager.reverse(s, i);
                ptr[i] = '\0';
                s.Length = i;
                return i;
            }
        }

        public unsafe static void FloatToStr(wstring s, float f, int afterpoint = -1, bool bAfterPointShowZero = true)
        {
            int num = 1;
            int num2 = -1;
            int num3 = (f >= 0f) ? 1 : -1;
            if (num3 < 0)
            {
                f *= -1f;
            }
            int num4;
            if (afterpoint < 0)
            {
                num4 = (int)f;
                float num5 = f - (float)num4;
                afterpoint = 0;
                while ((double)num5 != 0.0 && (double)num5 >= 0.0)
                {
                    num5 = f * (float)Math.Pow(10.0, (double)(afterpoint + 1));
                    num4 = (int)num5;
                    num5 -= (float)num4;
                    afterpoint++;
                }
            }
            else
            {
                float num6 = f;
                for (int i = 0; i < afterpoint; i++)
                {
                    num6 *= 10f;
                }
                num4 = (int)num6;
            }
            while ((f *= 0.1f) >= 1f)
            {
                num++;
            }
            if (!bAfterPointShowZero && afterpoint > 0)
            {
                int num7 = num4;
                int num8 = 0;
                for (int j = 0; j < afterpoint; j++)
                {
                    if (num7 % 10 != 0)
                    {
                        break;
                    }
                    num8++;
                    num7 /= 10;
                }
                if (num8 > 0)
                {
                    num4 /= (int)Math.Pow(10.0, (double)num8);
                    afterpoint -= num8;
                }
            }
            if (afterpoint > 0)
            {
                num2 = num;
                num = num + 1 + afterpoint;
            }
            if (num3 < 0)
            {
                num++;
                if (num2 != -1)
                {
                    num2++;
                }
            }

            fixed (char* ptr = s.ToString())
            {
                for (int j = num; j >= 0; j--)
                {
                    if (j == num)
                    {
                        ptr[j] = '\0';
                    }
                    else if (j == num2)
                    {
                        ptr[j] = '.';
                    }
                    else if (num3 < 0 && j == 0)
                    {
                        ptr[j] = '-';
                    }
                    else
                    {
                        int num9 = num4 % 10;
                        ptr[j] = m_numChar[num9];
                        num4 = (int)((float)num4 * 0.1f);
                    }
                }
                s.Length = num;
            }
        }

        public unsafe static void DoubleToStr(wstring s, double f, int afterpoint = -1, bool bAfterPointShowZero = true)
        {
            int num = 1;
            int num2 = -1;
            int num3 = (f >= 0.0) ? 1 : -1;
            if (num3 < 0)
            {
                f *= -1.0;
            }
            int num4;
            if (afterpoint < 0)
            {
                num4 = (int)f;
                double num5 = f - (double)num4;
                afterpoint = 0;
                while (num5 != 0.0 && num5 >= 0.0)
                {
                    num5 = f * Math.Pow(10.0, (double)(afterpoint + 1));
                    num4 = (int)num5;
                    num5 -= (double)num4;
                    afterpoint++;
                }
            }
            else
            {
                double num6 = f;
                for (int i = 0; i < afterpoint; i++)
                {
                    num6 *= 10.0;
                }
                num4 = (int)num6;
            }
            while ((f *= 0.10000000149011612) >= 1.0)
            {
                num++;
            }
            if (!bAfterPointShowZero && afterpoint > 0)
            {
                int num7 = num4;
                int num8 = 0;
                for (int j = 0; j < afterpoint; j++)
                {
                    if (num7 % 10 != 0)
                    {
                        break;
                    }
                    num8++;
                    num7 /= 10;
                }
                if (num8 > 0)
                {
                    num4 /= (int)Math.Pow(10.0, (double)num8);
                    afterpoint -= num8;
                }
            }
            if (afterpoint > 0)
            {
                num2 = num;
                num = num + 1 + afterpoint;
            }
            if (num3 < 0)
            {
                num++;
                if (num2 != -1)
                {
                    num2++;
                }
            }

            fixed (char* ptr = s.ToString())
            {
                for (int j = num; j >= 0; j--)
                {
                    if (j == num)
                    {
                        ptr[j] = '\0';
                    }
                    else if (j == num2)
                    {
                        ptr[j] = '.';
                    }
                    else if (num3 < 0 && j == 0)
                    {
                        ptr[j] = '-';
                    }
                    else
                    {
                        int num9 = num4 % 10;
                        ptr[j] = m_numChar[num9];
                        num4 = (int)((float)num4 * 0.1f);
                    }
                }
                s.Length = num;
            }
        }

        public bool IntToFormat(long x, int digits = 1, bool bNumber = false)
        {
            if (m_formatStringCount < m_formatString.Length)
            {
                wstringManager.IntToStr(m_formatString[m_formatStringCount], x, digits, bNumber);
                m_formatStringCount++;
                return true;
            }
            return false;
        }

        public bool uLongToFormat(ulong x, int digits = 1, bool bNumber = false)
        {
            if (m_formatStringCount < m_formatString.Length)
            {
                wstringManager.ulongToStr(m_formatString[m_formatStringCount], x, digits, bNumber);
                m_formatStringCount++;
                return true;
            }
            return false;
        }

        public bool FloatToFormat(float f, int afterpoint = -1, bool bAfterPointShowZero = true)
        {
            if (m_formatStringCount < m_formatString.Length)
            {
                wstringManager.FloatToStr(m_formatString[m_formatStringCount], f, afterpoint, bAfterPointShowZero);
                m_formatStringCount++;
                return true;
            }
            return false;
        }

        public bool DoubleToFormat(double f, int afterpoint = -1, bool bAfterPointShowZero = true)
        {
            if (m_formatStringCount < m_formatString.Length)
            {
                wstringManager.DoubleToStr(m_formatString[m_formatStringCount], f, afterpoint, bAfterPointShowZero);
                m_formatStringCount++;
                return true;
            }
            return false;
        }

        public bool StringToFormat(wstring tmpS)
        {
            if (m_formatStringCount < m_formatString.Length)
            {
                m_formatString[m_formatStringCount].ClearString();
                m_formatString[m_formatStringCount].Append(tmpS);
                m_formatStringCount++;
                return true;
            }
            return false;
        }

        public bool StringToFormat(string tmpS)
        {
            if (m_formatStringCount < m_formatString.Length)
            {
                m_formatString[m_formatStringCount].ClearString();
                m_formatString[m_formatStringCount].Append(tmpS);
                m_formatStringCount++;
                return true;
            }
            return false;
        }
    }

