using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//#define DBG

//GString（gcfreestring）是一个字符串包装，使用指针操作各种字符的目的是为了能够完成大部分的字符串的常用操作（连接，格式，替换等）无任何内存分配。

//GString是不是意味着要作为成员变量，而是在一个gstring block块声明它们，使用你想要的任何字符串操作，然后处理掉。

//令人高兴的是，你不必手动dispose gstrings，在一个gstring block块内，所有的作业都会注册以保证出block范围时块中的所有gstrings都会disposed

//但是，如果你想保存你计算出来的结果而不dispose它们呢？

//这是“intern”使用的地方——基本上有一个字符串的运行时intern（缓存）表（类似于.NET的字符串intern表）。 string str = result.Intern();

//这基本上说，如果字符串在intern（缓存）表中，返回它，否则为它分配新的内存并将它存储在表中，下次我们请求它时，它就在那里。

//interning的好处是，你可以预先intern你的字符串通过静态方法gstring.intern。

//笔记:
//1 -这个类并没有考虑并发/线程设计，它只用于Unity主线程使用。
//2 -Cultural stuff，我也没有考虑。
//3 -不应该将GString作为类的成员。所有的gstring实例都注定被销毁。只是快速地打开gstring.Block()，在{}内部使用gstrings，如果你想存储结果，那么就使用Intern.

namespace System
{
    public class nstring
    {
        static Queue<nstring>[] g_cache;//idx特定字符串长度,深拷贝核心缓存
        static Dictionary<int, Queue<nstring>> g_secCache;//key特定字符串长度value字符串栈，深拷贝次级缓存
        static Stack<nstring> g_shallowCache;//浅拷贝缓存

        static Stack<nstring_block> g_blocks;//gstring_block缓存栈
        static Stack<nstring_block> g_open_blocks;//gstring已经打开的缓存栈      
        static List<string> g_intern_table;//字符串intern表
        public static nstring_block g_current_block;//gstring所在的block块
        static List<int> g_finds;//字符串replace功能记录子串位置
        static nstring[] g_format_args;//存储格式化字符串值

        const int INITIAL_BLOCK_CAPACITY = 32;//gblock块数量  
        const int INITIAL_CACHE_CAPACITY = 128;//cache缓存字典容量  128*4Byte 500多Byte
        const int INITIAL_STACK_CAPACITY = 48;//cache字典每个stack默认nstring容量
        const int INITIAL_INTERN_CAPACITY = 256;//Intern容量
        const int INITIAL_OPEN_CAPACITY = 5;//默认打开层数为5
        const int INITIAL_SHALLOW_CAPACITY = 50;//默认50个浅拷贝用
        const char NEW_ALLOC_CHAR = 'X';//填充char
        private bool isShallow = false;//是否浅拷贝
        [NonSerialized] string _value;//值
        [NonSerialized] bool _disposed;//销毁标记

        //不支持构造
        private nstring()
        {
            throw new NotSupportedException();
        }
        //带默认长度的构造
        private nstring(int length)
        {
            _value = new string(NEW_ALLOC_CHAR, length);
        }
        //浅拷贝专用构造
        private nstring(string value, bool shallow)
        {
            if (!shallow)
            {
                throw new NotSupportedException();
            }
            _value = value;
            isShallow = true;
        }
        static nstring()
        {
            Initialize(INITIAL_CACHE_CAPACITY,
                       INITIAL_STACK_CAPACITY,
                       INITIAL_BLOCK_CAPACITY,
                       INITIAL_INTERN_CAPACITY,
                       INITIAL_OPEN_CAPACITY,
                       INITIAL_SHALLOW_CAPACITY
                       );

            g_finds = new List<int>(10);
            g_format_args = new nstring[10];
        }
        //析构
        private void dispose()
        {
            if (_disposed)
                throw new ObjectDisposedException(this);

            if (isShallow)//深浅拷贝走不同缓存
            {
                g_shallowCache.Push(this);
            }
            else
            {
                Queue<nstring> stack;
                if (g_cache.Length>Length)
                {
                    stack = g_cache[Length];//取出valuelength长度的栈，将自身push进去
                }
                else
                {
                    stack = g_secCache[Length];
                }
                stack.Enqueue(this);
            }
            //memcpy(_value, NEW_ALLOC_CHAR);//内存拷贝至value
            _disposed = true;
        }

