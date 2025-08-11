#region

using System.Collections;

#endregion

namespace ConcreteEngine.Common.Collections;

public class BinarySortedList<T> : IReadOnlyList<T> where T : IComparable<T>
{
    private readonly List<T> _list = [];

    public void Add(T item)
    {
        Helpers.AddSorted(_list, item);
    }

    public int IndexOf(T item)
    {
        return Helpers.IndexOf(_list, item);
    }


    public bool Remove(T item)
    {
        return Helpers.RemoveSorted(_list, item);
    }

    public bool Contains(T item)
    {
        return IndexOf(item) >= 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Clear()
    {
        _list.Clear();
    }


    public int Count => _list.Count;


    public T this[int index] => _list[index];


    private static class Helpers
    {
        public static bool RemoveSorted<T>(List<T> list, T item) where T : IComparable<T>
        {
            int index = list.BinarySearch(item);
            if (index < 0)
            {
                return false;
            }

            list.RemoveAt(index);
            return true;
        }

        public static void AddSorted<T>(List<T> list, T item) where T : IComparable<T>
        {
            if (list.Count == 0)
            {
                list.Add(item);
                return;
            }

            if (list[^1].CompareTo(item) <= 0)
            {
                list.Add(item);
                return;
            }

            if (list[0].CompareTo(item) >= 0)
            {
                list.Insert(0, item);
                return;
            }

            int index = list.BinarySearch(item);
            if (index < 0)
            {
                index = ~index;
            }

            list.Insert(index, item);
        }

        public static int IndexOf<T>(List<T> list, T item) where T : IComparable<T>
        {
            int index = list.BinarySearch(item);
            return index >= 0 ? index : -1;
        }
    }
}