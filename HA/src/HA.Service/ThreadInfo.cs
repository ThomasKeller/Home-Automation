using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HA.Service;

public static class ThreadInfo
{
    public static string GetThreadIdString()
    {
        return $"[TID:{Thread.CurrentThread.ManagedThreadId}]";
    }

    public static string GetThreadName()
    {
        return $"[TID:{Thread.CurrentThread.Name}]";
    }

}