        //由string获取相同内容gstring，深拷贝
        private static nstring get(string value)
        {
            if (value == null)
                return null;
#if DBG
            if (log != null)
                log("Getting: " + value);
#endif
            var result = get(value.Length);
            memcpy(dst: result, src: value);//内存拷贝
            return result;
        }
        //由string浅拷贝入gstring
        private static nstring getShallow(string value)
        {
            if (g_current_block == null)
            {
                throw new InvalidOperationException("nstring 操作必须在一个nstring_block块中。");
            }
            nstring result;
            if (g_shallowCache.Count == 0)
            {
                result = new nstring(value, true);
            }
            else
            {
                result = g_shallowCache.Pop();
                result._value = value;
            }
            result._disposed = false;
            g_current_block.push(result);//gstring推入块所在栈
            return result;
        }
        //将string加入intern表中
        private static string __intern(string value)
        {
            int idx = g_intern_table.IndexOf(value);
            if (idx != -1)
                return g_intern_table[idx];
            string interned = new string(NEW_ALLOC_CHAR, value.Length);
            memcpy(interned, value);
            g_intern_table.Add(interned);
#if DBG
            if (log != null)
                log("Interned: " + value);
#endif
            return interned;
        }
        //手动添加方法
        private static void getStackInCache(int index, out Queue<nstring> outStack)
        {
            int length = g_cache.Length;
            if (length > index)//从核心缓存中取
            {
                outStack = g_cache[index];
            }
            else//从次级缓存中取
            {
                if (!g_secCache.TryGetValue(index, out outStack))
                {
                    outStack = new Queue<nstring>(INITIAL_STACK_CAPACITY);
                    g_secCache[index] = outStack;
                }
            }
        }
        //获取特定长度gstring
        private static nstring get(int length)
        {
            if (g_current_block == null || length <= 0)
                throw new InvalidOperationException("nstring 操作必须在一个nstring_block块中。");

            nstring result;
            Queue<nstring> stack;
            getStackInCache(length, out stack);
            //从缓存中取Stack
            if (stack.Count == 0)
            {
                result = new nstring(length);
            }
            else
            {
                result = stack.Dequeue();
            }
            result._disposed = false;
            g_current_block.push(result);//gstring推入块所在栈
            return result;
        }
        //value是10的次方数
        private static int get_digit_count(int value)
        {
            int cnt;
            for (cnt = 1; (value /= 10) > 0; cnt++) ;
            return cnt;
        }
        //获取char在input中start起往后的下标
        private static int internal_index_of(string input, char value, int start)
        {
            return internal_index_of(input, value, start, input.Length - start);
        }
        //获取string在input中起始0的下标
        private static int internal_index_of(string input, string value)
        {
            return internal_index_of(input, value, 0, input.Length);
        }
        //获取string在input中自0起始下标
        private static int internal_index_of(string input, string value, int start)
        {
            return internal_index_of(input, value, start, input.Length - start);
        }
        //获取格式化字符串
        private unsafe static nstring internal_format(string input, int num_args)
        {
            // "{0} {1}", "Hello", "World" ->
            // "xxxxxxxxxxx"
            // "Helloxxxxxx"
            // "Hello xxxxx"
            // "Hello World"

            // "Player={0} Id={1}", "Jon", 10 ->
            // "xxxxxxxxxxxxxxxx"
            // "Player=xxxxxxxxx"
            // "Player=Jonxxxxxx"
            // "Player=Jon Id=xx"
            // "Player=Jon Id=10"

            if (input == null)
                throw new ArgumentNullException("value");
            //新字符串长度
            int new_len = input.Length - 3 * num_args;

            for (int i = 0; i < num_args; i++)
            {
                nstring arg = g_format_args[i];
                new_len += arg.Length;
            }

            nstring result = get(new_len);
            string res_value = result._value;

            int brace_idx = -3;
            for (int i = 0, j = 0, x = 0; x < num_args; x++)
            {
                string arg = g_format_args[x]._value;
                brace_idx = internal_index_of(input, '{', brace_idx + 3);
                if (brace_idx == -1)
                    throw new InvalidOperationException("没有发现大括号{ for argument " + arg);
                if (brace_idx + 2 >= input.Length || input[brace_idx + 2] != '}')
                    throw new InvalidOperationException("没有发现大括号} for argument " + arg);

                fixed (char* ptr_input = input)
                {
                    fixed (char* ptr_result = res_value)
                    {
                        for (int k = 0; i < new_len;)
                        {
                            if (j < brace_idx)
                                ptr_result[i++] = ptr_input[j++];
                            else
                            {
                                ptr_result[i++] = arg[k++];
                                if (k == arg.Length)
                                {
                                    j += 3;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        //获取char在字符串中start开始的下标
        private unsafe static int internal_index_of(string input, char value, int start, int count)
        {
            if (start < 0 || start >= input.Length)
                throw new ArgumentOutOfRangeException("start");

            if (start + count > input.Length)
                throw new ArgumentOutOfRangeException("count=" + count + " start+count=" + start + count);

            fixed (char* ptr_this = input)
            {
                int end = start + count;
                for (int i = start; i < end; i++)
                    if (ptr_this[i] == value)
                        return i;
                return -1;
            }
        }
        //获取value在input中自start起始下标
        private unsafe static int internal_index_of(string input, string value, int start, int count)
        {
            int input_len = input.Length;

            if (start < 0 || start >= input_len)
                throw new ArgumentOutOfRangeException("start");

            if (count < 0 || start + count > input_len)
                throw new ArgumentOutOfRangeException("count=" + count + " start+count=" + (start + count));

            if (count == 0)
                return -1;

            fixed (char* ptr_input = input)
            {
                fixed (char* ptr_value = value)
                {
                    int found = 0;
                    int end = start + count;
                    for (int i = start; i < end; i++)
                    {
                        for (int j = 0; j < value.Length && i + j < input_len; j++)
                        {
                            if (ptr_input[i + j] == ptr_value[j])
                            {
                                found++;
                                if (found == value.Length)
                                    return i;
                                continue;
                            }
                            if (found > 0)
                                break;
                        }
                    }
                    return -1;
                }
            }
        }
        //移除string中自start起始count长度子串
        private unsafe static nstring internal_remove(string input, int start, int count)
        {
            if (start < 0 || start >= input.Length)
                throw new ArgumentOutOfRangeException("start=" + start + " Length=" + input.Length);

            if (count < 0 || start + count > input.Length)
                throw new ArgumentOutOfRangeException("count=" + count + " start+count=" + (start + count) + " Length=" + input.Length);

            if (count == 0)
                return input;

            nstring result = get(input.Length - count);
            internal_remove(result, input, start, count);
            return result;
        }
        //将src中自start起count长度子串复制入dst
        private unsafe static void internal_remove(string dst, string src, int start, int count)
        {
            fixed (char* src_ptr = src)
            {
                fixed (char* dst_ptr = dst)
                {
                    for (int i = 0, j = 0; i < dst.Length; i++)
                    {
                        if (i >= start && i < start + count) // within removal range
                            continue;
                        dst_ptr[j++] = src_ptr[i];
                    }
                }
            }
        }
        //字符串replace，原字符串，需替换子串，替换的新子串
        private unsafe static nstring internal_replace(string value, string old_value, string new_value)
        {
            // "Hello, World. There World" | World->Jon =
            // "000000000000000000000" (len = orig - 2 * (world-jon) = orig - 4
            // "Hello, 00000000000000"
            // "Hello, Jon00000000000"
            // "Hello, Jon. There 000"
            // "Hello, Jon. There Jon"

            // "Hello, World. There World" | World->Alexander =
            // "000000000000000000000000000000000" (len = orig + 2 * (alexander-world) = orig + 8
            // "Hello, 00000000000000000000000000"
            // "Hello, Alexander00000000000000000"
            // "Hello, Alexander. There 000000000"
            // "Hello, Alexander. There Alexander"

            if (old_value == null)
                throw new ArgumentNullException("old_value");

            if (new_value == null)
                throw new ArgumentNullException("new_value");

            int idx = internal_index_of(value, old_value);
            if (idx == -1)
                return value;

            g_finds.Clear();
            g_finds.Add(idx);

            // 记录所有需要替换的idx点
            while (idx + old_value.Length < value.Length)
            {
                idx = internal_index_of(value, old_value, idx + old_value.Length);
                if (idx == -1)
                    break;
                g_finds.Add(idx);
            }

            // calc the right new total length
            int new_len;
            int dif = old_value.Length - new_value.Length;
            if (dif > 0)
                new_len = value.Length - (g_finds.Count * dif);
            else
                new_len = value.Length + (g_finds.Count * -dif);

            nstring result = get(new_len);
            fixed (char* ptr_this = value)
            {
                fixed (char* ptr_result = result._value)
                {
                    for (int i = 0, x = 0, j = 0; i < new_len;)
                    {
                        if (x == g_finds.Count || g_finds[x] != j)
                        {
                            ptr_result[i++] = ptr_this[j++];
                        }
                        else
                        {
                            for (int n = 0; n < new_value.Length; n++)
                                ptr_result[i + n] = new_value[n];

                            x++;
                            i += new_value.Length;
                            j += old_value.Length;
                        }
                    }
                }
            }
            return result;
        }
        //向字符串value中自start位置插入count长度的to_insertChar
        private unsafe static nstring internal_insert(string value, char to_insert, int start, int count)
        {
            // "HelloWorld" (to_insert=x, start=5, count=3) -> "HelloxxxWorld"

            if (start < 0 || start >= value.Length)
                throw new ArgumentOutOfRangeException("start=" + start + " Length=" + value.Length);

            if (count < 0)
                throw new ArgumentOutOfRangeException("count=" + count);

            if (count == 0)
                return get(value);

            int new_len = value.Length + count;
            nstring result = get(new_len);
            fixed (char* ptr_value = value)
            {
                fixed (char* ptr_result = result._value)
                {
                    for (int i = 0, j = 0; i < new_len; i++)
                    {
                        if (i >= start && i < start + count)
                            ptr_result[i] = to_insert;
                        else
                            ptr_result[i] = ptr_value[j++];
                    }
                }
            }
            return result;
        }
        //向input字符串中插入to_insert串，位置为start
        private unsafe static nstring internal_insert(string input, string to_insert, int start)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            if (to_insert == null)
                throw new ArgumentNullException("to_insert");

            if (start < 0 || start >= input.Length)
                throw new ArgumentOutOfRangeException("start=" + start + " Length=" + input.Length);

            if (to_insert.Length == 0)
                return get(input);

            int new_len = input.Length + to_insert.Length;
            nstring result = get(new_len);
            internal_insert(result, input, to_insert, start);
            return result;
        }
        //字符串拼接
        private unsafe static nstring internal_concat(string s1, string s2)
        {
            int total_length = s1.Length + s2.Length;
            nstring result = get(total_length);
            fixed (char* ptr_result = result._value)
            {
                fixed (char* ptr_s1 = s1)
                {
                    fixed (char* ptr_s2 = s2)
                    {
                        memcpy(dst: ptr_result, src: ptr_s1, length: s1.Length, src_offset: 0);
                        memcpy(dst: ptr_result, src: ptr_s2, length: s2.Length, src_offset: s1.Length);
                    }
                }
            }
            return result;
        }
        //将to_insert串插入src的start位置，内容写入dst
        private unsafe static void internal_insert(string dst, string src, string to_insert, int start)
        {
            fixed (char* ptr_src = src)
            {
                fixed (char* ptr_dst = dst)
                {
                    fixed (char* ptr_to_insert = to_insert)
                    {
                        for (int i = 0, j = 0, k = 0; i < dst.Length; i++)
                        {
                            if (i >= start && i < start + to_insert.Length)
                                ptr_dst[i] = ptr_to_insert[k++];
                            else
                                ptr_dst[i] = ptr_src[j++];
                        }
                    }
                }
            }
        }
        //将长度为count的数字插入dst中，起始位置为start，dst的长度需大于start+count
        private unsafe static void intcpy(char* dst, int value, int start, int count)
        {
            int end = start + count;
            for (int i = end - 1; i >= start; i--, value /= 10)
                *(dst + i) = (char)(value % 10 + 48);
        }
        //从src，0位置起始拷贝count长度字符串src到dst中
        private unsafe static void memcpy(char* dst, char* src, int count)
        {
            for (int i = 0; i < count; i++)
                *(dst++) = *(src++);
        }
        //将字符串dst用字符src填充
        private unsafe static void memcpy(string dst, char src)
        {
            fixed (char* ptr_dst = dst)
            {
                int len = dst.Length;
                for (int i = 0; i < len; i++)
                    ptr_dst[i] = src;
            }
        }
        //将字符拷贝到dst指定index位置
        private unsafe static void memcpy(string dst, char src, int index)
        {
            fixed (char* ptr = dst)
                ptr[index] = src;
        }
        //将相同长度的src内容拷入dst
        private unsafe static void memcpy(string dst, string src)
        {
            if (dst.Length != src.Length)
                throw new InvalidOperationException("两个字符串参数长度不一致。");

            memcpy(dst, src, dst.Length, 0);
        }
        //将src指定length内容拷入dst，dst下标src_offset偏移
        private unsafe static void memcpy(char* dst, char* src, int length, int src_offset)
        {
            for (int i = 0; i < length; i++)
                *(dst + i + src_offset) = *(src + i);
        }

        private unsafe static void memcpy(string dst, string src, int length, int src_offset)
        {
            fixed (char* ptr_dst = dst)
            {
                fixed (char* ptr_src = src)
                {
                    for (int i = 0; i < length; i++)
                        ptr_dst[i + src_offset] = ptr_src[i];
                }
            }
        }

        public class nstring_block : IDisposable
        {
            readonly Stack<nstring> stack;

            internal nstring_block(int capacity)
            {
                stack = new Stack<nstring>(capacity);
            }

            internal void push(nstring str)
            {
                stack.Push(str);
            }

            internal IDisposable begin()//构造函数
            {
#if DBG
                if (log != null)
                    log("Began block");
#endif
                return this;
            }

            void IDisposable.Dispose()//析构函数
            {
#if DBG
                if (log != null)
                    log("Disposing block");
#endif
                while (stack.Count > 0)
                {
                    var str = stack.Pop();
                    str.dispose();//循环调用栈中gstring的Dispose方法
                }
                nstring.g_blocks.Push(this);//将自身push入缓存栈

                //赋值currentBlock
                g_open_blocks.Pop();
                if (g_open_blocks.Count > 0)
                {
                    nstring.g_current_block = g_open_blocks.Peek();
                }
                else
                {
                    nstring.g_current_block = null;
                }
            }
        }

        // Public API
        #region 

        public static Action<string> Log = null;

        public static int DecimalAccuracy = 3; // 小数点后精度位数
        //获取字符串长度
        public int Length
        {
            get { return _value.Length; }
        }
        //类构造：cache_capacity缓存栈字典容量，stack_capacity缓存字符串栈容量，block_capacity缓存栈容量，intern_capacity缓存,open_capacity默认打开层数
        public static void Initialize(int cache_capacity, int stack_capacity, int block_capacity, int intern_capacity, int open_capacity, int shallowCache_capacity)
        {
            g_cache = new Queue<nstring>[cache_capacity];
            g_secCache = new Dictionary<int, Queue<nstring>>(cache_capacity);
            g_blocks = new Stack<nstring_block>(block_capacity);
            g_intern_table = new List<string>(intern_capacity);
            g_open_blocks = new Stack<nstring_block>(open_capacity);
            g_shallowCache = new Stack<nstring>(shallowCache_capacity);
            for (int c = 0; c < cache_capacity; c++)
            {
                var stack = new Queue<nstring>(stack_capacity);
                for (int j = 0; j < stack_capacity; j++)
                    stack.Enqueue(new nstring(c));
                g_cache[c] = stack;
            }

            for (int i = 0; i < block_capacity; i++)
            {
                var block = new nstring_block(block_capacity * 2);
                g_blocks.Push(block);
            }
            for (int i = 0; i < shallowCache_capacity; i++)
            {
                g_shallowCache.Push(new nstring(null, true));
            }
        }

        //using语法所用。从gstring_block栈中取出一个block并将其置为当前g_current_block，在代码块{}中新生成的gstring都将push入块内部stack中。当离开块作用域时，调用块的Dispose函数，将内栈中所有gstring填充初始值并放入gstring缓存栈。同时将自身放入block缓存栈中。（此处有个问题：使用Stack缓存block，当block被dispose放入Stack后g_current_block仍然指向此block，无法记录此block之前的block，这样导致gstring.Block()无法嵌套使用）
        public static IDisposable Block()
        {
            if (g_blocks.Count == 0)
                g_current_block = new nstring_block(INITIAL_BLOCK_CAPACITY * 2);
            else
                g_current_block = g_blocks.Pop();

            g_open_blocks.Push(g_current_block);//新加代码，将此玩意压入open栈
            return g_current_block.begin();
        }
        //将gstring value放入intern缓存表中以供外部使用
        public string Intern()
        {
            return __intern(_value);
        }
        //将string放入gstring intern缓存表中以供外部使用
        public static string Intern(string value)
        {
            return __intern(value);
        }

        public static void Intern(string[] values)
        {
            for (int i = 0; i < values.Length; i++)
                __intern(values[i]);
        }
        //下标取值函数
        public char this[int i]
        {
            get { return _value[i]; }
            set { memcpy(this, value, i); }
        }
        //获取hashcode
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
        //字面值比较
        public override bool Equals(object obj)
        {
            if (obj == null)
                return ReferenceEquals(this, null);

            var gstr = obj as nstring;
            if (gstr != null)
                return gstr._value == this._value;

            var str = obj as string;
            if (str != null)
                return str == this._value;

            return false;
        }
        //转化为string
        public override string ToString()
        {
            return _value;
        }
        //bool->gstring转换
        public static implicit operator nstring(bool value)
        {
            return get(value ? "True" : "False");
        }
        //int->gstring转换
        public unsafe static implicit operator nstring(int value)
        {
            // e.g. 125
            // first pass: count the number of digits
            // then: get a gstring with length = num digits
            // finally: iterate again, get the char of each digit, memcpy char to result
            bool negative = value < 0;
            value = Math.Abs(value);
            int num_digits = get_digit_count(value);
            nstring result;
            if (negative)
            {
                result = get(num_digits + 1);
                fixed (char* ptr = result._value)
                {
                    *ptr = '-';
                    intcpy(ptr, value, 1, num_digits);
                }
            }
            else
            {
                result = get(num_digits);
                fixed (char* ptr = result._value)
                    intcpy(ptr, value, 0, num_digits);
            }
            return result;
        }
        //float->gstring转换
        public unsafe static implicit operator nstring(float value)
        {
            // e.g. 3.148
            bool negative = value < 0;
            if (negative) value = -value;
            int mul = (int)Math.Pow(10, DecimalAccuracy);
            int number = (int)(value * mul); // gets the number as a whole, e.g. 3148
            int left_num = number / mul; // left part of the decimal point, e.g. 3
            int right_num = number % mul; // right part of the decimal pnt, e.g. 148
            int left_digit_count = get_digit_count(left_num); // e.g. 1
            int right_digit_count = get_digit_count(right_num); // e.g. 3
            int total = left_digit_count + right_digit_count + 1; // +1 for '.'

            nstring result;
            if (negative)
            {
                result = get(total + 1); // +1 for '-'
                fixed (char* ptr = result._value)
                {
                    *ptr = '-';
                    intcpy(ptr, left_num, 1, left_digit_count);
                    *(ptr + left_digit_count + 1) = '.';
                    intcpy(ptr, right_num, left_digit_count + 2, right_digit_count);
                }
            }
            else
            {
                result = get(total);
                fixed (char* ptr = result._value)
                {
                    intcpy(ptr, left_num, 0, left_digit_count);
                    *(ptr + left_digit_count) = '.';
                    intcpy(ptr, right_num, left_digit_count + 1, right_digit_count);
                }
            }
            return result;
        }
        //string->gstring深拷贝转换
        public static implicit operator nstring(string value)
        {
            return get(value);
        }
        //string->gstring浅拷贝转换(浅拷贝不缓存)
        public static nstring shallow(string value)
        {
            return getShallow(value);
        }
        //gstring->string转换
        public static implicit operator string(nstring value)
        {
            return value._value;
        }
        //+重载
        public static nstring operator +(nstring left, nstring right)
        {
            return internal_concat(left, right);
        }
        //==重载
        public static bool operator ==(nstring left, nstring right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            if (ReferenceEquals(right, null))
                return false;
            return left._value == right._value;
        }
        //!=重载
        public static bool operator !=(nstring left, nstring right)
        {
            return !(left._value == right._value);
        }
        //转换为大写
        public unsafe nstring ToUpper()
        {
            var result = get(Length);
            fixed (char* ptr_this = this._value)
            {
                fixed (char* ptr_result = result._value)
                {
                    for (int i = 0; i < _value.Length; i++)
                    {
                        var ch = ptr_this[i];
                        if (char.IsLower(ch))
                            ptr_result[i] = char.ToUpper(ch);
                        else
                            ptr_result[i] = ptr_this[i];
                    }
                }
            }
            return result;
        }
        //转换为小写
        public unsafe nstring ToLower()
        {
            var result = get(Length);
            fixed (char* ptr_this = this._value)
            {
                fixed (char* ptr_result = result._value)
                {
                    for (int i = 0; i < _value.Length; i++)
                    {
                        var ch = ptr_this[i];
                        if (char.IsUpper(ch))
                            ptr_result[i] = char.ToLower(ch);
                        else
                            ptr_result[i] = ptr_this[i];
                    }
                }
            }
            return result;
        }
        //移除剪切
        public nstring Remove(int start)
        {
            return Remove(start, Length - start);
        }
        //移除剪切
        public nstring Remove(int start, int count)
        {
            return internal_remove(this._value, start, count);
        }
        //插入start起count长度字符
        public nstring Insert(char value, int start, int count)
        {
            return internal_insert(this._value, value, start, count);
        }
        //插入start起字符串
        public nstring Insert(string value, int start)
        {
            return internal_insert(this._value, value, start);
        }
        //子字符替换
        public unsafe nstring Replace(char old_value, char new_value)
        {
            nstring result = get(Length);
            fixed (char* ptr_this = this._value)
            {
                fixed (char* ptr_result = result._value)
                {
                    for (int i = 0; i < Length; i++)
                    {
                        ptr_result[i] = ptr_this[i] == old_value ? new_value : ptr_this[i];
                    }
                }
            }
            return result;
        }
        //子字符串替换
        public nstring Replace(string old_value, string new_value)
        {
            return internal_replace(this._value, old_value, new_value);
        }
        //剪切start位置起后续子串
        public nstring Substring(int start)
        {
            return Substring(start, Length - start);
        }
        //剪切start起count长度的子串
        public unsafe nstring Substring(int start, int count)
        {
            if (start < 0 || start >= Length)
                throw new ArgumentOutOfRangeException("start");

            if (count > Length)
                throw new ArgumentOutOfRangeException("count");

            nstring result = get(count);
            fixed (char* src = this._value)
            fixed (char* dst = result._value)
                memcpy(dst, src + start, count);

            return result;
        }
        //子串包含判断
        public bool Contains(string value)
        {
            return IndexOf(value) != -1;
        }
        //字符包含判断
        public bool Contains(char value)
        {
            return IndexOf(value) != -1;
        }
        //子串第一次出现位置
        public int LastIndexOf(string value)
        {
            int idx = -1;
            int last_find = -1;
            while (true)
            {
                idx = internal_index_of(this._value, value, idx + value.Length);
                last_find = idx;
                if (idx == -1 || idx + value.Length >= this._value.Length)
                    break;
            }
            return last_find;
        }
        //字符第一次出现位置
        public int LastIndexOf(char value)
        {
            int idx = -1;
            int last_find = -1;
            while (true)
            {
                idx = internal_index_of(this._value, value, idx + 1);
                last_find = idx;
                if (idx == -1 || idx + 1 >= this._value.Length)
                    break;
            }
            return last_find;
        }
        //字符第一次出现位置
        public int IndexOf(char value)
        {
            return IndexOf(value, 0, Length);
        }
        //字符自start起第一次出现位置
        public int IndexOf(char value, int start)
        {
            return internal_index_of(this._value, value, start);
        }
        //字符自start起count长度内，
        public int IndexOf(char value, int start, int count)
        {
            return internal_index_of(this._value, value, start, count);
        }
        //子串第一次出现位置
        public int IndexOf(string value)
        {
            return IndexOf(value, 0, Length);
        }
        //子串自start位置起，第一次出现位置
        public int IndexOf(string value, int start)
        {
            return IndexOf(value, start, Length - start);
        }
        //子串自start位置起，count长度内第一次出现位置
        public int IndexOf(string value, int start, int count)
        {
            return internal_index_of(this._value, value, start, count);
        }
        //是否以某字符串结束
        public unsafe bool EndsWith(string postfix)
        {
            if (postfix == null)
                throw new ArgumentNullException("postfix");

            if (this.Length < postfix.Length)
                return false;

            fixed (char* ptr_this = this._value)
            {
                fixed (char* ptr_postfix = postfix)
                {
                    for (int i = this._value.Length - 1, j = postfix.Length - 1; j >= 0; i--, j--)
                        if (ptr_this[i] != ptr_postfix[j])
                            return false;
                }
            }

            return true;
        }
        //是否以某字符串开始
        public unsafe bool StartsWith(string prefix)
        {
            if (prefix == null)
                throw new ArgumentNullException("prefix");

            if (this.Length < prefix.Length)
                return false;

            fixed (char* ptr_this = this._value)
            {
                fixed (char* ptr_prefix = prefix)
                {
                    for (int i = 0; i < prefix.Length; i++)
                        if (ptr_this[i] != ptr_prefix[i])
                            return false;
                }
            }

            return true;
        }
        //获取某长度字符串缓存数量
        public static int GetCacheCount(int length)
        {
            Queue<nstring> stack;
            getStackInCache(length, out stack);
            return stack.Count;
        }
        //自身+value拼接
        public nstring Concat(nstring value)
        {
            return internal_concat(this, value);
        }
        //静态拼接方法簇
        public static nstring Concat(nstring s0, nstring s1) { return s0 + s1; }

        public static nstring Concat(nstring s0, nstring s1, nstring s2) { return s0 + s1 + s2; }

        public static nstring Concat(nstring s0, nstring s1, nstring s2, nstring s3) { return s0 + s1 + s2 + s3; }

        public static nstring Concat(nstring s0, nstring s1, nstring s2, nstring s3, nstring s4) { return s0 + s1 + s2 + s3 + s4; }

        public static nstring Concat(nstring s0, nstring s1, nstring s2, nstring s3, nstring s4, nstring s5) { return s0 + s1 + s2 + s3 + s4 + s5; }

        public static nstring Concat(nstring s0, nstring s1, nstring s2, nstring s3, nstring s4, nstring s5, nstring s6) { return s0 + s1 + s2 + s3 + s4 + s5 + s6; }

        public static nstring Concat(nstring s0, nstring s1, nstring s2, nstring s3, nstring s4, nstring s5, nstring s6, nstring s7) { return s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7; }

        public static nstring Concat(nstring s0, nstring s1, nstring s2, nstring s3, nstring s4, nstring s5, nstring s6, nstring s7, nstring s8) { return s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8; }

        public static nstring Concat(nstring s0, nstring s1, nstring s2, nstring s3, nstring s4, nstring s5, nstring s6, nstring s7, nstring s8, nstring s9) { return s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9; }
        //静态格式化方法簇
        public static nstring Format(string input, nstring arg0, nstring arg1, nstring arg2, nstring arg3, nstring arg4, nstring arg5, nstring arg6, nstring arg7, nstring arg8, nstring arg9)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");
            if (arg1 == null) throw new ArgumentNullException("arg1");
            if (arg2 == null) throw new ArgumentNullException("arg2");
            if (arg3 == null) throw new ArgumentNullException("arg3");
            if (arg4 == null) throw new ArgumentNullException("arg4");
            if (arg5 == null) throw new ArgumentNullException("arg5");
            if (arg6 == null) throw new ArgumentNullException("arg6");
            if (arg7 == null) throw new ArgumentNullException("arg7");
            if (arg8 == null) throw new ArgumentNullException("arg8");
            if (arg9 == null) throw new ArgumentNullException("arg9");

            g_format_args[0] = arg0;
            g_format_args[1] = arg1;
            g_format_args[2] = arg2;
            g_format_args[3] = arg3;
            g_format_args[4] = arg4;
            g_format_args[5] = arg5;
            g_format_args[6] = arg6;
            g_format_args[7] = arg7;
            g_format_args[8] = arg8;
            g_format_args[9] = arg9;
            return internal_format(input, 10);
        }

        public static nstring Format(string input, nstring arg0, nstring arg1, nstring arg2, nstring arg3, nstring arg4, nstring arg5, nstring arg6, nstring arg7, nstring arg8)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");
            if (arg1 == null) throw new ArgumentNullException("arg1");
            if (arg2 == null) throw new ArgumentNullException("arg2");
            if (arg3 == null) throw new ArgumentNullException("arg3");
            if (arg4 == null) throw new ArgumentNullException("arg4");
            if (arg5 == null) throw new ArgumentNullException("arg5");
            if (arg6 == null) throw new ArgumentNullException("arg6");
            if (arg7 == null) throw new ArgumentNullException("arg7");
            if (arg8 == null) throw new ArgumentNullException("arg8");

            g_format_args[0] = arg0;
            g_format_args[1] = arg1;
            g_format_args[2] = arg2;
            g_format_args[3] = arg3;
            g_format_args[4] = arg4;
            g_format_args[5] = arg5;
            g_format_args[6] = arg6;
            g_format_args[7] = arg7;
            g_format_args[8] = arg8;
            return internal_format(input, 9);
        }

        public static nstring Format(string input, nstring arg0, nstring arg1, nstring arg2, nstring arg3, nstring arg4, nstring arg5, nstring arg6, nstring arg7)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");
            if (arg1 == null) throw new ArgumentNullException("arg1");
            if (arg2 == null) throw new ArgumentNullException("arg2");
            if (arg3 == null) throw new ArgumentNullException("arg3");
            if (arg4 == null) throw new ArgumentNullException("arg4");
            if (arg5 == null) throw new ArgumentNullException("arg5");
            if (arg6 == null) throw new ArgumentNullException("arg6");
            if (arg7 == null) throw new ArgumentNullException("arg7");

            g_format_args[0] = arg0;
            g_format_args[1] = arg1;
            g_format_args[2] = arg2;
            g_format_args[3] = arg3;
            g_format_args[4] = arg4;
            g_format_args[5] = arg5;
            g_format_args[6] = arg6;
            g_format_args[7] = arg7;
            return internal_format(input, 8);
        }

        public static nstring Format(string input, nstring arg0, nstring arg1, nstring arg2, nstring arg3, nstring arg4, nstring arg5, nstring arg6)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");
            if (arg1 == null) throw new ArgumentNullException("arg1");
            if (arg2 == null) throw new ArgumentNullException("arg2");
            if (arg3 == null) throw new ArgumentNullException("arg3");
            if (arg4 == null) throw new ArgumentNullException("arg4");
            if (arg5 == null) throw new ArgumentNullException("arg5");
            if (arg6 == null) throw new ArgumentNullException("arg6");

            g_format_args[0] = arg0;
            g_format_args[1] = arg1;
            g_format_args[2] = arg2;
            g_format_args[3] = arg3;
            g_format_args[4] = arg4;
            g_format_args[5] = arg5;
            g_format_args[6] = arg6;
            return internal_format(input, 7);
        }

        public static nstring Format(string input, nstring arg0, nstring arg1, nstring arg2, nstring arg3, nstring arg4, nstring arg5)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");
            if (arg1 == null) throw new ArgumentNullException("arg1");
            if (arg2 == null) throw new ArgumentNullException("arg2");
            if (arg3 == null) throw new ArgumentNullException("arg3");
            if (arg4 == null) throw new ArgumentNullException("arg4");
            if (arg5 == null) throw new ArgumentNullException("arg5");

            g_format_args[0] = arg0;
            g_format_args[1] = arg1;
            g_format_args[2] = arg2;
            g_format_args[3] = arg3;
            g_format_args[4] = arg4;
            g_format_args[5] = arg5;
            return internal_format(input, 6);
        }

        public static nstring Format(string input, nstring arg0, nstring arg1, nstring arg2, nstring arg3, nstring arg4)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");
            if (arg1 == null) throw new ArgumentNullException("arg1");
            if (arg2 == null) throw new ArgumentNullException("arg2");
            if (arg3 == null) throw new ArgumentNullException("arg3");
            if (arg4 == null) throw new ArgumentNullException("arg4");

            g_format_args[0] = arg0;
            g_format_args[1] = arg1;
            g_format_args[2] = arg2;
            g_format_args[3] = arg3;
            g_format_args[4] = arg4;
            return internal_format(input, 5);
        }

