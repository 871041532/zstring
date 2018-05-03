using System;
using System.Collections.Generic;

public static class StringPool
{
    const int MaxSize = 1024;
    const int MaxQueueSize = 8;
    public static Dictionary<int, Queue<string>> map = new Dictionary<int, Queue<string>>();

    static public void PreAlloc(int size, int count)
    {
        if (size > MaxSize || size <= 0)
        {
            return;
        }

        count = Math.Max(MaxQueueSize, count);
        Queue<string> queue = null;

        if (map.TryGetValue(size, out queue))
        {
            for (int i = queue.Count; i < count; i++)
            {
                queue.Enqueue(new string((char)0xCC, size));
            }
        }
        else
        {
            queue = new Queue<string>();
            map[size] = queue;

            for (int i = 0; i < count; i++)
            {
                queue.Enqueue(new string((char)0xCC, size));
            }
        }
    }

    static public string Alloc(int size)
    {   
        if (size == 0)
        {
            return string.Empty;
        }

        if (size >= MaxSize)
        {
            return new string((char)0xCC, size);
        }
        
        Queue<string> queue = null;

        if (map.TryGetValue(size, out queue))
        {
            if (queue.Count > 0)
            {
                return queue.Dequeue();
            }
        }
        else
        {
            queue = new Queue<string>();
            map[size] = queue;
        }

        return new string((char)0xCC, size);
    }

    static public void Collect(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        int size = str.Length;

        if (size < MaxSize && size > 0)
        {
            Queue<String> queue = null;

            if (!map.TryGetValue(str.Length, out queue))            
            {
                queue = new Queue<string>();
                map[size] = queue;
            }

            if (queue.Count <= MaxQueueSize)
            {
                queue.Enqueue(str);
            }
        }
    }
}
