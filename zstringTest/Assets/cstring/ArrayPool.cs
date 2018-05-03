using System.Collections.Generic;
public class ArrayPool<T>
{
    public const int MAX_COUNT = 16;
    //数组队列 的 数组
    Queue<T[]>[] pool = new Queue<T[]>[MAX_COUNT];

    public ArrayPool()
    {
        for (int i = 0; i < MAX_COUNT; i++)
        {
            pool[i] = new Queue<T[]>();
        }
    }

    //返回下一个2的幂值  7返回8   255返回256
    public int NextPowerOfTwo(int v)
    {
        v -= 1;
        v |= v >> 16;
        v |= v >> 8;
        v |= v >> 4;
        v |= v >> 2;
        v |= v >> 1;
        return v + 1;
    }

    //分配空间
    public T[] Alloc(int n)
    {        
        int size = NextPowerOfTwo(n);//下一个2的幂值
        int pos = GetSlot(size);//获取此幂值所占位数

        if (pos >= 0 && pos < MAX_COUNT)
        {
            Queue<T[]> queue = pool[pos];
            int count = queue.Count;

            if (count > 0)
            {
                return queue.Dequeue();
            }

            return new T[size];
        }
        
        return new T[n];
    }

    //收集
    public void Collect(T[] buffer)
    {
        if (buffer == null) return;        
        int pos = GetSlot(buffer.Length);

        if (pos >= 0 && pos < MAX_COUNT)
        {
            Queue<T[]> queue = pool[pos];
            queue.Enqueue(buffer);
        }
    }

    //获取int值的位数
    int GetSlot(int value)
    {
        int len = 0;

        while (value > 0)
        {
            ++len;
            value >>= 1;
        }

        return len;
    }
}