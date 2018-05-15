using UnityEngine;
using System;
using System.Collections.Generic;
using GameFramework;
using System.IO;
using System.Text;

public class Test : MonoBehaviour
{
    Dictionary<string, int> dict = new Dictionary<string, int>();

    ProfilerBlock profiler = ProfilerBlock.Instance;

    public string outsideString;
    public string outsideString1;
    string bigString= new string('x', 10000);

    public bool bigStringTest = false;

    void Start() 
    {
        using (zstring.Block())
        {
            zstring.zstring_block block01 = zstring.g_current_block;
            zstring.zstring_block block02;
            using (zstring.Block())
            {
                block02 = zstring.g_current_block;
            }
            zstring.zstring_block block03 = zstring.g_current_block;

            Debug.Log(object.ReferenceEquals(block01, block02));//False  False
            Debug.Log(object.ReferenceEquals(block01, block03));//False  True
            Debug.Log(object.ReferenceEquals(block02, block03));//True   false            
        }
        //zstring a = "hello";
        using (zstring.Block())
        {
            zstring a = "hello";
            zstring b = " 我曹の:-O";
            zstring c = a + b;
            zstring d = c + b;
            Debug.Log(d);
            Debug.Log(zstring.Format("aaa{0}{1}", "我曹", "喔"));
            //testSizeof();       
            testSizeof02();
        }
        testSizeof();
    }


