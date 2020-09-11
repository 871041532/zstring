## 介绍：
​    C# 0GC 字符串补充方案。结合 `gstring`与`CString`两者特点（向这两个方案的作者致敬），只有一个文件，性能与使用方便性高于两者。

 ## 报告地址
​    https://coh5.cn/p/1ace6338.html

 ## 使用方式

1. Unity引擎将 `zstring.cs`文件放于`Plugins`目录下即可使用（不在`Plugins`目录，则IOS打包或IL2CPP打包等FULLAOT方式编译不过），或者直接把结构体定义放入zstring类中；其余C#程序将zstring.cs直接放入工程使用即可。 

2. （**最佳性能**）当update <u>每帧刷新</u> 标签显示，或者 <u>大量UI飘字</u> ，或者 <u>该字符串是短时间使用</u> 的则使用如下方式：

   ```c#
   // 此方式设置的string值位于浅拷贝缓存中，一定时间可能会改变,出作用域后正确性不予保证。;
   using (zstring.Block())
   {
       uiText1.text=(zstring)"hello world"+" you";
       uiText2.text=zstring.format("{0},{1}","hello","world");
   }
   ```

3. 资源路径这种 **<u>需要常驻</u>** 的则需要`intern`一下在作用域外使用

   ```c#
   //此方式设置的string值位于深拷贝缓存中，游戏运行期间不会改变，可以在作用域外使用。;
   using (zstring.Block())
   {
       zstring a="Assets/";
       zstring b=a+"prefabs/"+"/solider.prefab";
       prefabPath1=b.Intern();
   
       prefabPath2=zstring.format("{0},{1}","hello","world").Intern();
   }
   ```

4. 不可使用`zstring`作为类的成员变量，不建议在using作用域中写for循环，而是在for循环内using。

   ```c#
   /*;
   using (xx)
   {
       for (int i = 0; i < length; i++)
       {
   
       }
   }
   */
   //推荐写法;
   for (int i = 0; i < length; i++)
   {
       using (xx)
       {
   
       }
   }
   ```

5. 首次调用时会初始化类，分配各种空间，建议游戏启动时调用一次`using(zstring.Block()){}`

6. 0GC。时间消耗上，短字符串处理，`zstring`比`gstring`时间少 ，比原生慢。大字符串处理，`zstring`比`gstring`时间少70%~80%，接近原生`string`速度。
7. 追求极限性能的话，核心函数可以用`C++Dll`中的 `memcpy`内存拷贝函数，性能提升10%~20%，一般没这个必要。
8. 测试打开`zstringTest`工程，在`Test`脚本上勾选与不勾选`bigStringTest`下查看Profile性能。*(同时对比了`zstring`，`gstring`，`CString`,还有王国纪元里的 `string`)*



## 其他已知问题（FAQ）

Q： 据热心用户反应，**IL2CPP 2017.4** 在 Android 上有 字节对齐 问题，换成2018就木有了。

A：  - 解决办法有三个：

1. `IL2CPP`换成 `2018` 以上版本
2. **719** 行左右的`memcpy` 函数换成循环一次拷贝一个字节
3. 不怕麻烦的话此处调用C语言的内存拷贝函数dll，即C语言 `<string.h>`中的 `memcpy`，这样性能也更高。



## 联系作者

- 871041532@outlook.com 
- QQ(微信)：<u>871041532</u>