        public static nstring Format(string input, nstring arg0, nstring arg1, nstring arg2, nstring arg3)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");
            if (arg1 == null) throw new ArgumentNullException("arg1");
            if (arg2 == null) throw new ArgumentNullException("arg2");
            if (arg3 == null) throw new ArgumentNullException("arg3");

            g_format_args[0] = arg0;
            g_format_args[1] = arg1;
            g_format_args[2] = arg2;
            g_format_args[3] = arg3;
            return internal_format(input, 4);
        }

        public static nstring Format(string input, nstring arg0, nstring arg1, nstring arg2)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");
            if (arg1 == null) throw new ArgumentNullException("arg1");
            if (arg2 == null) throw new ArgumentNullException("arg2");

            g_format_args[0] = arg0;
            g_format_args[1] = arg1;
            g_format_args[2] = arg2;
            return internal_format(input, 3);
        }

        public static nstring Format(string input, nstring arg0, nstring arg1)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");
            if (arg1 == null) throw new ArgumentNullException("arg1");

            g_format_args[0] = arg0;
            g_format_args[1] = arg1;
            return internal_format(input, 2);
        }

        public static nstring Format(string input, nstring arg0)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");

            g_format_args[0] = arg0;
            return internal_format(input, 1);
        }

        //判空或长度
        public static bool IsNullOrEmpty(nstring str)
        {
            return str == null || str.Length == 0;
        }
        //是否以value结束
        public static bool IsPrefix(nstring str, string value)
        {
            return str.StartsWith(value);
        }
        //是否以value开始
        public static bool isPostfix(nstring str, string postfix)
        {
            return str.EndsWith(postfix);
        }
        #endregion
    }
}
