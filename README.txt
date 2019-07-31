/*
 介绍：
    C# 0GC字符串补充方案。结合gstring与CString两者特点（向这两个方案的作者致敬），只有一个文件，性能与使用方便性高于两者。
 
 报告地址：
    https://coh5.cn/p/1ace6338.html

 使用方式：
    1.Unity引擎将zstring.cs文件放于plugins目录下即可使用（不在plugins目录，则IOS打包或IL2CPP打包等FULLAOT方式编译不过），或者直接把结构体定义放入zstring类中；其余C#程序将zstring.cs直接放入工程使用即可。 

    2.（最佳性能）当update每帧刷新标签显示，或者大量UI飘字，或者该字符串是短时间使用的则使用如下方式：
        using (zstring.Block())
        {
            uiText1.text=(zstring)"hello world"+" you";
            uiText2.text=zstring.format("{0},{1}","hello","world");
        }
        此方式设置的string值位于浅拷贝缓存中，一定时间可能会改变,出作用域后正确性不予保证。

     3.资源路径这种需要常驻的则需要intern一下在作用域外使用

         using (zstring.Block())
        {
            zstring a="Assets/";
            zstring b=a+"prefabs/"+"/solider.prefab";
            prefabPath1=b.Intern();

            prefabPath2=zstring.format("{0},{1}","hello","world").Intern();
        }
        此方式设置的string值位于深拷贝缓存中，游戏运行期间不会改变，可以在作用域外使用。

    4.不可使用zstring作为类的成员变量，不建议在using作用域中写for循环，而是在for循环内using。

    5.首次调用时会初始化类，分配各种空间，建议游戏启动时调用一次using(zstring.Block()){}

    6.0GC。时间消耗上，短字符串处理，zstring比gstring时间少20%~30%，比原生慢。大字符串处理，zstring比gstring时间少70%~80%，接近原生string速度。

    7.追求极限性能的话，核心函数可以用C++Dll中的 memcpy内存拷贝函数，性能提升10%~20%，一般没这个必要。

    8.测试打开zstringTest工程，在Test脚本上勾选与不勾选bigStringTest下查看Profile性能。(同时对比了zstring，gstring，CString,还有王国纪元里的string)
    
    9.据热心用户反应，IL2CPP 2017.4 在 Android上有字节对齐问题，换成2018就木有了。所以此时解决办法有三个：1.IL2CPP换成2018以上版本。 2.719行左右的memcpy函数换成循环一次拷贝一个字节。 3.不怕麻烦的话此处调用C语言的内存拷贝函数dll，即C语言<string.h>中的memcpy，这样性能也更高。

    
    10.有事请联系 871041532@outlook.com 或 QQ(微信)：871041532
 */
