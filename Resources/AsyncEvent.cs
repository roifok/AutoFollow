using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoFollow.Resources
{
    /// <summary>
    /// Written by http://stackoverflow.com/users/1087692/tzachs
    /// </summary>
    public class AsyncEvent<TSender,TEventArgs> where TEventArgs : EventArgs
    {
        private readonly List<Func<TSender, TEventArgs, Task>> invocationList;
        private readonly object locker;

        private AsyncEvent()
        {
            invocationList = new List<Func<TSender, TEventArgs, Task>>();
            locker = new object();
        }

        public static AsyncEvent<TSender, TEventArgs> operator +(
            AsyncEvent<TSender, TEventArgs> e, Func<TSender, TEventArgs, Task> callback)
        {
            if (callback == null) throw new NullReferenceException("callback is null");

            //Note: Thread safety issue- if two threads register to the same event (on the first time, i.e when it is null)
            //they could get a different instance, so whoever was first will be overridden.
            //A solution for that would be to switch to a public constructor and use it, but then we'll 'lose' the similar syntax to c# events             
            if (e == null) e = new AsyncEvent<TSender, TEventArgs>();

            lock (e.locker)
            {
                e.invocationList.Add(callback);
            }
            return e;
        }

        public static AsyncEvent<TSender, TEventArgs> operator -(
            AsyncEvent<TSender, TEventArgs> e, Func<TSender, TEventArgs, Task> callback)
        {
            if (callback == null) throw new NullReferenceException("callback is null");
            if (e == null) return null;

            lock (e.locker)
            {
                e.invocationList.Remove(callback);
            }
            return e;
        }

        public async Task InvokeAsync(TSender sender, TEventArgs eventArgs)
        {
            List<Func<TSender, TEventArgs, Task>> tmpInvocationList;
            lock (locker)
            {
                tmpInvocationList = new List<Func<TSender, TEventArgs, Task>>(invocationList);
            }

            foreach (var callback in tmpInvocationList)
            {
                //Assuming we want a serial invocation, for a parallel invocation we can use Task.WhenAll instead
                await callback(sender, eventArgs);
            }
        }
    }
}

