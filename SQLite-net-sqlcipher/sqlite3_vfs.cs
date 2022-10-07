using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SQLite
{
    struct sqlite3_vfs
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int SQLiteDeleteDelegate(IntPtr pVfs, byte* zName, int syncDir);

        public int iVersion;

        public int szOsFile;

        public int mxPathname;

        public IntPtr pNext;

        public IntPtr zName;

        public IntPtr pAppData;

        public IntPtr xOpen;

        public SQLiteDeleteDelegate xDelete;

        public IntPtr xAccess;

        public IntPtr xFullPathname;

        public IntPtr xDlOpen;

        public IntPtr xDlError;

        public IntPtr xDlSym;

        public IntPtr xDlClose;

        public IntPtr xRandomness;

        public IntPtr xSleep;

        public IntPtr xCurrentTime;

        public IntPtr xGetLastError;
    }
}
