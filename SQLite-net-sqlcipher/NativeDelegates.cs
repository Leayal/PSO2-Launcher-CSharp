using System;
using System.Runtime.InteropServices;
using SQLitePCL;

namespace SQLite
{
    static class NativeDelegates
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void callback_log(IntPtr pUserData, int errorCode, IntPtr pMessage);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void callback_scalar_function(IntPtr context, int nArgs, IntPtr argsptr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void callback_agg_function_step(IntPtr context, int nArgs, IntPtr argsptr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void callback_agg_function_final(IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void callback_destroy(IntPtr p);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int callback_collation(IntPtr puser, int len1, IntPtr pv1, int len2, IntPtr pv2);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void callback_update(IntPtr p, int typ, IntPtr db, IntPtr tbl, long rowid);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int callback_commit(IntPtr puser);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void callback_profile(IntPtr puser, IntPtr statement, long elapsed);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int callback_progress_handler(IntPtr puser);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int callback_authorizer(IntPtr puser, int action_code, IntPtr param0, IntPtr param1, IntPtr dbName, IntPtr inner_most_trigger_or_view);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void callback_trace(IntPtr puser, IntPtr statement);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void callback_rollback(IntPtr puser);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int callback_exec(IntPtr db, int n, IntPtr values, IntPtr names);
    }
}
