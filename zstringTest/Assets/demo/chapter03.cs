using GameFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class chapter03 : MonoBehaviour
{
    Dictionary<string, int> dict = new Dictionary<string, int>();

    ProfilerBlock profiler = ProfilerBlock.Instance;

    public string outsideString;
    public string outsideString1;
    string bigString = new string('x', 10000);

    public bool bigStringTest = false;
    private void Start()
    {
        //gstring a = "a";
        using (gstring.Block())
        {
            gstring.gstring_block block01 = gstring.g_current_block;
            gstring.gstring_block block02;
            using (gstring.Block())
            {
                block02 = gstring.g_current_block;
            }
            gstring.gstring_block block03 = gstring.g_current_block;

            Debug.Log(object.ReferenceEquals(block01, block02));//False  False
            Debug.Log(object.ReferenceEquals(block01, block03));//False  True
            Debug.Log(object.ReferenceEquals(block02, block03));//False   false            
        }
        gstring b = "a";
    }
    void Update()
    {
        for (int n = 0; n < 1000; n++)
        {
            gstringTest();//github gtring
            stringTest();//原生string
            CStringTest();//github CString
            wstringTest();//王国纪元反编译版cstring
        }
    }
    void gstringTest()
    {
        using (profiler.Sample("gstring"))
        {
            using (gstring.Block())
            {
                using (profiler.Sample("Format"))
                {
                    gstring gf = gstring.Format("Number = {0}, Float = {1} String = {2}", 123, 3.148f, "Text");
                    int x = 10;
                }

                using (profiler.Sample("Concat"))
                {
                    gstring it = gstring.Concat("That's ", "a lot", " of", " strings", " to ", "concat");
                    int x = 10;
                }

                using (profiler.Sample("Substring + IndexOf + LastIndexOf"))
                {
                    gstring path = "Path/To/Some/File.txt";
                    int period = path.IndexOf('.');
                    var ext = path.Substring(period + 1);
                    var file = path.Substring(path.LastIndexOf('/') + 1, 4);
                    int x = 10;
                }

                using (profiler.Sample("Replace (char)"))
                {
                    gstring input = "This is some sort of text";
                    gstring replacement = input.Replace('o', '0').Replace('i', '1');
                    int x = 10;
                }

                using (profiler.Sample("Replace (string)"))
                {
                    gstring input = "m_This is the is is form of text";
                    gstring replacement = input.Replace("m_", "").Replace("is", "si");
                    int x = 10;
                }
                using (profiler.Sample("Concat + Intern"))
                {
                    for (int i = 0; i < 4; i++)
                        dict[gstring.Concat("Item", i).Intern()] = i;
                    outsideString1 = gstring.Concat("I'm ", "long ", "gone ", "by ", "the ", "end ", "of ", "this ", "gstring block");
                    outsideString = gstring.Concat("I'll ", "be ", "still ", "around ", "here").Intern();
                    int x = 10;
                }

                using (profiler.Sample("ToUpper + ToLower"))
                {
                    gstring s1 = "Hello";
                    gstring s2 = s1.ToUpper();
                    gstring s3 = s2 + s1.ToLower();
                    int x = 10;
                }
                if (!bigStringTest)
                {
                    return;
                }
                using (profiler.Sample("BigStringEval"))
                {
                    gstring s1 = bigString;
                    gstring s2 = s1 + "hello";
                }
            }
        }
    }
    void stringTest()
    {
        using (profiler.Sample("string"))
        {
            using (profiler.Sample("Format"))
            {
                string gf = string.Format("Number = {0}, Float = {1} String = {2}", 123, 3.148f, "Text");
                int x = 10;
            }

            using (profiler.Sample("Concat"))
            {
                string it = string.Concat("That's ", "a lot ", " of", " strings", " to ", "concat");
                int x = 10;
            }

            using (profiler.Sample("Substring + IndexOf + LastIndexOf"))
            {
                string path = "Path/To/Some/File.txt";
                int period = path.IndexOf('.');
                var ext = path.Substring(period + 1);
                var file = path.Substring(path.LastIndexOf('/') + 1, 4);
                int x = 10;
            }

            using (profiler.Sample("Replace (char)"))
            {
                string input = "This is some sort of text";
                string replacement = input.Replace('o', 'O').Replace('i', 'I');
                int x = 10;
            }

            using (profiler.Sample("Replace (string)"))
            {
                string input = "m_This is the is is form of text";
                string replacement = input.Replace("m_", "").Replace("is", "si");
                int x = 10;
            }

            using (profiler.Sample("ToUpper + ToLower"))
            {
                string s1 = "Hello";
                string s2 = s1.ToUpper();
                string s3 = s2 + s1.ToLower();
                int x = 10;
            }
            if (!bigStringTest)
            {
                return;
            }
            using (profiler.Sample("BigStringEval"))
            {
                string s1 = bigString;
                string s2 = s1 + "hello";
            }
        }
    }
    void CStringTest()
    {
        using (profiler.Sample("CString"))
        {
            using (profiler.Sample("Format"))
            {
                CString gf = string.Format("Number = {0}, Float = {1} String = {2}", 123, 3.148f, "Text");
                int x = 10;
            }

            using (profiler.Sample("Concat"))
            {
                using (CString a = "That's ", b = "a lot", c = " of", d = " strings", e = " to ", f = "concat", g = CString.Join(null, new CString[] { a, b, c, d, e, f })) ;
                int x = 10;
            }

            using (profiler.Sample("Substring + IndexOf + LastIndexOf"))
            {
                using (CString path = "Path/To/Some/File.txt")
                {
                    int period = path.IndexOf('.');
                    using (CString ext = path.Substring(period + 1), file = path.Substring(path.LastIndexOf('/') + 1, 4)) ;
                    int x = 10;
                }
            }

            using (profiler.Sample("Replace (char)"))
            {
                using (CString input = "This is some sort of text", replacement = input.Replace('o', '0').Replace('i', '1')) ;
                int x = 10;
            }

            using (profiler.Sample("Replace (string)"))
            {
                using (CString input = "m_This is the is is form of text", replacement = input.Replace("m_", "").Replace("is", "si")) ;
                int x = 10;
            }

            using (profiler.Sample("Concat + Intern"))
            {
                using (CString outsideString1 = CString.Join(null, new CString[] { "I'm ", "long", "gone ", "the ", "by ", "end ", "of ", "this ", "gstring block" }).ToString(), outsideString = string.Intern(CString.Join(null, new CString[] { "I'll ", "be ", "still ", "around ", "here " }).ToString())) ;
                int x = 10;
            }

            using (profiler.Sample("ToUpper + ToLower"))
            {
                using (CString s1 = "Hello", s2 = s1.ToUpper(), s3 = s2 + s1.ToLower()) ;
                int x = 10;
            }
            if (!bigStringTest)
            {
                return;
            }
            using (profiler.Sample("BigStringEval"))
            {
                using (CString s1 = bigString, s2 = s1 + "hello") ;
            }
        }
    }
    void wstringTest()
    {
        using (profiler.Sample("wstring"))
        {
            using (profiler.Sample("Format"))
            {
                wstring gf = wstringManager.Instance.StaticString1024();
                gf.IntToFormat(123);
                gf.FloatToFormat(3.148f);
                gf.Append("Text");
                gf.AppendFormat("Number = {0}, Float = {1} String = {2}");
                int x = 10;
                wstringManager.Instance.DeSpawnString(gf);
            }

            using (profiler.Sample("Concat"))
            {
                wstring gf = wstringManager.Instance.StaticString1024();
                gf.Append("That's ");
                gf.Append("a lot");
                gf.Append(" of");
                gf.Append(" strings");
                gf.Append(" to ");
                gf.Append("concat");
                gf.AppendFormat("{0}{1}{2}{3}{4}{5}");
                wstringManager.Instance.DeSpawnString(gf);
                int x = 10;
            }
            //功能缺失只能使用string替代
            using (profiler.Sample("Substring + IndexOf + LastIndexOf"))
            {
                string path = "Path/To/Some/File.txt";
                int period = path.IndexOf('.');
                var ext = path.Substring(period + 1);
                var file = path.Substring(path.LastIndexOf('/') + 1, 4);
                int x = 10;
            }

            //功能缺失只能使用string替代
            using (profiler.Sample("Replace (char)"))
            {
                string input = "This is some sort of text";
                string replacement = input.Replace('o', '0').Replace('i', '1');
                int x = 10;
            }
            //功能缺失只能使用string替代
            using (profiler.Sample("Replace (string)"))
            {
                string input = "m_This is the is is form of text";
                string replacement = input.Replace("m_", "").Replace("is", "si");
                int x = 10;
            }
            using (profiler.Sample("Concat + Intern"))
            {
                for (int i = 0; i < 4; i++)
                {
                    wstring gf = wstringManager.Instance.StaticString1024();
                    gf.StringToFormat("Item");
                    gf.IntToFormat(i);
                    gf.AppendFormat("{0}{1}");
                    dict[string.Intern(gf.ToString())] = i;
                    wstringManager.Instance.DeSpawnString(gf);
                }
                wstring gf02 = wstringManager.Instance.StaticString1024();
                gf02.Append("I'm ");
                gf02.Append("long ");
                gf02.Append("gone ");
                gf02.Append("by ");
                gf02.Append("the ");
                gf02.Append("end ");
                gf02.Append("of ");
                gf02.Append("this ");
                gf02.Append("gstring block ");
                gf02.AppendFormat("{0}{1}{2}{3}{4}{5}{6}{7}");
                outsideString1 = gf02.ToString();
                wstringManager.Instance.DeSpawnString(gf02);
                wstring gf03 = wstringManager.Instance.StaticString1024();
                gf03.Append("I'll ");
                gf03.Append("be ");
                gf03.Append("still ");
                gf03.Append("around ");
                gf03.Append("here");
                gf03.Append("{0}{1}{2}{3}{4}");
                outsideString = string.Intern(gf03.ToString());
                wstringManager.Instance.DeSpawnString(gf03);
                int x = 10;
            }

            using (profiler.Sample("ToUpper + ToLower"))
            {
                wstring gf = wstringManager.Instance.StaticString1024();
                gf.Append("Hello");
                gf.AppendFormat("{0}");
                gf.ToUpper();
                wstring gf02 = wstringManager.Instance.StaticString1024();
                gf02.Append(gf);
                gf02.Append(gf.ToString().ToLower());
                gf02.AppendFormat("{0}{1}");
                wstringManager.Instance.DeSpawnString(gf);
                wstringManager.Instance.DeSpawnString(gf02);
                int x = 10;
            }
            if (!bigStringTest)
            {
                return;
            }
            using (profiler.Sample("BigStringEval"))
            {
                wstring gf = wstringManager.Instance.StaticString1024();
                gf.Append(bigString);
                gf.Append("hello");
                gf.AppendFormat("{0}{1}");
            }
        }
    }
}
