using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using SQLitePCL;

namespace SQLite
{
    /// <summary></summary>
    [Preserve(AllMembers = true)]
    public sealed class SQLite3CustomProvider : ISQLite3Provider
    {
        private readonly NativeMethodGroup NativeMethods;

        private const CallingConvention CALLING_CONVENTION = CallingConvention.Cdecl;

        private static readonly bool IsArm64cc = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 && (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS")));

        private readonly NativeDelegates.callback_commit commit_hook_bridge = commit_hook_bridge_impl;

        private readonly NativeDelegates.callback_scalar_function scalar_function_hook_bridge;

        private static IDisposable? disp_log_hook_handle;

        private readonly NativeDelegates.callback_log log_hook_bridge = log_hook_bridge_impl;

        private readonly NativeDelegates.callback_agg_function_step agg_function_step_hook_bridge;

        private readonly NativeDelegates.callback_agg_function_final agg_function_final_hook_bridge;

        private readonly NativeDelegates.callback_collation collation_hook_bridge = collation_hook_bridge_impl;

        private readonly NativeDelegates.callback_update update_hook_bridge = update_hook_bridge_impl;

        private readonly NativeDelegates.callback_rollback rollback_hook_bridge = rollback_hook_bridge_impl;

        private readonly NativeDelegates.callback_trace trace_hook_bridge = trace_hook_bridge_impl;

        private readonly NativeDelegates.callback_profile profile_hook_bridge = profile_hook_bridge_impl;

        private readonly NativeDelegates.callback_progress_handler progress_handler_hook_bridge = progress_handler_hook_bridge_impl;

        private readonly NativeDelegates.callback_authorizer authorizer_hook_bridge = authorizer_hook_bridge_impl;

        string ISQLite3Provider.GetNativeLibraryName() => (this.NativeMethods.FileName ?? string.Empty);

        /// <summary>Constructor</summary>
        public SQLite3CustomProvider(string librarypath)
        {
            this.NativeMethods = new NativeMethodGroup(librarypath);
            this.agg_function_step_hook_bridge = this.agg_function_step_hook_bridge_impl;
            this.scalar_function_hook_bridge = this.scalar_function_hook_bridge_impl;
            this.agg_function_final_hook_bridge = this.agg_function_final_hook_bridge_impl;
        }

        private bool my_streq(IntPtr p, IntPtr q, int len)
        {
            return NativeMethods.sqlite3_strnicmp(p, q, len) == 0;
        }

        private hook_handles get_hooks(sqlite3 db)
        {
            return db.GetOrCreateExtra(() => new hook_handles(my_streq));
        }

        unsafe int ISQLite3Provider.sqlite3_win32_set_directory(int typ, utf8z path)
        {
            fixed (byte* directoryPath = path)
            {
                return NativeMethods.sqlite3_win32_set_directory8((uint)typ, directoryPath);
            }
        }

        unsafe int ISQLite3Provider.sqlite3_open(utf8z filename, out IntPtr db)
        {
            fixed (byte* filename2 = filename)
            {
                return NativeMethods.sqlite3_open(filename2, out db);
            }
        }

        unsafe int ISQLite3Provider.sqlite3_open_v2(utf8z filename, out IntPtr db, int flags, utf8z vfs)
        {
            fixed (byte* filename2 = filename)
            {
                fixed (byte* vfs2 = vfs)
                {
                    return NativeMethods.sqlite3_open_v2(filename2, out db, flags, vfs2);
                }
            }
        }

        unsafe int ISQLite3Provider.sqlite3__vfs__delete(utf8z vfs, utf8z filename, int syncDir)
        {
            fixed (byte* vfs2 = vfs)
            {
                fixed (byte* zName = filename)
                {
                    IntPtr intPtr = NativeMethods.sqlite3_vfs_find(vfs2);
#pragma warning disable CS8605 // Unboxing a possibly null value.
                    return ((sqlite3_vfs)Marshal.PtrToStructure(intPtr, typeof(sqlite3_vfs))).xDelete(intPtr, zName, 1);
#pragma warning restore CS8605 // Unboxing a possibly null value.
                }
            }
        }

        int ISQLite3Provider.sqlite3_close_v2(IntPtr db)
        {
            return NativeMethods.sqlite3_close_v2(db);
        }

        int ISQLite3Provider.sqlite3_close(IntPtr db)
        {
            return NativeMethods.sqlite3_close(db);
        }

        void ISQLite3Provider.sqlite3_free(IntPtr p)
        {
            NativeMethods.sqlite3_free(p);
        }

        int ISQLite3Provider.sqlite3_stricmp(IntPtr p, IntPtr q)
        {
            return NativeMethods.sqlite3_stricmp(p, q);
        }

        int ISQLite3Provider.sqlite3_strnicmp(IntPtr p, IntPtr q, int n)
        {
            return NativeMethods.sqlite3_strnicmp(p, q, n);
        }

        int ISQLite3Provider.sqlite3_enable_shared_cache(int enable)
        {
            return NativeMethods.sqlite3_enable_shared_cache(enable);
        }

        void ISQLite3Provider.sqlite3_interrupt(sqlite3 db)
        {
            NativeMethods.sqlite3_interrupt(db);
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_exec))]
        private static int exec_hook_bridge_impl(IntPtr p, int n, IntPtr values_ptr, IntPtr names_ptr)
        {
            return exec_hook_info.from_ptr(p).call(n, values_ptr, names_ptr);
        }

        unsafe int ISQLite3Provider.sqlite3_exec(sqlite3 db, utf8z sql, delegate_exec func, object user_data, out IntPtr errMsg)
        {
            NativeDelegates.callback_exec? cb;
            exec_hook_info? target;
            if (func != null)
            {
                cb = exec_hook_bridge_impl;
                target = new exec_hook_info(func, user_data);
            }
            else
            {
                cb = null;
                target = null;
            }

            hook_handle hook_handle = new hook_handle(target);
            int result;
            fixed (byte* strSql = sql)
            {
                result = NativeMethods.sqlite3_exec(db, strSql, cb, hook_handle, out errMsg);
            }

            hook_handle.Dispose();
            return result;
        }

