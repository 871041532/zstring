using UnityEngine;
using System;
using System.Collections.Generic;
using GameFramework;
using System.IO;
using System.Text;

public class chapter044 : MonoBehaviour
{
    Dictionary<string, int> dict = new Dictionary<string, int>();

    ProfilerBlock profiler = ProfilerBlock.Instance;

    public string outsideString;
    public string outsideString1;
    string bigString = new string('x', 10000);

    public bool bigStringTest = false;

    void Update()
    {
        for (int n = 0; n < 1000; n++)
        {
            gstringTest();
            nstringTest();//gstring升级版nstring
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