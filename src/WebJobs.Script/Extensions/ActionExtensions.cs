﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Script
{
    public static class ActionExtensions
    {
        public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
        {
            var last = 0;

            return (arg) =>
            {
                var current = Interlocked.Increment(ref last);

                Task.Delay(milliseconds).ContinueWith(t =>
                {
                    if (current == last)
                    {
                        // Only proceeed with the operation if there have been no
                        // more events withing the specified time window (i.e. there
                        // is a quiet period)
                        func(arg);
                    }
                    t.Dispose();
                });
            };
        }
    }
}