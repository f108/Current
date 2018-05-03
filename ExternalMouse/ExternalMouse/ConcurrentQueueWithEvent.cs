using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace ExternalMouse
{
    class ConcurrentQueueWithEvent<T> : ConcurrentQueue<T>
    {
        AutoResetEvent haveNewItem = new AutoResetEvent(false);

        public new void Enqueue(T item)
        {
            base.Enqueue(item);
            haveNewItem.Set();
        }

        public bool WaitOne(int millisecondsTimeout)
        {
            return haveNewItem.WaitOne(millisecondsTimeout);
        }

    }
}
