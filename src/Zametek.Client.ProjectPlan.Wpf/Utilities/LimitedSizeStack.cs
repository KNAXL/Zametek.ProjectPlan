using System;
using System.Collections.Generic;

namespace Zametek.Client.ProjectPlan.Wpf
{
    public class LimitedSizeStack<T>
        : LinkedList<T>
        where T : class
    {
        private readonly int m_MaxSize;

        public LimitedSizeStack(int maxSize)
        {
            if (maxSize < 1)
            {
                throw new ArgumentException(@"Value cannot be less than 1", nameof(maxSize));
            }
            m_MaxSize = maxSize;
        }

        public void Push(T item)
        {
            AddFirst(item);

            if (Count > m_MaxSize)
            {
                RemoveLast();
            }
        }

        public T Pop()
        {
            LinkedListNode<T> item = First;

            if (item != null)
            {
                RemoveFirst();
            }

            return item?.Value;
        }
    }
}
