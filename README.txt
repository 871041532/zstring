/*
 介绍：
    综合gstring与CString两者特点，只有一个文件，性能与使用方便性高于两者。
 
 报告地址：
    https://coh5.cn/p/1ace6338.html

 使用方式：
    1.将zstring.cs文件放于plugins目录下即可使用（不在plugins目录IOS打包FULLAOT编译不过）

    2.（最佳性能）当update每帧刷新标签显示，或者大量UI飘字，或者该字符串是短时间使用的则使用如下方式：
        using (zstring.Block())
        {
            uiText1.text=(zstring)"hello world"+" you";
            uiText2.text=zstring.format("{0},{1}","hello","world");
        }
        此方式设置的string值位于浅拷贝缓存中，一定时间可能会改变。

     3.资源路径这种需要常驻的则需要intern一下在作用域外使用

         using (zstring.Block())
        {
            zstring a="Assets/";
            zstring b=a+"prefabs/"+"/solider.prefab";
            monster1.path=b.Intern();

            monster2.path=zstring.format("{0},{1}","hello","world");
        }
        此方式设置的string值位于intern表中，游戏运行期间不会改变。

    4.不可使用zstring作为类的成员变量，不建议在using作用域中写for循环，而是在for循环内using。

    5.首次调用时会初始化类，分配各种空间，建议游戏启动时调用一次using(zstring.block()){}

    6.0GC。时间消耗上，短字符串处理，zstring比gstring时间少20%~30%，比原生慢。大字符串处理，zstring比gstring时间少70%~80%，接近原生string速度。

    7.追求极限性能的话，核心函数可以用C++Dll中的 memcpy内存拷贝函数，性能提升10%~20%，一般没这个必要。

    8.测试打开zstringTest工程，分别在smallstringTest与bigStringTest下查看Profile性能。
    
    9.有事请联系 871041532@outlook.com 或 QQ：871041532
 */