        unsafe int ISQLite3Provider.sqlite3_complete(utf8z sql)
        {
            fixed (byte* pSql = sql)
            {
                return NativeMethods.sqlite3_complete(pSql);
            }
        }

        unsafe utf8z ISQLite3Provider.sqlite3_compileoption_get(int n)
        {
            return utf8z.FromPtr(NativeMethods.sqlite3_compileoption_get(n));
        }

        unsafe int ISQLite3Provider.sqlite3_compileoption_used(utf8z s)
        {
            fixed (byte* pSql = s)
            {
                return NativeMethods.sqlite3_compileoption_used(pSql);
            }
        }

        unsafe int ISQLite3Provider.sqlite3_table_column_metadata(sqlite3 db, utf8z dbName, utf8z tblName, utf8z colName, out utf8z dataType, out utf8z collSeq, out int notNull, out int primaryKey, out int autoInc)
        {
            fixed (byte* dbName2 = dbName)
            {
                fixed (byte* tblName2 = tblName)
                {
                    fixed (byte* colName2 = colName)
                    {
                        byte* ptrDataType;
                        byte* ptrCollSeq;
                        int result = NativeMethods.sqlite3_table_column_metadata(db, dbName2, tblName2, colName2, out ptrDataType, out ptrCollSeq, out notNull, out primaryKey, out autoInc);
                        dataType = utf8z.FromPtr(ptrDataType);
                        collSeq = utf8z.FromPtr(ptrCollSeq);
                        return result;
                    }
                }
            }
        }

        unsafe int ISQLite3Provider.sqlite3_key(sqlite3 db, ReadOnlySpan<byte> k)
        {
            fixed (byte* key = k)
            {
                return NativeMethods.sqlite3_key(db, key, k.Length);
            }
        }

        unsafe int ISQLite3Provider.sqlite3_key_v2(sqlite3 db, utf8z name, ReadOnlySpan<byte> k)
        {
            fixed (byte* key = k)
            {
                fixed (byte* dbname = name)
                {
                    return NativeMethods.sqlite3_key_v2(db, dbname, key, k.Length);
                }
            }
        }

        unsafe int ISQLite3Provider.sqlite3_rekey(sqlite3 db, ReadOnlySpan<byte> k)
        {
            fixed (byte* key = k)
            {
                return NativeMethods.sqlite3_rekey(db, key, k.Length);
            }
        }

        unsafe int ISQLite3Provider.sqlite3_rekey_v2(sqlite3 db, utf8z name, ReadOnlySpan<byte> k)
        {
            fixed (byte* key = k)
            {
                fixed (byte* dbname = name)
                {
                    return NativeMethods.sqlite3_rekey_v2(db, dbname, key, k.Length);
                }
            }
        }

        unsafe int ISQLite3Provider.sqlite3_prepare_v2(sqlite3 db, ReadOnlySpan<byte> sql, out IntPtr stm, out ReadOnlySpan<byte> tail)
        {
            fixed (byte* ptr = sql)
            {
                byte* ptrRemain;
                int result = NativeMethods.sqlite3_prepare_v2(db, ptr, sql.Length, out stm, out ptrRemain);
                int num = (int)(ptrRemain - ptr);
                int num2 = sql.Length - num;
                if (num2 > 0)
                {
                    tail = sql.Slice(num, num2);
                    return result;
                }

                tail = ReadOnlySpan<byte>.Empty;
                return result;
            }
        }

        unsafe int ISQLite3Provider.sqlite3_prepare_v2(sqlite3 db, utf8z sql, out IntPtr stm, out utf8z tail)
        {
            fixed (byte* pSql = sql)
            {
                byte* ptrRemain;
                int result = NativeMethods.sqlite3_prepare_v2(db, pSql, -1, out stm, out ptrRemain);
                tail = utf8z.FromPtr(ptrRemain);
                return result;
            }
        }

        unsafe int ISQLite3Provider.sqlite3_prepare_v3(sqlite3 db, ReadOnlySpan<byte> sql, uint flags, out IntPtr stm, out ReadOnlySpan<byte> tail)
        {
            fixed (byte* ptr = sql)
            {
                byte* ptrRemain;
                int result = NativeMethods.sqlite3_prepare_v3(db, ptr, sql.Length, flags, out stm, out ptrRemain);
                int num = (int)(ptrRemain - ptr);
                int num2 = sql.Length - num;
                if (num2 > 0)
                {
                    tail = sql.Slice(num, num2);
                    return result;
                }

                tail = ReadOnlySpan<byte>.Empty;
                return result;
            }
        }

        unsafe int ISQLite3Provider.sqlite3_prepare_v3(sqlite3 db, utf8z sql, uint flags, out IntPtr stm, out utf8z tail)
        {
            fixed (byte* pSql = sql)
            {
                byte* ptrRemain;
                int result = NativeMethods.sqlite3_prepare_v3(db, pSql, -1, flags, out stm, out ptrRemain);
                tail = utf8z.FromPtr(ptrRemain);
                return result;
            }
        }

        int ISQLite3Provider.sqlite3_db_status(sqlite3 db, int op, out int current, out int highest, int resetFlg)
        {
            return NativeMethods.sqlite3_db_status(db, op, out current, out highest, resetFlg);
        }

        unsafe utf8z ISQLite3Provider.sqlite3_sql(sqlite3_stmt stmt)
        {
            return utf8z.FromPtr(NativeMethods.sqlite3_sql(stmt));
        }

        IntPtr ISQLite3Provider.sqlite3_db_handle(IntPtr stmt)
        {
            return NativeMethods.sqlite3_db_handle(stmt);
        }

        unsafe int ISQLite3Provider.sqlite3_blob_open(sqlite3 db, utf8z db_utf8, utf8z table_utf8, utf8z col_utf8, long rowid, int flags, out sqlite3_blob blob)
        {
            fixed (byte* sdb = db_utf8)
            {
                fixed (byte* table = table_utf8)
                {
                    fixed (byte* col = col_utf8)
                    {
                        return NativeMethods.sqlite3_blob_open(db, sdb, table, col, rowid, flags, out blob);
                    }
                }
            }
        }