    unsafe void testSizeof02()
    {
        string s = "Assets/ResourcesAssets/Prefabs/Scene/Battle/battle003/tx_PVE_alpha_model_003.prefab我曹";
        Debug.Log(s.Length);        
        Debug.Log(System.Text.Encoding.UTF8.GetBytes(s).Length);
        Debug.Log(s.Length * sizeof(char));
        fixed (char* sptr=s)
        {
            int startPos = (int)sptr;
            int endPos = (int)(sptr + s.Length);
            Debug.Log(endPos-startPos);
        }
    }
    unsafe void testSizeof()
    {
        Debug.Log(sizeof(Byte1));
        Debug.Log(sizeof(Byte2));
        Debug.Log(sizeof(Byte4));
        Debug.Log(sizeof(Byte8));
        Debug.Log(sizeof(Byte16));
        Debug.Log(sizeof(Byte32));
        Debug.Log(sizeof(Byte64));
        Debug.Log(sizeof(Byte128));
        Debug.Log(sizeof(Byte256));
        Debug.Log(sizeof(Byte512));
        Debug.Log(sizeof(Byte1024));
        Debug.Log(sizeof(Byte2048));
        Debug.Log(sizeof(Byte4096));
        Debug.Log(sizeof(Byte8192));

        var a1 = new Byte1[2];
        var a2 = new Byte2[2];
        var a4 = new Byte4[2];
        var a8 = new Byte8[2];
        var a16 = new Byte16[2];
        var a32 = new Byte32[2];
        var a64 = new Byte64[2];
        var a128 = new Byte128[2];
        var a256 = new Byte256[2];
        var a512 = new Byte512[2];
        var a1024 = new Byte1024[2];
        var a2048 = new Byte2048[2];
        var a4096 = new Byte4096[2];
        var a8192 = new Byte8192[2];

        fixed (Byte1* ptr = a1)
        {
            Debug.Log((int)(ptr+1)-(int)ptr);
        }        
        fixed (Byte2* ptr = a2)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte4* ptr = a4)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte8* ptr = a8)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte16* ptr = a16)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        };
        fixed (Byte32* ptr = a32)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte64* ptr = a64)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte128* ptr = a128)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte256* ptr = a256)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte512* ptr = a512)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte1024* ptr = a1024)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte2048* ptr = a2048)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte4096* ptr = a4096)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte8192* ptr = a8192)
        {
            Debug.Log((int)(ptr + 1) - (int)ptr);
        }
        fixed (Byte8192* ptr = a8192)
        {
            Byte16* ptr2 = (Byte16*)ptr;
            Debug.Log((int)(ptr2 + 1) - (int)ptr2);
        }

    }
    unsafe void change(string a, char b)
    {
        fixed (char* s = a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                s[i] = b;
            }
        }
    }
    void Update()
    {
        for (int n = 0; n < 1000; n++)
        {
            gstringTest();//github gtring bug修复版
            nstringTest();//gstring升级版nstring
            stringTest();//原生string
            CStringTest();//github CString
            wstringTest();//王国纪元反编译版cstring
            zstringTest();//骚操作版nstring
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
    void nstringTest()
    {
        using (profiler.Sample("nstring"))
        {
            using (nstring.Block())
            {
                using (profiler.Sample("Format"))
                {
                    nstring gf = nstring.Format("Number = {0}, Float = {1} String = {2}", 123, 3.148f, "Text");
                    int x = 10;
                }

                using (profiler.Sample("Concat"))
                {
                    nstring it = nstring.Concat("That's ", "a lot", " of", " strings", " to ", "concat");
                    int x = 10;
                }

                using (profiler.Sample("Substring + IndexOf + LastIndexOf"))
                {
                    nstring path = "Path/To/Some/File.txt";
                    int period = path.IndexOf('.');
                    var ext = path.Substring(period + 1);
                    var file = path.Substring(path.LastIndexOf('/') + 1, 4);
                    int x = 10;
                }

                using (profiler.Sample("Replace (char)"))
                {
                    nstring input = "This is some sort of text";
                    nstring replacement = input.Replace('o', '0').Replace('i', '1');
                    int x = 10;
                }

                using (profiler.Sample("Replace (string)"))
                {
                    nstring input = "m_This is the is is form of text";
                    nstring replacement = input.Replace("m_", "").Replace("is", "si");
                    int x = 10;
                }
                using (profiler.Sample("Concat + Intern"))
                {
                    for (int i = 0; i < 4; i++)
                        dict[nstring.Concat("Item", i).Intern()] = i;
                    outsideString1 = nstring.Concat("I'm ", "long ", "gone ", "by ", "the ", "end ", "of ", "this ", "gstring block");
                    outsideString = nstring.Concat("I'll ", "be ", "still ", "around ", "here").Intern();
                    int x = 10;
                }

                using (profiler.Sample("ToUpper + ToLower"))
                {
                    nstring s1 = "Hello";
                    nstring s2 = s1.ToUpper();
                    nstring s3 = s2 + s1.ToLower();
                    int x = 10;
                }
                if (!bigStringTest)
                {
                    return;
                }
                using (profiler.Sample("BigStringEval"))
                {
                    nstring s1 = bigString;
                    nstring s2 = s1 + "hello";
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
                using (CString s1=bigString,s2=s1+"hello") ;
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
                outsideString =string.Intern(gf03.ToString());
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
    void zstringTest()
    {
        using (profiler.Sample("zstring"))
        {
            using (zstring.Block())
            {
                using (profiler.Sample("Format"))
                {
                    zstring gf = zstring.Format("Number = {0}, Float = {1} String = {2}", 123, 3.148f, "Text");
                    int x = 10;
                }

                using (profiler.Sample("Concat"))
                {
                    zstring it = zstring.Concat("That's ", "a lot", " of", " strings", " to ", "concat");
                    int x = 10;
                }

                using (profiler.Sample("Substring + IndexOf + LastIndexOf"))
                {
                    zstring path = "Path/To/Some/File.txt";
                    int period = path.IndexOf('.');
                    var ext = path.Substring(period + 1);
                    var file = path.Substring(path.LastIndexOf('/') + 1, 4);
                    int x = 10;
                }

                using (profiler.Sample("Replace (char)"))
                {
                    zstring input = "This is some sort of text";
                    zstring replacement = input.Replace('o', '0').Replace('i', '1');
                    int x = 10;
                }

                using (profiler.Sample("Replace (string)"))
                {
                    zstring input = "m_This is the is is form of text";
                    zstring replacement = input.Replace("m_", "").Replace("is", "si");
                    int x = 10;
                }
                using (profiler.Sample("Concat + Intern"))
                {
                    for (int i = 0; i < 4; i++)
                        dict[zstring.Concat("Item", i).Intern()] = i;
                    outsideString1 = zstring.Concat("I'm ", "long ", "gone ", "by ", "the ", "end ", "of ", "this ", "gstring block");
                    outsideString = zstring.Concat("I'll ", "be ", "still ", "around ", "here").Intern();
                    int x = 10;
                }

                using (profiler.Sample("ToUpper + ToLower"))
                {
                    zstring s1 = "Hello";
                    zstring s2 = s1.ToUpper();
                    zstring s3 = s2 + s1.ToLower();
                    int x = 10;
                }
                //using (profiler.Sample("Intern"))
                //{
                //    zstring s1 = "hello world";
                //    zstring s2 = s1 + UnityEngine.Random.Range(0, 10000);
                //    string a = s2.Intern();
                //}
                if (!bigStringTest)
                {
                    return;
                }
                using (profiler.Sample("BigStringEval"))
                {
                    zstring s1 = bigString;
                    zstring s2 = s1 + "hello";
                }
            }
        }
    }
   

}
public class ProfilerBlock : IDisposable
{
    public static readonly ProfilerBlock Instance = new ProfilerBlock();

    public IDisposable Sample(string sample)
    {
        UnityEngine.Profiling.Profiler.BeginSample(sample);
        return this;
    }

    public void Dispose()
    {
        UnityEngine.Profiling.Profiler.EndSample();
    }
}