// GAMMA Easy 0.1.0. Copyright (C) 2026. GPL-3.0-or-later.
// Independent community launcher. Not an official GAMMA or GSC product.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

class ProcessJob : IDisposable
{
    IntPtr handle;
    [StructLayout(LayoutKind.Sequential)] struct Basic { public long PerProcessUserTimeLimit, PerJobUserTimeLimit; public uint LimitFlags; public UIntPtr MinimumWorkingSetSize, MaximumWorkingSetSize; public uint ActiveProcessLimit; public UIntPtr Affinity; public uint PriorityClass, SchedulingClass; }
    [StructLayout(LayoutKind.Sequential)] struct Counters { public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount; }
    [StructLayout(LayoutKind.Sequential)] struct Extended { public Basic BasicLimitInformation; public Counters IoInfo; public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateJobObject(IntPtr attributes, string name);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetInformationJobObject(IntPtr job, int info, IntPtr data, uint size);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    internal ProcessJob()
    {
        handle = CreateJobObject(IntPtr.Zero, null);
        var limits = new Extended(); limits.BasicLimitInformation.LimitFlags = 0x2000;
        int size = Marshal.SizeOf(limits); IntPtr pointer = Marshal.AllocHGlobal(size);
        try { Marshal.StructureToPtr(limits, pointer, false); if (handle == IntPtr.Zero || !SetInformationJobObject(handle, 9, pointer, (uint)size)) throw new Win32Exception(); }
        finally { Marshal.FreeHGlobal(pointer); }
    }
    internal void Attach(Process p) { if (!AssignProcessToJobObject(handle, p.Handle)) throw new Win32Exception(); }
    public void Dispose() { if (handle != IntPtr.Zero) { CloseHandle(handle); handle = IntPtr.Zero; } }
}
