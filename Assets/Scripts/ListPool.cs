using System.Collections.Generic;

/// <summary>
/// 堆栈缓存池
/// </summary>
/// <typeparam name="T"></typeparam>
public class ListPool<T>
{
    static Stack<List<T>> stack = new Stack<List<T>>();

    /// <summary>
    /// 移除并返回顶部对象
    /// </summary>
    /// <returns></returns>
    public static List<T> Get()
    {
        if(stack.Count>0)
        {
            return stack.Pop();
        }

        return new List<T>();
    }

    /// <summary>
    /// 在堆栈顶部添加一个对象
    /// </summary>
    /// <param name="list"></param>
    public static void Add(List<T> list)
    {
        list.Clear();
        stack.Push(list);
    }
}