        int ISQLite3Provider.sqlite3_blob_bytes(sqlite3_blob blob)
        {
            return NativeMethods.sqlite3_blob_bytes(blob);
        }

        int ISQLite3Provider.sqlite3_blob_reopen(sqlite3_blob blob, long rowid)
        {
            return NativeMethods.sqlite3_blob_reopen(blob, rowid);
        }

        unsafe int ISQLite3Provider.sqlite3_blob_read(sqlite3_blob blob, Span<byte> b, int offset)
        {
            fixed (byte* b2 = b)
            {
                return NativeMethods.sqlite3_blob_read(blob, b2, b.Length, offset);
            }
        }

        unsafe int ISQLite3Provider.sqlite3_blob_write(sqlite3_blob blob, ReadOnlySpan<byte> b, int offset)
        {
            fixed (byte* b2 = b)
            {
                return NativeMethods.sqlite3_blob_write(blob, b2, b.Length, offset);
            }
        }

        int ISQLite3Provider.sqlite3_blob_close(IntPtr blob)
        {
            return NativeMethods.sqlite3_blob_close(blob);
        }

        unsafe int ISQLite3Provider.sqlite3_snapshot_get(sqlite3 db, utf8z schema, out IntPtr snap)
        {
            fixed (byte* schema2 = schema)
            {
                return NativeMethods.sqlite3_snapshot_get(db, schema2, out snap);
            }
        }

        int ISQLite3Provider.sqlite3_snapshot_cmp(sqlite3_snapshot p1, sqlite3_snapshot p2)
        {
            return NativeMethods.sqlite3_snapshot_cmp(p1, p2);
        }

        unsafe int ISQLite3Provider.sqlite3_snapshot_open(sqlite3 db, utf8z schema, sqlite3_snapshot snap)
        {
            fixed (byte* schema2 = schema)
            {
                return NativeMethods.sqlite3_snapshot_open(db, schema2, snap);
            }
        }

        unsafe int ISQLite3Provider.sqlite3_snapshot_recover(sqlite3 db, utf8z name)
        {
            fixed (byte* name2 = name)
            {
                return NativeMethods.sqlite3_snapshot_recover(db, name2);
            }
        }

        void ISQLite3Provider.sqlite3_snapshot_free(IntPtr snap)
        {
            NativeMethods.sqlite3_snapshot_free(snap);
        }

        unsafe sqlite3_backup ISQLite3Provider.sqlite3_backup_init(sqlite3 destDb, utf8z destName, sqlite3 sourceDb, utf8z sourceName)
        {
            fixed (byte* zDestName = destName)
            {
                fixed (byte* zSourceName = sourceName)
                {
                    return NativeMethods.sqlite3_backup_init(destDb, zDestName, sourceDb, zSourceName);
                }
            }
        }

        int ISQLite3Provider.sqlite3_backup_step(sqlite3_backup backup, int nPage)
        {
            return NativeMethods.sqlite3_backup_step(backup, nPage);
        }

        int ISQLite3Provider.sqlite3_backup_remaining(sqlite3_backup backup)
        {
            return NativeMethods.sqlite3_backup_remaining(backup);
        }

        int ISQLite3Provider.sqlite3_backup_pagecount(sqlite3_backup backup)
        {
            return NativeMethods.sqlite3_backup_pagecount(backup);
        }

        int ISQLite3Provider.sqlite3_backup_finish(IntPtr backup)
        {
            return NativeMethods.sqlite3_backup_finish(backup);
        }

        IntPtr ISQLite3Provider.sqlite3_next_stmt(sqlite3 db, IntPtr stmt)
        {
            return NativeMethods.sqlite3_next_stmt(db, stmt);
        }

        long ISQLite3Provider.sqlite3_last_insert_rowid(sqlite3 db)
        {
            return NativeMethods.sqlite3_last_insert_rowid(db);
        }

        int ISQLite3Provider.sqlite3_changes(sqlite3 db)
        {
            return NativeMethods.sqlite3_changes(db);
        }

        int ISQLite3Provider.sqlite3_total_changes(sqlite3 db)
        {
            return NativeMethods.sqlite3_total_changes(db);
        }

        int ISQLite3Provider.sqlite3_extended_result_codes(sqlite3 db, int onoff)
        {
            return NativeMethods.sqlite3_extended_result_codes(db, onoff);
        }

        unsafe utf8z ISQLite3Provider.sqlite3_errstr(int rc)
        {
            return utf8z.FromPtr(NativeMethods.sqlite3_errstr(rc));
        }

        int ISQLite3Provider.sqlite3_errcode(sqlite3 db)
        {
            return NativeMethods.sqlite3_errcode(db);
        }

        int ISQLite3Provider.sqlite3_extended_errcode(sqlite3 db)
        {
            return NativeMethods.sqlite3_extended_errcode(db);
        }

        int ISQLite3Provider.sqlite3_busy_timeout(sqlite3 db, int ms)
        {
            return NativeMethods.sqlite3_busy_timeout(db, ms);
        }

        int ISQLite3Provider.sqlite3_get_autocommit(sqlite3 db)
        {
            return NativeMethods.sqlite3_get_autocommit(db);
        }

        unsafe int ISQLite3Provider.sqlite3_db_readonly(sqlite3 db, utf8z dbName)
        {
            fixed (byte* dbName2 = dbName)
            {
                return NativeMethods.sqlite3_db_readonly(db, dbName2);
            }
        }

        unsafe utf8z ISQLite3Provider.sqlite3_db_filename(sqlite3 db, utf8z att)
        {
            fixed (byte* att2 = att)
            {
                return utf8z.FromPtr(NativeMethods.sqlite3_db_filename(db, att2));
            }
        }

        unsafe utf8z ISQLite3Provider.sqlite3_errmsg(sqlite3 db)
        {
            return utf8z.FromPtr(NativeMethods.sqlite3_errmsg(db));
        }

