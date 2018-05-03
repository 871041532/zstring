using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
    public class gstring
    {
        static Dictionary<int, Stack<gstring>> g_cache;//key特定字符串长度，value是特定长度的字符串栈
        static Stack<gstring_block> g_blocks;//gstring_block缓存栈
        static List<string> g_intern_table;//字符串intern表
        public static gstring_block g_current_block;//gstring所在的block块
        static List<int> g_finds;//字符串replace功能记录子串位置
        static gstring[] g_format_args;//存储格式化字符串值

        const int INITIAL_BLOCK_CAPACITY = 32;
        const int INITIAL_CACHE_CAPACITY = 128;
        const int INITIAL_STACK_CAPACITY = 48;//缓存栈默认容量
        const int INITIAL_INTERN_CAPACITY = 256;//Intern容量
        const int INITIAL_OPEN_CAPACITY = 5;//默认打开层数为5
        const char NEW_ALLOC_CHAR = 'X';//填充char

        [NonSerialized] string _value;//值
        [NonSerialized] bool _disposed;//销毁标记

        //不支持构造
        internal gstring()
        {
            throw new NotSupportedException();
        }
        //带默认长度的构造
        internal gstring(int length)
        {
            _value = new string(NEW_ALLOC_CHAR, length);
        }

        static gstring()
        {
            Initialize(INITIAL_CACHE_CAPACITY,
                       INITIAL_STACK_CAPACITY,
                       INITIAL_BLOCK_CAPACITY,
                       INITIAL_INTERN_CAPACITY,
                       INITIAL_OPEN_CAPACITY);

            g_finds = new List<int>(10);
            g_format_args = new gstring[10];
        }
        //析构
        internal void dispose()
        {
            if (_disposed)
                throw new ObjectDisposedException(this);

            var stack = g_cache[Length];//从字典中取出valuelength长度的栈，将自身push进去
            stack.Push(this);
#if DBG
            if (log != null)
                log("Disposed: " + _value + " Length=" + Length + " Stack=" + stack.Count);
#endif
            memcpy(_value, NEW_ALLOC_CHAR);//内存拷贝至value

            _disposed = true;
        }
        //由string获取相同内容gstring
        internal static gstring get(string value)
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

        //将string加入intern表中
        internal static string __intern(string value)
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

        //获取特定长度gstring
        internal static gstring get(int length)
        {
            if (g_current_block == null)
                throw new InvalidOperationException("GString 操作必须在一个gstring_block块中。");

            if (length <= 0)
                throw new InvalidOperationException("错误参数 length: " + length);

            gstring result;
            Stack<gstring> stack;
            //从缓存中取Stack
            if (!g_cache.TryGetValue(length, out stack))
            {
                stack = new Stack<gstring>(INITIAL_STACK_CAPACITY);
                for (int i = 0; i < INITIAL_STACK_CAPACITY; i++)
                    stack.Push(new gstring(length));
                g_cache[length] = stack;
                result = stack.Pop();
            }
            else
            {
                if (stack.Count == 0)
                {
#if DBG
                    if (Log != null)
                        Log("Stack=0 Allocating new gstring Length=" + length);
#endif
                    result = new gstring(length);

                }
                else
                {
                    result = stack.Pop();
#if DBG
                    if (log != null)
                        log("Popped Length=" + length + " Stack=" + stack.Count);
#endif
                }
            }

            result._disposed = false;

            g_current_block.push(result);//gstring推入块所在栈

            return result;
        }
        //value是10的次方数
        internal static int get_digit_count(int value)
        {
            int cnt;
            for (cnt = 1; (value /= 10) > 0; cnt++) ;
            return cnt;
        }
        //获取char在input中start起往后的下标
        internal static int internal_index_of(string input, char value, int start)
        {
            return internal_index_of(input, value, start, input.Length - start);
        }
        //获取string在input中起始0的下标
        internal static int internal_index_of(string input, string value)
        {
            return internal_index_of(input, value, 0, input.Length);
        }
        //获取string在input中自0起始下标
        internal static int internal_index_of(string input, string value, int start)
        {
            return internal_index_of(input, value, start, input.Length - start);
        }
        //获取格式化字符串
        internal unsafe static gstring internal_format(string input, int num_args)
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
                gstring arg = g_format_args[i];
                new_len += arg.Length;
            }

            gstring result = get(new_len);
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
        internal unsafe static int internal_index_of(string input, char value, int start, int count)
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
        internal unsafe static int internal_index_of(string input, string value, int start, int count)
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
        internal unsafe static gstring internal_remove(string input, int start, int count)
        {
            if (start < 0 || start >= input.Length)
                throw new ArgumentOutOfRangeException("start=" + start + " Length=" + input.Length);

            if (count < 0 || start + count > input.Length)
                throw new ArgumentOutOfRangeException("count=" + count + " start+count=" + (start + count) + " Length=" + input.Length);

            if (count == 0)
                return input;

            gstring result = get(input.Length - count);
            internal_remove(result, input, start, count);
            return result;
        }
        //将src中自start起count长度子串复制入dst
        internal unsafe static void internal_remove(string dst, string src, int start, int count)
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
        internal unsafe static gstring internal_replace(string value, string old_value, string new_value)
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

            gstring result = get(new_len);
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
        internal unsafe static gstring internal_insert(string value, char to_insert, int start, int count)
        {
            // "HelloWorld" (to_insert=x, start=5, count=3) -> "HelloxxxWorld"

            if (start < 0 || start >= value.Length)
                throw new ArgumentOutOfRangeException("start=" + start + " Length=" + value.Length);

            if (count < 0)
                throw new ArgumentOutOfRangeException("count=" + count);

            if (count == 0)
                return get(value);

            int new_len = value.Length + count;
            gstring result = get(new_len);
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
        internal unsafe static gstring internal_insert(string input, string to_insert, int start)
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
            gstring result = get(new_len);
            internal_insert(result, input, to_insert, start);
            return result;
        }
        //字符串拼接
        internal unsafe static gstring internal_concat(string s1, string s2)
        {
            int total_length = s1.Length + s2.Length;
            gstring result = get(total_length);
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
        internal unsafe static void internal_insert(string dst, string src, string to_insert, int start)
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
        internal unsafe static void intcpy(char* dst, int value, int start, int count)
        {
            int end = start + count;
            for (int i = end - 1; i >= start; i--, value /= 10)
                *(dst + i) = (char)(value % 10 + 48);
        }
        //从src，0位置起始拷贝count长度字符串src到dst中
        internal unsafe static void memcpy(char* dst, char* src, int count)
        {
            for (int i = 0; i < count; i++)
                *(dst++) = *(src++);
        }
        //将字符串dst用字符src填充
        internal unsafe static void memcpy(string dst, char src)
        {
            fixed (char* ptr_dst = dst)
            {
                int len = dst.Length;
                for (int i = 0; i < len; i++)
                    ptr_dst[i] = src;
            }
        }
        //将字符拷贝到dst指定index位置
        internal unsafe static void memcpy(string dst, char src, int index)
        {
            fixed (char* ptr = dst)
                ptr[index] = src;
        }
        //将相同长度的src内容拷入dst
        internal unsafe static void memcpy(string dst, string src)
        {
            if (dst.Length != src.Length)
                throw new InvalidOperationException("两个字符串参数长度不一致。");

            memcpy(dst, src, dst.Length, 0);
        }
        //将src指定length内容拷入dst，dst下标src_offset偏移
        internal unsafe static void memcpy(char* dst, char* src, int length, int src_offset)
        {
            for (int i = 0; i < length; i++)
                *(dst + i + src_offset) = *(src + i);
        }

        internal unsafe static void memcpy(string dst, string src, int length, int src_offset)
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

        public class gstring_block : IDisposable
        {
            readonly Stack<gstring> stack;

            internal gstring_block(int capacity)
            {
                stack = new Stack<gstring>(capacity);
            }

            internal void push(gstring str)
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
                gstring.g_blocks.Push(this);//将自身push入缓存栈
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
        public static void Initialize(int cache_capacity, int stack_capacity, int block_capacity, int intern_capacity, int open_capacity)
        {
            g_cache = new Dictionary<int, Stack<gstring>>(cache_capacity);
            g_blocks = new Stack<gstring_block>(block_capacity);
            g_intern_table = new List<string>(intern_capacity);
            for (int c = 1; c < cache_capacity; c++)
            {
                var stack = new Stack<gstring>(stack_capacity);
                for (int j = 0; j < stack_capacity; j++)
                    stack.Push(new gstring(c));
                g_cache[c] = stack;
            }

            for (int i = 0; i < block_capacity; i++)
            {
                var block = new gstring_block(block_capacity * 2);
                g_blocks.Push(block);
            }
        }

        //using语法所用。从gstring_block栈中取出一个block并将其置为当前g_current_block，在代码块{}中新生成的gstring都将push入块内部stack中。当离开块作用域时，调用块的Dispose函数，将内栈中所有gstring填充初始值并放入gstring缓存栈。同时将自身放入block缓存栈中。（此处有个问题：使用Stack缓存block，当block被dispose放入Stack后g_current_block仍然指向此block，无法记录此block之前的block，这样导致gstring.Block()无法嵌套使用）
        public static IDisposable Block()
        {
            if (g_blocks.Count == 0)
                g_current_block = new gstring_block(INITIAL_BLOCK_CAPACITY * 2);
            else
                g_current_block = g_blocks.Pop();

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
            return RuntimeHelpers.GetHashCode(_value);
        }
        //字面值比较
        public override bool Equals(object obj)
        {
            if (obj == null)
                return ReferenceEquals(this, null);

            var gstr = obj as gstring;
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
        public static implicit operator gstring(bool value)
        {
            return get(value ? "True" : "False");
        }
        //int->gstring转换
        public unsafe static implicit operator gstring(int value)
        {
            // e.g. 125
            // first pass: count the number of digits
            // then: get a gstring with length = num digits
            // finally: iterate again, get the char of each digit, memcpy char to result
            bool negative = value < 0;
            value = Math.Abs(value);
            int num_digits = get_digit_count(value);
            gstring result;
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
        public unsafe static implicit operator gstring(float value)
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

            gstring result;
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
        //string->gstring转换
        public static implicit operator gstring(string value)
        {
            return get(value);
        }
        //gstring->string转换
        public static implicit operator string(gstring value)
        {
            return value._value;
        }
        //+重载
        public static gstring operator +(gstring left, gstring right)
        {
            return internal_concat(left, right);
        }
        //==重载
        public static bool operator ==(gstring left, gstring right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            if (ReferenceEquals(right, null))
                return false;
            return left._value == right._value;
        }
        //!=重载
        public static bool operator !=(gstring left, gstring right)
        {
            return !(left._value == right._value);
        }
        //转换为大写
        public unsafe gstring ToUpper()
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
        public unsafe gstring ToLower()
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
        public gstring Remove(int start)
        {
            return Remove(start, Length - start);
        }
        //移除剪切
        public gstring Remove(int start, int count)
        {
            return internal_remove(this._value, start, count);
        }
        //插入start起count长度字符
        public gstring Insert(char value, int start, int count)
        {
            return internal_insert(this._value, value, start, count);
        }
        //插入start起字符串
        public gstring Insert(string value, int start)
        {
            return internal_insert(this._value, value, start);
        }
        //子字符替换
        public unsafe gstring Replace(char old_value, char new_value)
        {
            gstring result = get(Length);
            fixed (char* ptr_this = this._value)
            {
                fixed (char* ptr_result = result._value)
                {
                    for (int i = 0; i < Length; i++)
                    {
                        if (ptr_this[i] == old_value)
                            ptr_result[i] = new_value;
                        else
                            ptr_result[i] = ptr_this[i];
                    }
                }
            }
            return result;
        }
        //子字符串替换
        public gstring Replace(string old_value, string new_value)
        {
            return internal_replace(this._value, old_value, new_value);
        }
        //剪切start位置起后续子串
        public gstring Substring(int start)
        {
            return Substring(start, Length - start);
        }
        //剪切start起count长度的子串
        public unsafe gstring Substring(int start, int count)
        {
            if (start < 0 || start >= Length)
                throw new ArgumentOutOfRangeException("start");

            if (count > Length)
                throw new ArgumentOutOfRangeException("count");

            gstring result = get(count);
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
            Stack<gstring> stack;
            if (!g_cache.TryGetValue(length, out stack))
                return -1;
            return stack.Count;
        }
        //自身+value拼接
        public gstring Concat(gstring value)
        {
            return internal_concat(this, value);
        }
        //静态拼接方法簇
        public static gstring Concat(gstring s0, gstring s1) { return s0 + s1; }

        public static gstring Concat(gstring s0, gstring s1, gstring s2) { return s0 + s1 + s2; }

        public static gstring Concat(gstring s0, gstring s1, gstring s2, gstring s3) { return s0 + s1 + s2 + s3; }

        public static gstring Concat(gstring s0, gstring s1, gstring s2, gstring s3, gstring s4) { return s0 + s1 + s2 + s3 + s4; }

        public static gstring Concat(gstring s0, gstring s1, gstring s2, gstring s3, gstring s4, gstring s5) { return s0 + s1 + s2 + s3 + s4 + s5; }

        public static gstring Concat(gstring s0, gstring s1, gstring s2, gstring s3, gstring s4, gstring s5, gstring s6) { return s0 + s1 + s2 + s3 + s4 + s5 + s6; }

        public static gstring Concat(gstring s0, gstring s1, gstring s2, gstring s3, gstring s4, gstring s5, gstring s6, gstring s7) { return s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7; }

        public static gstring Concat(gstring s0, gstring s1, gstring s2, gstring s3, gstring s4, gstring s5, gstring s6, gstring s7, gstring s8) { return s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8; }

        public static gstring Concat(gstring s0, gstring s1, gstring s2, gstring s3, gstring s4, gstring s5, gstring s6, gstring s7, gstring s8, gstring s9) { return s0 + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9; }
        //静态格式化方法簇
        public static gstring Format(string input, gstring arg0, gstring arg1, gstring arg2, gstring arg3, gstring arg4, gstring arg5, gstring arg6, gstring arg7, gstring arg8, gstring arg9)
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

        public static gstring Format(string input, gstring arg0, gstring arg1, gstring arg2, gstring arg3, gstring arg4, gstring arg5, gstring arg6, gstring arg7, gstring arg8)
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

        public static gstring Format(string input, gstring arg0, gstring arg1, gstring arg2, gstring arg3, gstring arg4, gstring arg5, gstring arg6, gstring arg7)
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

        public static gstring Format(string input, gstring arg0, gstring arg1, gstring arg2, gstring arg3, gstring arg4, gstring arg5, gstring arg6)
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

        public static gstring Format(string input, gstring arg0, gstring arg1, gstring arg2, gstring arg3, gstring arg4, gstring arg5)
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

        public static gstring Format(string input, gstring arg0, gstring arg1, gstring arg2, gstring arg3, gstring arg4)
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

        public static gstring Format(string input, gstring arg0, gstring arg1, gstring arg2, gstring arg3)
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

        public static gstring Format(string input, gstring arg0, gstring arg1, gstring arg2)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");
            if (arg1 == null) throw new ArgumentNullException("arg1");
            if (arg2 == null) throw new ArgumentNullException("arg2");

            g_format_args[0] = arg0;
            g_format_args[1] = arg1;
            g_format_args[2] = arg2;
            return internal_format(input, 3);
        }

        public static gstring Format(string input, gstring arg0, gstring arg1)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");
            if (arg1 == null) throw new ArgumentNullException("arg1");

            g_format_args[0] = arg0;
            g_format_args[1] = arg1;
            return internal_format(input, 2);
        }

        public static gstring Format(string input, gstring arg0)
        {
            if (arg0 == null) throw new ArgumentNullException("arg0");

            g_format_args[0] = arg0;
            return internal_format(input, 1);
        }

        //判空或长度
        public static bool IsNullOrEmpty(gstring str)
        {
            return str == null || str.Length == 0;
        }
        //是否以value结束
        public static bool IsPrefix(gstring str, string value)
        {
            return str.StartsWith(value);
        }
        //是否以value开始
        public static bool isPostfix(gstring str, string postfix)
        {
            return str.EndsWith(postfix);
        }
        #endregion
    }
}
