// This file is taken from CLRProfiler:
// http://clrprofiler.codeplex.com/
// https://www.microsoft.com/en-us/download/details.aspx?id=16273
// Original source licensed under "Microsoft Limited Public License (MS-LPL)"

// In accord with terms of license, the license is reproduced in its entirity below:
//
// Microsoft Limited Public License (MS-LPL)
//
// This license governs use of the accompanying software.If you use the software, you accept this license.
// If you do not accept the license, do not use the software.
//
// 1. Definitions
// The terms “reproduce,” “reproduction,” “derivative works,” and “distribution” have the same meaning here
// as under U.S.copyright law.
// A “contribution” is the original software, or any additions or changes to the software.
// A “contributor” is any person that distributes its contribution under this license.
// “Licensed patents” are a contributor’s patent claims that read directly on its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations
// in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to
// reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution
// or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in
// section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed
// patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution
// in the software or derivative works of the contribution in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors’ name, logo, or
// trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the
// software, your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and
// attribution notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this
// license by including a complete copy of this license with your distribution. If you distribute any portion
// of the software in compiled or object code form, you may only do so under a license that complies with this
// license.
// (E) The software is licensed “as-is.” You bear the risk of using it. The contributors give no express
// warranties, guarantees or conditions.You may have additional consumer rights under your local laws which
// this license cannot change.To the extent permitted under your local laws, the contributors exclude the
// implied warranties of merchantability, fitness for a particular purpose and non-infringement.
// (F) Platform Limitation- The licenses granted in sections 2(A) & 2(B) extend only to the software or
// derivative works that you create that run on a Microsoft Windows operating system product.

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class CLRProfilerControl
{
    [DllImport("ProfilerOBJ.dll", CharSet = CharSet.Unicode)]
    private static extern void LogComment(string comment);

    [DllImport("ProfilerOBJ.dll")]
    private static extern bool GetAllocationLoggingActive();

    [DllImport("ProfilerOBJ.dll")]
    private static extern void SetAllocationLoggingActive(bool active);

    [DllImport("ProfilerOBJ.dll")]
    private static extern bool GetCallLoggingActive();

    [DllImport("ProfilerOBJ.dll")]
    private static extern void SetCallLoggingActive(bool active);

    [DllImport("ProfilerOBJ.dll")]
    private static extern bool DumpHeap(uint timeOut);

    private static bool processIsUnderProfiler;

    public static void LogWriteLine(string comment)
    {
        if (processIsUnderProfiler)
        {
            LogComment(comment);
            if (comment == killProcessMarker)
                Process.GetCurrentProcess().Kill();
        }
    }

    public static void LogWriteLine(string format, params object[] args)
    {
        if (processIsUnderProfiler)
        {
            LogComment(string.Format(format, args));
        }
    }

    public static bool AllocationLoggingActive
    {
        get
        {
            if (processIsUnderProfiler)
                return GetAllocationLoggingActive();
            else
                return false;
        }
        set
        {
            if (processIsUnderProfiler)
                SetAllocationLoggingActive(value);
        }
    }

    public static bool CallLoggingActive
    {
        get
        {
            if (processIsUnderProfiler)
                return GetCallLoggingActive();
            else
                return false;
        }
        set
        {
            if (processIsUnderProfiler)
                SetCallLoggingActive(value);
        }
    }

    public static void DumpHeap()
    {
        if (processIsUnderProfiler)
        {
            if (!DumpHeap(60 * 1000))
                throw new Exception("Failure to dump heap");
        }
    }

    public static bool ProcessIsUnderProfiler
    {
        get { return processIsUnderProfiler; }
    }

    static string killProcessMarker;

    static CLRProfilerControl()
    {
        try
        {
            // if AllocationLoggingActive does something, this implies profilerOBJ.dll is attached
            // and initialized properly
            bool active = GetAllocationLoggingActive();
            SetAllocationLoggingActive(!active);
            processIsUnderProfiler = active != GetAllocationLoggingActive();
            SetAllocationLoggingActive(active);
            killProcessMarker = Environment.GetEnvironmentVariable("OMV_KILLPROCESS_MARKER");
        }
        catch (DllNotFoundException)
        {
        }
    }
}