        unsafe utf8z ISQLite3Provider.sqlite3_libversion()
        {
            return utf8z.FromPtr(NativeMethods.sqlite3_libversion());
        }

        int ISQLite3Provider.sqlite3_libversion_number()
        {
            return NativeMethods.sqlite3_libversion_number();
        }

        int ISQLite3Provider.sqlite3_threadsafe()
        {
            return NativeMethods.sqlite3_threadsafe();
        }

        int ISQLite3Provider.sqlite3_config(int op)
        {
            return NativeMethods.sqlite3_config_none(op);
        }

        int ISQLite3Provider.sqlite3_config(int op, int val)
        {
            if (IsArm64cc)
            {
                return NativeMethods.sqlite3_config_int_arm64cc(op, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, val);
            }

            return NativeMethods.sqlite3_config_int(op, val);
        }

        unsafe int ISQLite3Provider.sqlite3_db_config(sqlite3 db, int op, utf8z val)
        {
            fixed (byte* val2 = val)
            {
                if (IsArm64cc)
                {
                    return NativeMethods.sqlite3_db_config_charptr_arm64cc(db, op, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, val2);
                }

                return NativeMethods.sqlite3_db_config_charptr(db, op, val2);
            }
        }

        unsafe int ISQLite3Provider.sqlite3_db_config(sqlite3 db, int op, int val, out int result)
        {
            int num = 0;
            int result2 = ((!IsArm64cc) ? NativeMethods.sqlite3_db_config_int_outint(db, op, val, &num) : NativeMethods.sqlite3_db_config_int_outint_arm64cc(db, op, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, val, &num));
            result = num;
            return result2;
        }

        int ISQLite3Provider.sqlite3_db_config(sqlite3 db, int op, IntPtr ptr, int int0, int int1)
        {
            if (IsArm64cc)
            {
                return NativeMethods.sqlite3_db_config_intptr_int_int_arm64cc(db, op, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ptr, int0, int1);
            }

            return NativeMethods.sqlite3_db_config_intptr_int_int(db, op, ptr, int0, int1);
        }

        int ISQLite3Provider.sqlite3_limit(sqlite3 db, int id, int newVal)
        {
            return NativeMethods.sqlite3_limit(db, id, newVal);
        }

        int ISQLite3Provider.sqlite3_initialize()
        {
            return NativeMethods.sqlite3_initialize();
        }

        int ISQLite3Provider.sqlite3_shutdown()
        {
            return NativeMethods.sqlite3_shutdown();
        }

        int ISQLite3Provider.sqlite3_enable_load_extension(sqlite3 db, int onoff)
        {
            return NativeMethods.sqlite3_enable_load_extension(db, onoff);
        }

        unsafe int ISQLite3Provider.sqlite3_load_extension(sqlite3 db, utf8z zFile, utf8z zProc, out utf8z pzErrMsg)
        {
            pzErrMsg = utf8z.FromPtr(null);
            return 1;
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_commit))]
        private static int commit_hook_bridge_impl(IntPtr p)
        {
            return commit_hook_info.from_ptr(p).call();
        }

        void ISQLite3Provider.sqlite3_commit_hook(sqlite3 db, delegate_commit? func, object v)
        {
            hook_handles hook_handles = get_hooks(db);
            if (hook_handles.commit != null)
            {
                hook_handles.commit.Dispose();
                hook_handles.commit = null;
            }

            NativeDelegates.callback_commit? func2;
            commit_hook_info? target;
            if (func != null)
            {
                func2 = commit_hook_bridge;
                target = new commit_hook_info(func, v);
            }
            else
            {
                func2 = null;
                target = null;
            }

            hook_handle hook_handle = new hook_handle(target);
            NativeMethods.sqlite3_commit_hook(db, func2, hook_handle);
            hook_handles.commit = hook_handle.ForDispose();
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_scalar_function))]
        private void scalar_function_hook_bridge_impl(IntPtr context, int num_args, IntPtr argsptr)
        {
            function_hook_info.from_ptr(NativeMethods.sqlite3_user_data(context)).call_scalar(context, num_args, argsptr);
        }

        int ISQLite3Provider.sqlite3_create_function(sqlite3 db, byte[] name, int nargs, int flags, object v, delegate_function_scalar? func)
        {
            hook_handles hook_handles = get_hooks(db);
            hook_handles.RemoveScalarFunction(name, nargs);
            int nType = 1 | flags;
            NativeDelegates.callback_scalar_function? callback_scalar_function;
            function_hook_info? target;
            if (func != null)
            {
                callback_scalar_function = scalar_function_hook_bridge;
                target = new function_hook_info(func, v);
            }
            else
            {
                callback_scalar_function = null;
                target = null;
            }

            hook_handle hook_handle = new hook_handle(target);
            int num = NativeMethods.sqlite3_create_function_v2(db, name, nargs, nType, hook_handle, callback_scalar_function, null, null, null);
            if (num == 0 && callback_scalar_function != null)
            {
                hook_handles.AddScalarFunction(name, nargs, hook_handle.ForDispose());
            }

            return num;
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_log))]
        private static void log_hook_bridge_impl(IntPtr p, int rc, IntPtr s)
        {
            log_hook_info.from_ptr(p).call(rc, utf8z.FromIntPtr(s));
        }

        int ISQLite3Provider.sqlite3_config_log(delegate_log? func, object v)
        {
            if (disp_log_hook_handle != null)
            {
                disp_log_hook_handle.Dispose();
                disp_log_hook_handle = null;
            }

            NativeDelegates.callback_log? func2;
            log_hook_info? target;
            if (func != null)
            {
                func2 = log_hook_bridge;
                target = new log_hook_info(func, v);
            }
            else
            {
                func2 = null;
                target = null;
            }

            hook_handle pvUser = (hook_handle)(disp_log_hook_handle = new hook_handle(target));
            if (IsArm64cc)
            {
                return NativeMethods.sqlite3_config_log_arm64cc(16, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, func2, pvUser);
            }

            return NativeMethods.sqlite3_config_log(16, func2, pvUser);
        }

        unsafe void ISQLite3Provider.sqlite3_log(int errcode, utf8z s)
        {
            fixed (byte* zFormat = s)
            {
                NativeMethods.sqlite3_log(errcode, zFormat);
            }
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_agg_function_step))]
        private void agg_function_step_hook_bridge_impl(IntPtr context, int num_args, IntPtr argsptr)
        {
            IntPtr agg_context = NativeMethods.sqlite3_aggregate_context(context, 8);
            function_hook_info.from_ptr(NativeMethods.sqlite3_user_data(context)).call_step(context, agg_context, num_args, argsptr);
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_agg_function_final))]
        private void agg_function_final_hook_bridge_impl(IntPtr context)
        {
            IntPtr agg_context = NativeMethods.sqlite3_aggregate_context(context, 8);
            function_hook_info.from_ptr(NativeMethods.sqlite3_user_data(context)).call_final(context, agg_context);
        }

        int ISQLite3Provider.sqlite3_create_function(sqlite3 db, byte[] name, int nargs, int flags, object v, delegate_function_aggregate_step func_step, delegate_function_aggregate_final func_final)
        {
            hook_handles hook_handles = get_hooks(db);
            hook_handles.RemoveAggFunction(name, nargs);
            int nType = 1 | flags;
            NativeDelegates.callback_agg_function_step callback_agg_function_step;
            NativeDelegates.callback_agg_function_final ffinal;
            function_hook_info target;
            if (func_step != null)
            {
                callback_agg_function_step = agg_function_step_hook_bridge;
                ffinal = agg_function_final_hook_bridge;
                target = new function_hook_info(func_step, func_final, v);
            }
            else
            {
                callback_agg_function_step = null;
                ffinal = null;
                target = null;
            }

            hook_handle hook_handle = new hook_handle(target);
            int num = NativeMethods.sqlite3_create_function_v2(db, name, nargs, nType, hook_handle, null, callback_agg_function_step, ffinal, null);
            if (num == 0 && callback_agg_function_step != null)
            {
                hook_handles.AddAggFunction(name, nargs, hook_handle.ForDispose());
            }

            return num;
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_collation))]
        private unsafe static int collation_hook_bridge_impl(IntPtr p, int len1, IntPtr pv1, int len2, IntPtr pv2)
        {
            collation_hook_info collation_hook_info = collation_hook_info.from_ptr(p);
            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(pv1.ToPointer(), len1);
            ReadOnlySpan<byte> s2 = new ReadOnlySpan<byte>(pv2.ToPointer(), len2);
            return collation_hook_info.call(s, s2);
        }

        int ISQLite3Provider.sqlite3_create_collation(sqlite3 db, byte[] name, object v, delegate_collation func)
        {
            hook_handles hook_handles = get_hooks(db);
            hook_handles.RemoveCollation(name);
            NativeDelegates.callback_collation callback_collation;
            collation_hook_info target;
            if (func != null)
            {
                callback_collation = collation_hook_bridge;
                target = new collation_hook_info(func, v);
            }
            else
            {
                callback_collation = null;
                target = null;
            }

            hook_handle hook_handle = new hook_handle(target);
            int num = NativeMethods.sqlite3_create_collation(db, name, 1, hook_handle, callback_collation);
            if (num == 0 && callback_collation != null)
            {
                hook_handles.AddCollation(name, hook_handle.ForDispose());
            }

            return num;
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_update))]
        private static void update_hook_bridge_impl(IntPtr p, int typ, IntPtr db, IntPtr tbl, long rowid)
        {
            update_hook_info.from_ptr(p).call(typ, utf8z.FromIntPtr(db), utf8z.FromIntPtr(tbl), rowid);
        }

        void ISQLite3Provider.sqlite3_update_hook(sqlite3 db, delegate_update func, object v)
        {
            hook_handles hook_handles = get_hooks(db);
            if (hook_handles.update != null)
            {
                hook_handles.update.Dispose();
                hook_handles.update = null;
            }

            NativeDelegates.callback_update func2;
            update_hook_info target;
            if (func != null)
            {
                func2 = update_hook_bridge;
                target = new update_hook_info(func, v);
            }
            else
            {
                func2 = null;
                target = null;
            }

            hook_handle hook_handle = new hook_handle(target);
            hook_handles.update = hook_handle.ForDispose();
            NativeMethods.sqlite3_update_hook(db, func2, hook_handle);
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_rollback))]
        private static void rollback_hook_bridge_impl(IntPtr p)
        {
            rollback_hook_info.from_ptr(p).call();
        }

        void ISQLite3Provider.sqlite3_rollback_hook(sqlite3 db, delegate_rollback func, object v)
        {
            hook_handles hook_handles = get_hooks(db);
            if (hook_handles.rollback != null)
            {
                hook_handles.rollback.Dispose();
                hook_handles.rollback = null;
            }

            NativeDelegates.callback_rollback func2;
            rollback_hook_info target;
            if (func != null)
            {
                func2 = rollback_hook_bridge;
                target = new rollback_hook_info(func, v);
            }
            else
            {
                func2 = null;
                target = null;
            }

            hook_handle hook_handle = new hook_handle(target);
            hook_handles.rollback = hook_handle.ForDispose();
            NativeMethods.sqlite3_rollback_hook(db, func2, hook_handle);
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_trace))]
        private static void trace_hook_bridge_impl(IntPtr p, IntPtr s)
        {
            trace_hook_info.from_ptr(p).call(utf8z.FromIntPtr(s));
        }

        void ISQLite3Provider.sqlite3_trace(sqlite3 db, delegate_trace func, object v)
        {
            hook_handles hook_handles = get_hooks(db);
            if (hook_handles.trace != null)
            {
                hook_handles.trace.Dispose();
                hook_handles.trace = null;
            }

            NativeDelegates.callback_trace func2;
            trace_hook_info target;
            if (func != null)
            {
                func2 = trace_hook_bridge;
                target = new trace_hook_info(func, v);
            }
            else
            {
                func2 = null;
                target = null;
            }

            hook_handle hook_handle = new hook_handle(target);
            hook_handles.trace = hook_handle.ForDispose();
            NativeMethods.sqlite3_trace(db, func2, hook_handle);
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_profile))]
        private static void profile_hook_bridge_impl(IntPtr p, IntPtr s, long elapsed)
            => profile_hook_info.from_ptr(p).call(utf8z.FromIntPtr(s), elapsed);

        void ISQLite3Provider.sqlite3_profile(sqlite3 db, delegate_profile func, object v)
        {
            hook_handles hook_handles = get_hooks(db);
            if (hook_handles.profile != null)
            {
                hook_handles.profile.Dispose();
                hook_handles.profile = null;
            }

            NativeDelegates.callback_profile func2;
            profile_hook_info target;
            if (func != null)
            {
                func2 = profile_hook_bridge;
                target = new profile_hook_info(func, v);
            }
            else
            {
                func2 = null;
                target = null;
            }

            hook_handle hook_handle = new hook_handle(target);
            hook_handles.profile = hook_handle.ForDispose();
            NativeMethods.sqlite3_profile(db, func2, hook_handle);
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_progress_handler))]
        private static int progress_handler_hook_bridge_impl(IntPtr p)
            => progress_hook_info.from_ptr(p).call();

        void ISQLite3Provider.sqlite3_progress_handler(sqlite3 db, int instructions, delegate_progress func, object v)
        {
            hook_handles hook_handles = get_hooks(db);
            if (hook_handles.progress != null)
            {
                hook_handles.progress.Dispose();
                hook_handles.progress = null;
            }

            NativeDelegates.callback_progress_handler func2;
            progress_hook_info target;
            if (func != null)
            {
                func2 = progress_handler_hook_bridge;
                target = new progress_hook_info(func, v);
            }
            else
            {
                func2 = null;
                target = null;
            }

            hook_handle hook_handle = new hook_handle(target);
            hook_handles.progress = hook_handle.ForDispose();
            NativeMethods.sqlite3_progress_handler(db, instructions, func2, hook_handle);
        }

        [MonoPInvokeCallback(typeof(NativeDelegates.callback_authorizer))]
        private static int authorizer_hook_bridge_impl(IntPtr p, int action_code, IntPtr param0, IntPtr param1, IntPtr dbName, IntPtr inner_most_trigger_or_view)
            => authorizer_hook_info.from_ptr(p).call(action_code, utf8z.FromIntPtr(param0), utf8z.FromIntPtr(param1), utf8z.FromIntPtr(dbName), utf8z.FromIntPtr(inner_most_trigger_or_view));

        int ISQLite3Provider.sqlite3_set_authorizer(sqlite3 db, delegate_authorizer func, object v)
        {
            hook_handles hook_handles = get_hooks(db);
            if (hook_handles.authorizer != null)
            {
                hook_handles.authorizer.Dispose();
                hook_handles.authorizer = null;
            }

            NativeDelegates.callback_authorizer cb;
            authorizer_hook_info target;
            if (func != null)
            {
                cb = authorizer_hook_bridge;
                target = new authorizer_hook_info(func, v);
            }
            else
            {
                cb = null;
                target = null;
            }

            hook_handle hook_handle = new hook_handle(target);
            hook_handles.authorizer = hook_handle.ForDispose();
            return NativeMethods.sqlite3_set_authorizer(db, cb, hook_handle);
        }

        long ISQLite3Provider.sqlite3_memory_used()
            => NativeMethods.sqlite3_memory_used();

        long ISQLite3Provider.sqlite3_memory_highwater(int resetFlag)
            => NativeMethods.sqlite3_memory_highwater(resetFlag);

        long ISQLite3Provider.sqlite3_soft_heap_limit64(long n)
            => NativeMethods.sqlite3_soft_heap_limit64(n);

        long ISQLite3Provider.sqlite3_hard_heap_limit64(long n)
            => NativeMethods.sqlite3_hard_heap_limit64(n);

        int ISQLite3Provider.sqlite3_status(int op, out int current, out int highwater, int resetFlag)
            => NativeMethods.sqlite3_status(op, out current, out highwater, resetFlag);

        unsafe utf8z ISQLite3Provider.sqlite3_sourceid()
            => utf8z.FromPtr(NativeMethods.sqlite3_sourceid());

        void ISQLite3Provider.sqlite3_result_int64(IntPtr ctx, long val)
            => NativeMethods.sqlite3_result_int64(ctx, val);

        void ISQLite3Provider.sqlite3_result_int(IntPtr ctx, int val)
            => NativeMethods.sqlite3_result_int(ctx, val);

        void ISQLite3Provider.sqlite3_result_double(IntPtr ctx, double val)
            => NativeMethods.sqlite3_result_double(ctx, val);

        void ISQLite3Provider.sqlite3_result_null(IntPtr stm)
            => NativeMethods.sqlite3_result_null(stm);

        unsafe void ISQLite3Provider.sqlite3_result_error(IntPtr ctx, ReadOnlySpan<byte> val)
        {
            fixed (byte* strErr = val)
            {
                NativeMethods.sqlite3_result_error(ctx, strErr, val.Length);
            }
        }

        unsafe void ISQLite3Provider.sqlite3_result_error(IntPtr ctx, utf8z val)
        {
            fixed (byte* strErr = val)
            {
                NativeMethods.sqlite3_result_error(ctx, strErr, -1);
            }
        }

        unsafe void ISQLite3Provider.sqlite3_result_text(IntPtr ctx, ReadOnlySpan<byte> val)
        {
            fixed (byte* val2 = val)
            {
                NativeMethods.sqlite3_result_text(ctx, val2, val.Length, new IntPtr(-1));
            }
        }

        unsafe void ISQLite3Provider.sqlite3_result_text(IntPtr ctx, utf8z val)
        {
            fixed (byte* val2 = val)
            {
                NativeMethods.sqlite3_result_text(ctx, val2, -1, new IntPtr(-1));
            }
        }

        unsafe void ISQLite3Provider.sqlite3_result_blob(IntPtr ctx, ReadOnlySpan<byte> blob)
        {
            fixed (byte* ptr = blob)
            {
                NativeMethods.sqlite3_result_blob(ctx, (IntPtr)ptr, blob.Length, new IntPtr(-1));
            }
        }

        void ISQLite3Provider.sqlite3_result_zeroblob(IntPtr ctx, int n)
            => NativeMethods.sqlite3_result_zeroblob(ctx, n);

        void ISQLite3Provider.sqlite3_result_error_toobig(IntPtr ctx)
            => NativeMethods.sqlite3_result_error_toobig(ctx);

        void ISQLite3Provider.sqlite3_result_error_nomem(IntPtr ctx)
            => NativeMethods.sqlite3_result_error_nomem(ctx);

        void ISQLite3Provider.sqlite3_result_error_code(IntPtr ctx, int code)
            => NativeMethods.sqlite3_result_error_code(ctx, code);

        unsafe ReadOnlySpan<byte> ISQLite3Provider.sqlite3_value_blob(IntPtr p)
        {
            IntPtr intPtr = NativeMethods.sqlite3_value_blob(p);
            if (intPtr == IntPtr.Zero)
            {
                return null;
            }

            int length = NativeMethods.sqlite3_value_bytes(p);
            return new ReadOnlySpan<byte>(intPtr.ToPointer(), length);
        }

        int ISQLite3Provider.sqlite3_value_bytes(IntPtr p)
            => NativeMethods.sqlite3_value_bytes(p);

        double ISQLite3Provider.sqlite3_value_double(IntPtr p)
            => NativeMethods.sqlite3_value_double(p);

        int ISQLite3Provider.sqlite3_value_int(IntPtr p)
            => NativeMethods.sqlite3_value_int(p);

        long ISQLite3Provider.sqlite3_value_int64(IntPtr p)
            => NativeMethods.sqlite3_value_int64(p);

        int ISQLite3Provider.sqlite3_value_type(IntPtr p)
            => NativeMethods.sqlite3_value_type(p);

        unsafe utf8z ISQLite3Provider.sqlite3_value_text(IntPtr p)
            => utf8z.FromPtr(NativeMethods.sqlite3_value_text(p));

        int ISQLite3Provider.sqlite3_bind_int(sqlite3_stmt stm, int paramIndex, int val)
            => NativeMethods.sqlite3_bind_int(stm, paramIndex, val);

        int ISQLite3Provider.sqlite3_bind_int64(sqlite3_stmt stm, int paramIndex, long val)
            => NativeMethods.sqlite3_bind_int64(stm, paramIndex, val);

        unsafe int ISQLite3Provider.sqlite3_bind_text(sqlite3_stmt stm, int paramIndex, ReadOnlySpan<byte> t)
        {
            fixed (byte* val = t)
            {
                return NativeMethods.sqlite3_bind_text(stm, paramIndex, val, t.Length, new IntPtr(-1));
            }
        }

        unsafe int ISQLite3Provider.sqlite3_bind_text16(sqlite3_stmt stm, int paramIndex, ReadOnlySpan<char> t)
        {
            fixed (char* val = t)
            {
                return NativeMethods.sqlite3_bind_text16(stm, paramIndex, val, t.Length * 2, new IntPtr(-1));
            }
        }

        unsafe int ISQLite3Provider.sqlite3_bind_text(sqlite3_stmt stm, int paramIndex, utf8z t)
        {
            fixed (byte* val = t)
            {
                return NativeMethods.sqlite3_bind_text(stm, paramIndex, val, -1, new IntPtr(-1));
            }
        }

        int ISQLite3Provider.sqlite3_bind_double(sqlite3_stmt stm, int paramIndex, double val)
            => NativeMethods.sqlite3_bind_double(stm, paramIndex, val);

        unsafe int ISQLite3Provider.sqlite3_bind_blob(sqlite3_stmt stm, int paramIndex, ReadOnlySpan<byte> blob)
        {
            if (blob.Length == 0)
            {
                fixed (byte* val = (ReadOnlySpan<byte>)new byte[1] { 42 })
                {
                    return NativeMethods.sqlite3_bind_blob(stm, paramIndex, val, 0, new IntPtr(-1));
                }
            }

            fixed (byte* val2 = blob)
            {
                return NativeMethods.sqlite3_bind_blob(stm, paramIndex, val2, blob.Length, new IntPtr(-1));
            }
        }

        int ISQLite3Provider.sqlite3_bind_zeroblob(sqlite3_stmt stm, int paramIndex, int size)
            => NativeMethods.sqlite3_bind_zeroblob(stm, paramIndex, size);

        int ISQLite3Provider.sqlite3_bind_null(sqlite3_stmt stm, int paramIndex)
            => NativeMethods.sqlite3_bind_null(stm, paramIndex);

        int ISQLite3Provider.sqlite3_bind_parameter_count(sqlite3_stmt stm)
            => NativeMethods.sqlite3_bind_parameter_count(stm);

        unsafe utf8z ISQLite3Provider.sqlite3_bind_parameter_name(sqlite3_stmt stm, int paramIndex)
            => utf8z.FromPtr(NativeMethods.sqlite3_bind_parameter_name(stm, paramIndex));

        unsafe int ISQLite3Provider.sqlite3_bind_parameter_index(sqlite3_stmt stm, utf8z paramName)
        {
            fixed (byte* strName = paramName)
            {
                return NativeMethods.sqlite3_bind_parameter_index(stm, strName);
            }
        }

        int ISQLite3Provider.sqlite3_step(sqlite3_stmt stm)
            => NativeMethods.sqlite3_step(stm);

        int ISQLite3Provider.sqlite3_stmt_isexplain(sqlite3_stmt stm)
            => NativeMethods.sqlite3_stmt_isexplain(stm);

        int ISQLite3Provider.sqlite3_stmt_busy(sqlite3_stmt stm)
            => NativeMethods.sqlite3_stmt_busy(stm);

        int ISQLite3Provider.sqlite3_stmt_readonly(sqlite3_stmt stm)
            => NativeMethods.sqlite3_stmt_readonly(stm);

        int ISQLite3Provider.sqlite3_column_int(sqlite3_stmt stm, int columnIndex)
            => NativeMethods.sqlite3_column_int(stm, columnIndex);

        long ISQLite3Provider.sqlite3_column_int64(sqlite3_stmt stm, int columnIndex)
            => NativeMethods.sqlite3_column_int64(stm, columnIndex);

        unsafe utf8z ISQLite3Provider.sqlite3_column_text(sqlite3_stmt stm, int columnIndex)
        {
            byte* p = NativeMethods.sqlite3_column_text(stm, columnIndex);
            int len = NativeMethods.sqlite3_column_bytes(stm, columnIndex);
            return utf8z.FromPtrLen(p, len);
        }

        unsafe utf8z ISQLite3Provider.sqlite3_column_decltype(sqlite3_stmt stm, int columnIndex)
            => utf8z.FromPtr(NativeMethods.sqlite3_column_decltype(stm, columnIndex));

        double ISQLite3Provider.sqlite3_column_double(sqlite3_stmt stm, int columnIndex)
            => NativeMethods.sqlite3_column_double(stm, columnIndex);

        unsafe ReadOnlySpan<byte> ISQLite3Provider.sqlite3_column_blob(sqlite3_stmt stm, int columnIndex)
        {
            IntPtr intPtr = NativeMethods.sqlite3_column_blob(stm, columnIndex);
            if (intPtr == IntPtr.Zero)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            int length = NativeMethods.sqlite3_column_bytes(stm, columnIndex);
            return new ReadOnlySpan<byte>(intPtr.ToPointer(), length);
        }

        int ISQLite3Provider.sqlite3_column_type(sqlite3_stmt stm, int columnIndex)
            => NativeMethods.sqlite3_column_type(stm, columnIndex);

        int ISQLite3Provider.sqlite3_column_bytes(sqlite3_stmt stm, int columnIndex)
            => NativeMethods.sqlite3_column_bytes(stm, columnIndex);

        int ISQLite3Provider.sqlite3_column_count(sqlite3_stmt stm)
            => NativeMethods.sqlite3_column_count(stm);

        int ISQLite3Provider.sqlite3_data_count(sqlite3_stmt stm)
            => NativeMethods.sqlite3_data_count(stm);

        unsafe utf8z ISQLite3Provider.sqlite3_column_name(sqlite3_stmt stm, int columnIndex)
            => utf8z.FromPtr(NativeMethods.sqlite3_column_name(stm, columnIndex));

        unsafe utf8z ISQLite3Provider.sqlite3_column_origin_name(sqlite3_stmt stm, int columnIndex)
            => utf8z.FromPtr(NativeMethods.sqlite3_column_origin_name(stm, columnIndex));

        unsafe utf8z ISQLite3Provider.sqlite3_column_table_name(sqlite3_stmt stm, int columnIndex)
            => utf8z.FromPtr(NativeMethods.sqlite3_column_table_name(stm, columnIndex));

        unsafe utf8z ISQLite3Provider.sqlite3_column_database_name(sqlite3_stmt stm, int columnIndex)
            => utf8z.FromPtr(NativeMethods.sqlite3_column_database_name(stm, columnIndex));

        int ISQLite3Provider.sqlite3_reset(sqlite3_stmt stm)
            => NativeMethods.sqlite3_reset(stm);

        int ISQLite3Provider.sqlite3_clear_bindings(sqlite3_stmt stm)
            => NativeMethods.sqlite3_clear_bindings(stm);

        int ISQLite3Provider.sqlite3_stmt_status(sqlite3_stmt stm, int op, int resetFlg)
            => NativeMethods.sqlite3_stmt_status(stm, op, resetFlg);

        int ISQLite3Provider.sqlite3_finalize(IntPtr stm)
            => NativeMethods.sqlite3_finalize(stm);

        int ISQLite3Provider.sqlite3_wal_autocheckpoint(sqlite3 db, int n)
            => NativeMethods.sqlite3_wal_autocheckpoint(db, n);

        unsafe int ISQLite3Provider.sqlite3_wal_checkpoint(sqlite3 db, utf8z dbName)
        {
            fixed (byte* dbName2 = dbName)
            {
                return NativeMethods.sqlite3_wal_checkpoint(db, dbName2);
            }
        }

        unsafe int ISQLite3Provider.sqlite3_wal_checkpoint_v2(sqlite3 db, utf8z dbName, int eMode, out int logSize, out int framesCheckPointed)
        {
            fixed (byte* dbName2 = dbName)
            {
                return NativeMethods.sqlite3_wal_checkpoint_v2(db, dbName2, eMode, out logSize, out framesCheckPointed);
            }
        }

        int ISQLite3Provider.sqlite3_keyword_count() => NativeMethods.sqlite3_keyword_count();

        unsafe int ISQLite3Provider.sqlite3_keyword_name(int i, out string name)
        {
            byte* name2;
            int length;
            int result = NativeMethods.sqlite3_keyword_name(i, out name2, out length);
            name = Encoding.UTF8.GetString(name2, length);
            return result;
        }

        IntPtr ISQLite3Provider.sqlite3_malloc(int n) => NativeMethods.sqlite3_malloc(n);

        IntPtr ISQLite3Provider.sqlite3_malloc64(long n) => NativeMethods.sqlite3_malloc64(n);

        unsafe IntPtr ISQLite3Provider.sqlite3_serialize(sqlite3 db, utf8z schema, out long size, int flags)
        {
            fixed (byte* p_schema = schema)
            {
                return NativeMethods.sqlite3_serialize(db, p_schema, out size, flags);
            }
        }

        unsafe int ISQLite3Provider.sqlite3_deserialize(sqlite3 db, utf8z schema, IntPtr data, long szDb, long szBuf, int flags)
        {
            fixed (byte* p_schema = schema)
            {
                return NativeMethods.sqlite3_deserialize(db, p_schema, data, szDb, szBuf, flags);
            }
        }
    }
}
