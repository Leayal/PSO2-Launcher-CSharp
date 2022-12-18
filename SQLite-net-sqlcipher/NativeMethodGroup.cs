using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using SQLitePCL;
using static SQLite.NativeDelegates;
using static SQLite.NativeMethodGroup;

namespace SQLite
{
    /// <summary>Reinvent the wheel? Not actually, it's justa full copy with no-brain copypaste edits. Still an effort.</summary>
    sealed class NativeMethodGroup : SafeHandle
    {
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        [DllImport("kernel32.dll", SetLastError = true), PreserveSig]
        private static extern int GetModuleFileName([In] IntPtr hModule, [Out] char[] lpFilename, [In, MarshalAs(UnmanagedType.U4)] int nSize);

        public string? FullName
        {
            get
            {
                if (this.IsInvalid) return null;
                char[] buffer = ArrayPool<char>.Shared.Rent(1024);
                try
                {
                    var ex = GetFileNameSpan(ref buffer, out var span);
                    if (ex == null)
                    {
                        if (span.IsEmpty)
                        {
                            // Can't be here, unless MS messed up.
                            throw new InvalidProgramException();
                        }
                        else
                        {
                            return new string(span);
                        }
                    }
                    else
                    {
                        throw ex;
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(buffer);
                }
            }
        }

        public string? FileName
        {
            get
            {
                if (this.IsInvalid) return null;
                char[] buffer = ArrayPool<char>.Shared.Rent(1024);
                try
                {
                    var ex = GetFileNameSpan(ref buffer, out var span);
                    if (ex == null)
                    {
                        if (span.IsEmpty)
                        {
                            // Can't be here, unless MS messed up.
                            throw new InvalidProgramException();
                        }
                        else
                        {
                            if (span.IndexOfAny(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) != -1)
                            {
                                return new string(System.IO.Path.GetFileName(span));
                            }
                            else
                            {
                                return new string(span);
                            }
                        }
                    }
                    else
                    {
                        throw ex;
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(buffer);
                }
            }
        }

        private Exception? GetFileNameSpan(ref char[] buffer, out ReadOnlySpan<char> bufferSpan)
        {
            int errorCode, returnLen, retries = 1;
            while (true)
            {
                returnLen = GetModuleFileName(this.handle, buffer, buffer.Length);
                errorCode = Marshal.GetLastPInvokeError();

                if (errorCode != ERROR_INSUFFICIENT_BUFFER) break;

                ArrayPool<char>.Shared.Return(buffer);
                buffer = ArrayPool<char>.Shared.Rent((++retries) * 1024);
            }
            if (errorCode == 0)
            {
                bufferSpan = new ReadOnlySpan<char>(buffer, 0, returnLen);
                return null;
            }
            else
            {
                var ex = Marshal.GetExceptionForHR(errorCode);
                if (ex != null)
                {
                    throw ex;
                }
                else
                {
                    bufferSpan = ReadOnlySpan<char>.Empty;
                    return null;
                }
            }
        }

        public override bool IsInvalid => (this.handle == IntPtr.Zero);

        public NativeMethodGroup(string librarypath) : base(IntPtr.Zero, true)
        {
            var p_lib = NativeLibrary.Load(librarypath);
            this.SetHandle(p_lib);

            // Import functions

            // sqlite3_close
            var p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_close");
            this.sqlite3_close = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_int>(p_exportfunc);

            // sqlite3_close_v2
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_close_v2");
            this.sqlite3_close_v2 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_int>(p_exportfunc);

            // sqlite3_enable_shared_cache
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_enable_shared_cache");
            this.sqlite3_enable_shared_cache = Marshal.GetDelegateForFunctionPointer<d_sqlite3_enable_shared_cache>(p_exportfunc);

            // sqlite3_interrupt
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_interrupt");
            this.sqlite3_interrupt = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db>(p_exportfunc);

            // sqlite3_finalize
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_finalize");
            this.sqlite3_finalize = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_int>(p_exportfunc);

            // sqlite3_reset
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_reset");
            this.sqlite3_reset = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_to_int>(p_exportfunc);

            // sqlite3_clear_bindings
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_clear_bindings");
            this.sqlite3_clear_bindings = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_to_int>(p_exportfunc);

            // sqlite3_stmt_status
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_stmt_status");
            this.sqlite3_stmt_status = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_status>(p_exportfunc);

            // sqlite3_bind_parameter_name
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_bind_parameter_name");
            this.sqlite3_bind_parameter_name = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_column_val>(p_exportfunc);

            // sqlite3_column_database_name
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_database_name");
            this.sqlite3_column_database_name = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_column_val>(p_exportfunc);

            // sqlite3_column_decltype
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_decltype");
            this.sqlite3_column_decltype = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_column_val>(p_exportfunc);

            // sqlite3_column_name
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_name");
            this.sqlite3_column_name = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_column_val>(p_exportfunc);

            // sqlite3_column_origin_name
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_origin_name");
            this.sqlite3_column_origin_name = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_column_val>(p_exportfunc);

            // sqlite3_column_table_name
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_table_name");
            this.sqlite3_column_table_name = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_column_val>(p_exportfunc);

            // sqlite3_column_text
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_text");
            this.sqlite3_column_text = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_column_val>(p_exportfunc);

            // sqlite3_errmsg
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_errmsg");
            this.sqlite3_errmsg = Marshal.GetDelegateForFunctionPointer<d_sqlite3_errmsg>(p_exportfunc);

            // sqlite3_db_readonly
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_db_readonly");
            this.sqlite3_db_readonly = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_readonly>(p_exportfunc);

            // sqlite3_db_filename
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_db_filename");
            this.sqlite3_db_filename = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_filename>(p_exportfunc);

            // sqlite3_prepare_v2
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_prepare_v2");
            this.sqlite3_prepare_v2 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_prepare_v2>(p_exportfunc);

            // sqlite3_prepare_v3
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_prepare_v3");
            this.sqlite3_prepare_v3 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_prepare_v3>(p_exportfunc);

            // sqlite3_db_status
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_db_status");
            this.sqlite3_db_status = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_status>(p_exportfunc);

            // sqlite3_complete
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_complete");
            this.sqlite3_complete = Marshal.GetDelegateForFunctionPointer<d_sqlite3_rptr_to_int>(p_exportfunc);

            // sqlite3_compileoption_used
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_compileoption_used");
            this.sqlite3_compileoption_used = Marshal.GetDelegateForFunctionPointer<d_sqlite3_rptr_to_int>(p_exportfunc);

            // sqlite3_compileoption_get
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_compileoption_get");
            this.sqlite3_compileoption_get = Marshal.GetDelegateForFunctionPointer<d_sqlite3_int_to_rptr>(p_exportfunc);

            // sqlite3_table_column_metadata
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_table_column_metadata");
            this.sqlite3_table_column_metadata = Marshal.GetDelegateForFunctionPointer<d_sqlite3_table_column_metadata>(p_exportfunc);

            // sqlite3_value_text
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_value_text");
            this.sqlite3_value_text = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_rptr>(p_exportfunc);

            // sqlite3_enable_load_extension
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_enable_load_extension");
            this.sqlite3_enable_load_extension = Marshal.GetDelegateForFunctionPointer<d_sqlite3_enable_load_extension>(p_exportfunc);

            // sqlite3_limit
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_limit");
            this.sqlite3_limit = Marshal.GetDelegateForFunctionPointer<d_sqlite3_limit>(p_exportfunc);

            // sqlite3_initialize
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_initialize");
            this.sqlite3_initialize = Marshal.GetDelegateForFunctionPointer<d_sqlite3_noargs_to_int>(p_exportfunc);

            // sqlite3_shutdown
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_shutdown");
            this.sqlite3_shutdown = Marshal.GetDelegateForFunctionPointer<d_sqlite3_noargs_to_int>(p_exportfunc);

            // sqlite3_shutdown
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_shutdown");
            this.sqlite3_shutdown = Marshal.GetDelegateForFunctionPointer<d_sqlite3_noargs_to_int>(p_exportfunc);

            // sqlite3_libversion
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_libversion");
            this.sqlite3_libversion = Marshal.GetDelegateForFunctionPointer<d_sqlite3_noargs_to_rptr>(p_exportfunc);

            // sqlite3_libversion_number
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_libversion_number");
            this.sqlite3_libversion_number = Marshal.GetDelegateForFunctionPointer<d_sqlite3_noargs_to_int>(p_exportfunc);

            // sqlite3_threadsafe
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_threadsafe");
            this.sqlite3_threadsafe = Marshal.GetDelegateForFunctionPointer<d_sqlite3_noargs_to_int>(p_exportfunc);

            // sqlite3_sourceid
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_sourceid");
            this.sqlite3_sourceid = Marshal.GetDelegateForFunctionPointer<d_sqlite3_noargs_to_rptr>(p_exportfunc);

            // sqlite3_malloc
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_malloc");
            this.sqlite3_malloc = Marshal.GetDelegateForFunctionPointer<d_sqlite3_int_to_ptr>(p_exportfunc);

            // sqlite3_realloc
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_realloc");
            this.sqlite3_realloc = Marshal.GetDelegateForFunctionPointer<d_sqlite3_realloc>(p_exportfunc);

            // sqlite3_free
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_free");
            this.sqlite3_free = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_none>(p_exportfunc);

            // sqlite3_stricmp
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_stricmp");
            this.sqlite3_stricmp = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stricmp>(p_exportfunc);

            // sqlite3_strnicmp
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_strnicmp");
            this.sqlite3_strnicmp = Marshal.GetDelegateForFunctionPointer<d_sqlite3_strnicmp>(p_exportfunc);

            // sqlite3_open
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_open");
            this.sqlite3_open = Marshal.GetDelegateForFunctionPointer<d_sqlite3_open>(p_exportfunc);

            // sqlite3_open_v2
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_open_v2");
            this.sqlite3_open_v2 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_open_v2>(p_exportfunc);

            // sqlite3_vfs_find
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_vfs_find");
            this.sqlite3_vfs_find = Marshal.GetDelegateForFunctionPointer<d_sqlite3_rptr_to_ptr>(p_exportfunc);

            // sqlite3_last_insert_rowid
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_last_insert_rowid");
            this.sqlite3_last_insert_rowid = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_to_long>(p_exportfunc);

            // sqlite3_changes
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_changes");
            this.sqlite3_changes = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_to_int>(p_exportfunc);

            // sqlite3_total_changes
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_total_changes");
            this.sqlite3_total_changes = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_to_int>(p_exportfunc);

            // sqlite3_memory_used
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_memory_used");
            this.sqlite3_memory_used = Marshal.GetDelegateForFunctionPointer<d_sqlite3_noargs_to_long>(p_exportfunc);

            // sqlite3_memory_highwater
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_memory_highwater");
            this.sqlite3_memory_highwater = Marshal.GetDelegateForFunctionPointer<d_sqlite3_int_to_long>(p_exportfunc);

            // sqlite3_soft_heap_limit64
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_soft_heap_limit64");
            this.sqlite3_soft_heap_limit64 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_heap_limit64>(p_exportfunc);

            // sqlite3_hard_heap_limit64
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_hard_heap_limit64");
            this.sqlite3_hard_heap_limit64 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_heap_limit64>(p_exportfunc);

            // sqlite3_status
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_status");
            this.sqlite3_status = Marshal.GetDelegateForFunctionPointer<d_sqlite3_status>(p_exportfunc);

            // sqlite3_busy_timeout
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_busy_timeout");
            this.sqlite3_busy_timeout = Marshal.GetDelegateForFunctionPointer<d_sqlite3_busy_timeout>(p_exportfunc);

            // sqlite3_bind_blob
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_bind_blob");
            this.sqlite3_bind_blob = Marshal.GetDelegateForFunctionPointer<d_sqlite3_bind_blob>(p_exportfunc);

            // sqlite3_bind_zeroblob
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_bind_zeroblob");
            this.sqlite3_bind_zeroblob = Marshal.GetDelegateForFunctionPointer<d_sqlite3_bind_zeroblob>(p_exportfunc);

            // sqlite3_bind_double
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_bind_double");
            this.sqlite3_bind_double = Marshal.GetDelegateForFunctionPointer<d_sqlite3_bind_double>(p_exportfunc);

            // sqlite3_bind_int
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_bind_int");
            this.sqlite3_bind_int = Marshal.GetDelegateForFunctionPointer<d_sqlite3_bind_int>(p_exportfunc);

            // sqlite3_bind_int64
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_bind_int64");
            this.sqlite3_bind_int64 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_bind_int64>(p_exportfunc);

            // sqlite3_bind_null
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_bind_null");
            this.sqlite3_bind_null = Marshal.GetDelegateForFunctionPointer<d_sqlite3_columnindex_to_int>(p_exportfunc);

            // sqlite3_bind_text
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_bind_text");
            this.sqlite3_bind_text = Marshal.GetDelegateForFunctionPointer<d_sqlite3_bind_text>(p_exportfunc);

            // sqlite3_bind_text16
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_bind_text16");
            this.sqlite3_bind_text16 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_bind_text16>(p_exportfunc);

            // sqlite3_bind_parameter_count
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_bind_parameter_count");
            this.sqlite3_bind_parameter_count = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_to_int>(p_exportfunc);

            // sqlite3_bind_parameter_index
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_bind_parameter_index");
            this.sqlite3_bind_parameter_index = Marshal.GetDelegateForFunctionPointer<d_sqlite3_bind_parameter_index>(p_exportfunc);

            // sqlite3_column_count
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_count");
            this.sqlite3_column_count = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_to_int>(p_exportfunc);

            // sqlite3_data_count
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_data_count");
            this.sqlite3_data_count = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_to_int>(p_exportfunc);

            // sqlite3_step
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_step");
            this.sqlite3_step = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_to_int>(p_exportfunc);

            // sqlite3_sql
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_sql");
            this.sqlite3_sql = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_to_rptr>(p_exportfunc);

            // sqlite3_column_double
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_double");
            this.sqlite3_column_double = Marshal.GetDelegateForFunctionPointer<d_sqlite3_column_double>(p_exportfunc);

            // sqlite3_column_int
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_int");
            this.sqlite3_column_int = Marshal.GetDelegateForFunctionPointer<d_sqlite3_columnindex_to_int>(p_exportfunc);

            // sqlite3_column_int64
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_int64");
            this.sqlite3_column_int64 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_column_int64>(p_exportfunc);

            // sqlite3_column_blob
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_blob");
            this.sqlite3_column_blob = Marshal.GetDelegateForFunctionPointer<d_sqlite3_column_blob>(p_exportfunc);

            // sqlite3_column_bytes
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_bytes");
            this.sqlite3_column_bytes = Marshal.GetDelegateForFunctionPointer<d_sqlite3_columnindex_to_int>(p_exportfunc);

            // sqlite3_column_type
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_column_type");
            this.sqlite3_column_type = Marshal.GetDelegateForFunctionPointer<d_sqlite3_columnindex_to_int>(p_exportfunc);

            // sqlite3_aggregate_count
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_aggregate_count");
            this.sqlite3_aggregate_count = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_int>(p_exportfunc);

            // sqlite3_value_blob
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_value_blob");
            this.sqlite3_value_blob = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_ptr>(p_exportfunc);

            // sqlite3_value_bytes
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_value_bytes");
            this.sqlite3_value_bytes = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_int>(p_exportfunc);

            // sqlite3_value_double
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_value_double");
            this.sqlite3_value_double = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_double>(p_exportfunc);

            // sqlite3_value_int
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_value_int");
            this.sqlite3_value_int = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_int>(p_exportfunc);

            // sqlite3_value_int64
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_value_int64");
            this.sqlite3_value_int64 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_long>(p_exportfunc);

            // sqlite3_value_type
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_value_type");
            this.sqlite3_value_type = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_int>(p_exportfunc);

            // sqlite3_user_data
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_user_data");
            this.sqlite3_user_data = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_ptr>(p_exportfunc);

            // sqlite3_result_blob
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_result_blob");
            this.sqlite3_result_blob = Marshal.GetDelegateForFunctionPointer<d_sqlite3_result_blob>(p_exportfunc);

            // sqlite3_result_double
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_result_double");
            this.sqlite3_result_double = Marshal.GetDelegateForFunctionPointer<d_sqlite3_result_double>(p_exportfunc);

            // sqlite3_result_error
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_result_error");
            this.sqlite3_result_error = Marshal.GetDelegateForFunctionPointer<d_sqlite3_result_error>(p_exportfunc);

            // sqlite3_result_int
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_result_int");
            this.sqlite3_result_int = Marshal.GetDelegateForFunctionPointer<d_sqlite3_result_int_zeroblob>(p_exportfunc);

            // sqlite3_result_int64
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_result_int64");
            this.sqlite3_result_int64 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_result_int64>(p_exportfunc);

            // sqlite3_result_null
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_result_null");
            this.sqlite3_result_null = Marshal.GetDelegateForFunctionPointer<d_sqlite3_result_null>(p_exportfunc);

            // sqlite3_result_text
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_result_text");
            this.sqlite3_result_text = Marshal.GetDelegateForFunctionPointer<d_sqlite3_result_text>(p_exportfunc);

            // sqlite3_result_zeroblob
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_result_zeroblob");
            this.sqlite3_result_zeroblob = Marshal.GetDelegateForFunctionPointer<d_sqlite3_result_int_zeroblob>(p_exportfunc);

            // sqlite3_result_error_toobig
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_result_error_toobig");
            this.sqlite3_result_error_toobig = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_none>(p_exportfunc);

            // sqlite3_result_error_nomem
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_result_error_nomem");
            this.sqlite3_result_error_nomem = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_none>(p_exportfunc);

            // sqlite3_result_error_code
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_result_error_code");
            this.sqlite3_result_error_code = Marshal.GetDelegateForFunctionPointer<d_sqlite3_result_error_code>(p_exportfunc);

            // sqlite3_aggregate_context
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_aggregate_context");
            this.sqlite3_aggregate_context = Marshal.GetDelegateForFunctionPointer<d_sqlite3_aggregate_context>(p_exportfunc);

            // sqlite3_key
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_key");
            this.sqlite3_key = Marshal.GetDelegateForFunctionPointer<d_sqlite3_key>(p_exportfunc);

            // sqlite3_key_v2
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_key_v2");
            this.sqlite3_key_v2 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_key_v2>(p_exportfunc);

            // sqlite3_rekey
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_rekey");
            this.sqlite3_rekey = Marshal.GetDelegateForFunctionPointer<d_sqlite3_key>(p_exportfunc);

            // sqlite3_rekey_v2
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_rekey_v2");
            this.sqlite3_rekey_v2 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_key_v2>(p_exportfunc);

            // sqlite3_config_none
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_config");
            this.sqlite3_config_none = Marshal.GetDelegateForFunctionPointer<d_sqlite3_int_to_int>(p_exportfunc);

            // sqlite3_config_int
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_config");
            this.sqlite3_config_int = Marshal.GetDelegateForFunctionPointer<d_sqlite3_config_int>(p_exportfunc);

            // sqlite3_config_int_arm64cc
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_config");
            this.sqlite3_config_int_arm64cc = Marshal.GetDelegateForFunctionPointer<d_sqlite3_config_int_arm64cc>(p_exportfunc);

            // sqlite3_config_log
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_config");
            this.sqlite3_config_log = Marshal.GetDelegateForFunctionPointer<d_sqlite3_config_log>(p_exportfunc);

            // sqlite3_config_log_arm64cc
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_config");
            this.sqlite3_config_log_arm64cc = Marshal.GetDelegateForFunctionPointer<d_sqlite3_config_log_arm64cc>(p_exportfunc);

            // sqlite3_db_config_charptr
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_db_config");
            this.sqlite3_db_config_charptr = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_config_charptr>(p_exportfunc);

            // sqlite3_db_config_charptr_arm64cc
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_db_config");
            this.sqlite3_db_config_charptr_arm64cc = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_config_charptr_arm64cc>(p_exportfunc);

            // sqlite3_db_config_int_outint
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_db_config");
            this.sqlite3_db_config_int_outint = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_config_int_outint>(p_exportfunc);

            // sqlite3_db_config_int_outint_arm64cc
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_db_config");
            this.sqlite3_db_config_int_outint_arm64cc = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_config_int_outint_arm64cc>(p_exportfunc);

            // sqlite3_db_config_intptr_int_int
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_db_config");
            this.sqlite3_db_config_intptr_int_int = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_config_intptr_int_int>(p_exportfunc);

            // sqlite3_db_config_intptr_int_int_arm64cc
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_db_config");
            this.sqlite3_db_config_intptr_int_int_arm64cc = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_config_intptr_int_int_arm64cc>(p_exportfunc);

            // sqlite3_create_collation
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_create_collation");
            this.sqlite3_create_collation = Marshal.GetDelegateForFunctionPointer<d_sqlite3_create_collation>(p_exportfunc);

            // sqlite3_update_hook
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_update_hook");
            this.sqlite3_update_hook = Marshal.GetDelegateForFunctionPointer<d_sqlite3_update_hook>(p_exportfunc);

            // sqlite3_commit_hook
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_commit_hook");
            this.sqlite3_commit_hook = Marshal.GetDelegateForFunctionPointer<d_sqlite3_commit_hook>(p_exportfunc);

            // sqlite3_profile
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_profile");
            this.sqlite3_profile = Marshal.GetDelegateForFunctionPointer<d_sqlite3_profile>(p_exportfunc);

            // sqlite3_progress_handler
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_progress_handler");
            this.sqlite3_progress_handler = Marshal.GetDelegateForFunctionPointer<d_sqlite3_progress_handler>(p_exportfunc);

            // sqlite3_trace
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_trace");
            this.sqlite3_trace = Marshal.GetDelegateForFunctionPointer<d_sqlite3_trace>(p_exportfunc);

            // sqlite3_rollback_hook
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_rollback_hook");
            this.sqlite3_rollback_hook = Marshal.GetDelegateForFunctionPointer<d_sqlite3_rollback_hook>(p_exportfunc);

            // sqlite3_db_handle
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_db_handle");
            this.sqlite3_db_handle = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_ptr>(p_exportfunc);

            // sqlite3_next_stmt
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_next_stmt");
            this.sqlite3_next_stmt = Marshal.GetDelegateForFunctionPointer<d_sqlite3_next_stmt>(p_exportfunc);

            // sqlite3_stmt_isexplain
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_stmt_isexplain");
            this.sqlite3_stmt_isexplain = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_to_int>(p_exportfunc);

            // sqlite3_stmt_busy
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_stmt_busy");
            this.sqlite3_stmt_busy = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_to_int>(p_exportfunc);

            // sqlite3_stmt_readonly
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_stmt_readonly");
            this.sqlite3_stmt_readonly = Marshal.GetDelegateForFunctionPointer<d_sqlite3_stmt_to_int>(p_exportfunc);

            // sqlite3_exec
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_exec");
            this.sqlite3_exec = Marshal.GetDelegateForFunctionPointer<d_sqlite3_exec>(p_exportfunc);

            // sqlite3_get_autocommit
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_get_autocommit");
            this.sqlite3_get_autocommit = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_to_int>(p_exportfunc);

            // sqlite3_extended_result_codes
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_extended_result_codes");
            this.sqlite3_extended_result_codes = Marshal.GetDelegateForFunctionPointer<d_sqlite3_extended_result_codes>(p_exportfunc);

            // sqlite3_errcode
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_errcode");
            this.sqlite3_errcode = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_to_int>(p_exportfunc);

            // sqlite3_extended_errcode
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_extended_errcode");
            this.sqlite3_extended_errcode = Marshal.GetDelegateForFunctionPointer<d_sqlite3_db_to_int>(p_exportfunc);

            // sqlite3_errstr
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_errstr");
            this.sqlite3_errstr = Marshal.GetDelegateForFunctionPointer<d_sqlite3_int_to_rptr>(p_exportfunc);

            // sqlite3_log
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_log");
            this.sqlite3_log = Marshal.GetDelegateForFunctionPointer<d_sqlite3_log>(p_exportfunc);

            // sqlite3_file_control
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_file_control");
            this.sqlite3_file_control = Marshal.GetDelegateForFunctionPointer<d_sqlite3_file_control>(p_exportfunc);

            // sqlite3_backup_init
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_backup_init");
            this.sqlite3_backup_init = Marshal.GetDelegateForFunctionPointer<d_sqlite3_backup_init>(p_exportfunc);

            // sqlite3_backup_step
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_backup_step");
            this.sqlite3_backup_step = Marshal.GetDelegateForFunctionPointer<d_sqlite3_backup_step>(p_exportfunc);

            // sqlite3_backup_remaining
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_backup_remaining");
            this.sqlite3_backup_remaining = Marshal.GetDelegateForFunctionPointer<d_sqlite3_backup_to_int>(p_exportfunc);

            // sqlite3_backup_pagecount
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_backup_pagecount");
            this.sqlite3_backup_pagecount = Marshal.GetDelegateForFunctionPointer<d_sqlite3_backup_to_int>(p_exportfunc);

            // sqlite3_backup_finish
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_backup_finish");
            this.sqlite3_backup_finish = Marshal.GetDelegateForFunctionPointer<d_sqlite3_backup_finish>(p_exportfunc);

            // sqlite3_snapshot_get
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_snapshot_get");
            this.sqlite3_snapshot_get = Marshal.GetDelegateForFunctionPointer<d_sqlite3_snapshot_get>(p_exportfunc);

            // sqlite3_snapshot_open
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_snapshot_open");
            this.sqlite3_snapshot_open = Marshal.GetDelegateForFunctionPointer<d_sqlite3_snapshot_open>(p_exportfunc);

            // sqlite3_snapshot_recover
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_snapshot_recover");
            this.sqlite3_snapshot_recover = Marshal.GetDelegateForFunctionPointer<d_sqlite3_snapshot_recover>(p_exportfunc);

            // sqlite3_snapshot_cmp
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_snapshot_cmp");
            this.sqlite3_snapshot_cmp = Marshal.GetDelegateForFunctionPointer<d_sqlite3_snapshot_cmp>(p_exportfunc);

            // sqlite3_snapshot_free
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_snapshot_free");
            this.sqlite3_snapshot_free = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_none>(p_exportfunc);

            // sqlite3_blob_open
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_blob_open");
            this.sqlite3_blob_open = Marshal.GetDelegateForFunctionPointer<d_sqlite3_blob_open>(p_exportfunc);

            // sqlite3_blob_write
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_blob_write");
            this.sqlite3_blob_write = Marshal.GetDelegateForFunctionPointer<d_sqlite3_blob_readwrite>(p_exportfunc);

            // sqlite3_blob_read
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_blob_read");
            this.sqlite3_blob_read = Marshal.GetDelegateForFunctionPointer<d_sqlite3_blob_readwrite>(p_exportfunc);

            // sqlite3_blob_bytes
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_blob_bytes");
            this.sqlite3_blob_bytes = Marshal.GetDelegateForFunctionPointer<d_sqlite3_blob_bytes>(p_exportfunc);

            // sqlite3_blob_reopen
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_blob_reopen");
            this.sqlite3_blob_reopen = Marshal.GetDelegateForFunctionPointer<d_sqlite3_blob_reopen>(p_exportfunc);

            // sqlite3_blob_close
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_blob_close");
            this.sqlite3_blob_close = Marshal.GetDelegateForFunctionPointer<d_sqlite3_ptr_to_int>(p_exportfunc);

            // sqlite3_wal_autocheckpoint
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_wal_autocheckpoint");
            this.sqlite3_wal_autocheckpoint = Marshal.GetDelegateForFunctionPointer<d_sqlite3_wal_autocheckpoint>(p_exportfunc);

            // sqlite3_wal_checkpoint
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_wal_checkpoint");
            this.sqlite3_wal_checkpoint = Marshal.GetDelegateForFunctionPointer<d_sqlite3_wal_checkpoint>(p_exportfunc);

            // sqlite3_wal_checkpoint_v2
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_wal_checkpoint_v2");
            this.sqlite3_wal_checkpoint_v2 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_wal_checkpoint_v2>(p_exportfunc);

            // sqlite3_set_authorizer
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_set_authorizer");
            this.sqlite3_set_authorizer = Marshal.GetDelegateForFunctionPointer<d_sqlite3_set_authorizer>(p_exportfunc);

            // sqlite3_win32_set_directory8
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_win32_set_directory8");
            this.sqlite3_win32_set_directory8 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_win32_set_directory8>(p_exportfunc);

            // sqlite3_create_function_v2
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_create_function_v2");
            this.sqlite3_create_function_v2 = Marshal.GetDelegateForFunctionPointer<d_sqlite3_create_function_v2>(p_exportfunc);

            // sqlite3_keyword_count
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_keyword_count");
            this.sqlite3_keyword_count = Marshal.GetDelegateForFunctionPointer<d_sqlite3_noargs_to_int>(p_exportfunc);

            // sqlite3_keyword_name
            p_exportfunc = NativeLibrary.GetExport(p_lib, "sqlite3_keyword_name");
            this.sqlite3_keyword_name = Marshal.GetDelegateForFunctionPointer<d_sqlite3_keyword_name>(p_exportfunc);
        }

        protected override bool ReleaseHandle()
        {
            if (!this.IsInvalid)
            {
                NativeLibrary.Free(this.handle);
                this.SetHandleAsInvalid();
            }
            return true;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_ptr_to_int(IntPtr db);
        public readonly d_sqlite3_ptr_to_int sqlite3_close,
            sqlite3_close_v2,
            sqlite3_aggregate_count,
            sqlite3_value_bytes,
            sqlite3_value_int,
            sqlite3_finalize,
            sqlite3_value_type,
            sqlite3_blob_close;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_enable_shared_cache(int enable);
        public readonly d_sqlite3_enable_shared_cache sqlite3_enable_shared_cache;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void d_sqlite3_db(sqlite3 db);
        public readonly d_sqlite3_db sqlite3_interrupt;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_stmt_status(sqlite3_stmt stm, int op, int resetFlg);
        public readonly d_sqlite3_stmt_status sqlite3_stmt_status;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate byte* d_sqlite3_stmt_column_val(sqlite3_stmt stmt, int index);
        public readonly d_sqlite3_stmt_column_val sqlite3_bind_parameter_name,
            sqlite3_column_database_name,
            sqlite3_column_decltype,
            sqlite3_column_name,
            sqlite3_column_origin_name,
            sqlite3_column_table_name,
            sqlite3_column_text;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate byte* d_sqlite3_errmsg(sqlite3 db);
        public readonly d_sqlite3_errmsg sqlite3_errmsg;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_db_readonly(sqlite3 db, byte* dbName);
        public readonly d_sqlite3_db_readonly sqlite3_db_readonly;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate byte* d_sqlite3_db_filename(sqlite3 db, byte* att);
        public readonly d_sqlite3_db_filename sqlite3_db_filename;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_prepare_v2(sqlite3 db, byte* pSql, int nBytes, out IntPtr stmt, out byte* ptrRemain);
        public readonly d_sqlite3_prepare_v2 sqlite3_prepare_v2;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_prepare_v3(sqlite3 db, byte* pSql, int nBytes, uint flags, out IntPtr stmt, out byte* ptrRemain);
        public readonly d_sqlite3_prepare_v3 sqlite3_prepare_v3;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_db_status(sqlite3 db, int op, out int current, out int highest, int resetFlg);
        public readonly d_sqlite3_db_status sqlite3_db_status;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_rptr_to_int(byte* pSql);
        public readonly d_sqlite3_rptr_to_int sqlite3_complete, sqlite3_compileoption_used;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate byte* d_sqlite3_int_to_rptr(int n);
        public readonly d_sqlite3_int_to_rptr sqlite3_compileoption_get,
            sqlite3_errstr;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_table_column_metadata(sqlite3 db, byte* dbName, byte* tblName, byte* colName, out byte* ptrDataType, out byte* ptrCollSeq, out int notNull, out int primaryKey, out int autoInc);
        public readonly d_sqlite3_table_column_metadata sqlite3_table_column_metadata;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate byte* d_sqlite3_ptr_to_rptr(IntPtr p);
        public readonly d_sqlite3_ptr_to_rptr sqlite3_value_text;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_enable_load_extension(sqlite3 db, int enable);
        public readonly d_sqlite3_enable_load_extension sqlite3_enable_load_extension;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_limit(sqlite3 db, int id, int newVal);
        public readonly d_sqlite3_limit sqlite3_limit;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_noargs_to_int();
        public readonly d_sqlite3_noargs_to_int sqlite3_initialize,
            sqlite3_shutdown,
            sqlite3_libversion_number,
            sqlite3_threadsafe,
            sqlite3_keyword_count;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate byte* d_sqlite3_noargs_to_rptr();
        public readonly d_sqlite3_noargs_to_rptr sqlite3_libversion, sqlite3_sourceid;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr d_sqlite3_int_to_ptr(int n);
        public readonly d_sqlite3_int_to_ptr sqlite3_malloc;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr d_sqlite3_realloc(IntPtr p, int n);
        public readonly d_sqlite3_realloc sqlite3_realloc;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void d_sqlite3_ptr_none(IntPtr p);
        public readonly d_sqlite3_ptr_none sqlite3_free,
            sqlite3_result_error_toobig,
            sqlite3_result_error_nomem,
            sqlite3_snapshot_free;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_stricmp(IntPtr p, IntPtr q);
        public readonly d_sqlite3_stricmp sqlite3_stricmp;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_strnicmp(IntPtr p, IntPtr q, int n);
        public readonly d_sqlite3_strnicmp sqlite3_strnicmp;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_open(byte* filename, out IntPtr db);
        public readonly d_sqlite3_open sqlite3_open;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_open_v2(byte* filename, out IntPtr db, int flags, byte* vfs);
        public readonly d_sqlite3_open_v2 sqlite3_open_v2;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate IntPtr d_sqlite3_rptr_to_ptr(byte* vfs);
        public readonly d_sqlite3_rptr_to_ptr sqlite3_vfs_find;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long d_sqlite3_db_to_long(sqlite3 db);
        public readonly d_sqlite3_db_to_long sqlite3_last_insert_rowid;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_db_to_int(sqlite3 db);
        public readonly d_sqlite3_db_to_int sqlite3_changes,
            sqlite3_total_changes,
            sqlite3_get_autocommit,
            sqlite3_errcode,
            sqlite3_extended_errcode;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long d_sqlite3_noargs_to_long();
        public readonly d_sqlite3_noargs_to_long sqlite3_memory_used;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long d_sqlite3_int_to_long(int n);
        public readonly d_sqlite3_int_to_long sqlite3_memory_highwater;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long d_sqlite3_heap_limit64(long n);
        public readonly d_sqlite3_heap_limit64 sqlite3_soft_heap_limit64, sqlite3_hard_heap_limit64;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_status(int op, out int current, out int highwater, int resetFlag);
        public readonly d_sqlite3_status sqlite3_status;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_busy_timeout(sqlite3 db, int ms);
        public readonly d_sqlite3_busy_timeout sqlite3_busy_timeout;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_bind_blob(sqlite3_stmt stmt, int index, byte* val, int nSize, IntPtr nTransient);
        public readonly d_sqlite3_bind_blob sqlite3_bind_blob;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_bind_zeroblob(sqlite3_stmt stmt, int index, int size);
        public readonly d_sqlite3_bind_zeroblob sqlite3_bind_zeroblob;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_bind_double(sqlite3_stmt stmt, int index, double val);
        public readonly d_sqlite3_bind_double sqlite3_bind_double;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_bind_int(sqlite3_stmt stmt, int index, int val);
        public readonly d_sqlite3_bind_int sqlite3_bind_int;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_bind_int64(sqlite3_stmt stmt, int index, long val);
        public readonly d_sqlite3_bind_int64 sqlite3_bind_int64;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_columnindex_to_int(sqlite3_stmt stmt, int index);
        public readonly d_sqlite3_columnindex_to_int sqlite3_bind_null,
            sqlite3_column_int,
            sqlite3_column_bytes,
            sqlite3_column_type;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_bind_text(sqlite3_stmt stmt, int index, byte* val, int nlen, IntPtr pvReserved);
        public readonly d_sqlite3_bind_text sqlite3_bind_text;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_bind_text16(sqlite3_stmt stmt, int index, char* val, int nlen, IntPtr pvReserved);
        public readonly d_sqlite3_bind_text16 sqlite3_bind_text16;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_stmt_to_int(sqlite3_stmt stmt);
        public readonly d_sqlite3_stmt_to_int sqlite3_reset,
            sqlite3_clear_bindings,
            sqlite3_bind_parameter_count,
            sqlite3_column_count,
            sqlite3_data_count,
            sqlite3_step,
            sqlite3_stmt_isexplain,
            sqlite3_stmt_busy,
            sqlite3_stmt_readonly;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_bind_parameter_index(sqlite3_stmt stmt, byte* strName);
        public readonly d_sqlite3_bind_parameter_index sqlite3_bind_parameter_index;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate byte* d_sqlite3_stmt_to_rptr(sqlite3_stmt stmt);
        public readonly d_sqlite3_stmt_to_rptr sqlite3_sql;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double d_sqlite3_column_double(sqlite3_stmt stmt, int index);
        public readonly d_sqlite3_column_double sqlite3_column_double;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long d_sqlite3_column_int64(sqlite3_stmt stmt, int index);
        public readonly d_sqlite3_column_int64 sqlite3_column_int64;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr d_sqlite3_column_blob(sqlite3_stmt stmt, int index);
        public readonly d_sqlite3_column_blob sqlite3_column_blob;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr d_sqlite3_ptr_to_ptr(IntPtr p);
        public readonly d_sqlite3_ptr_to_ptr sqlite3_value_blob,
            sqlite3_user_data,
            sqlite3_db_handle;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double d_sqlite3_ptr_to_double(IntPtr p);
        public readonly d_sqlite3_ptr_to_double sqlite3_value_double;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long d_sqlite3_ptr_to_long(IntPtr p);
        public readonly d_sqlite3_ptr_to_long sqlite3_value_int64;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void d_sqlite3_result_blob(IntPtr context, IntPtr val, int nSize, IntPtr pvReserved);
        public readonly d_sqlite3_result_blob sqlite3_result_blob;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void d_sqlite3_result_double(IntPtr context, double val);
        public readonly d_sqlite3_result_double sqlite3_result_double;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void d_sqlite3_result_error(IntPtr context, byte* strErr, int nLen);
        public readonly d_sqlite3_result_error sqlite3_result_error;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void d_sqlite3_result_int_zeroblob(IntPtr context, int val);
        public readonly d_sqlite3_result_int_zeroblob sqlite3_result_int, sqlite3_result_zeroblob;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void d_sqlite3_result_int64(IntPtr context, long val);
        public readonly d_sqlite3_result_int64 sqlite3_result_int64;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void d_sqlite3_result_null(IntPtr context);
        public readonly d_sqlite3_result_null sqlite3_result_null;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void d_sqlite3_result_text(IntPtr context, byte* val, int nLen, IntPtr pvReserved);
        public readonly d_sqlite3_result_text sqlite3_result_text;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void d_sqlite3_result_error_code(IntPtr context, int code);
        public readonly d_sqlite3_result_error_code sqlite3_result_error_code;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr d_sqlite3_aggregate_context(IntPtr context, int nBytes);
        public readonly d_sqlite3_aggregate_context sqlite3_aggregate_context;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_key(sqlite3 db, byte* key, int keylen);
        public readonly d_sqlite3_key sqlite3_key, sqlite3_rekey;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_key_v2(sqlite3 db, byte* dbname, byte* key, int keylen);
        public readonly d_sqlite3_key_v2 sqlite3_key_v2, sqlite3_rekey_v2;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_int_to_int(int op);
        public readonly d_sqlite3_int_to_int sqlite3_config_none;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_config_int(int op, int val);
        public readonly d_sqlite3_config_int sqlite3_config_int;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_config_int_arm64cc(int op, IntPtr dummy1, IntPtr dummy2, IntPtr dummy3, IntPtr dummy4, IntPtr dummy5, IntPtr dummy6, IntPtr dummy7, int val);
        public readonly d_sqlite3_config_int_arm64cc sqlite3_config_int_arm64cc;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_config_log(int op, callback_log? func, hook_handle pvUser);
        public readonly d_sqlite3_config_log sqlite3_config_log;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_config_log_arm64cc(int op, IntPtr dummy1, IntPtr dummy2, IntPtr dummy3, IntPtr dummy4, IntPtr dummy5, IntPtr dummy6, IntPtr dummy7, callback_log? func, hook_handle pvUser);
        public readonly d_sqlite3_config_log_arm64cc sqlite3_config_log_arm64cc;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_db_config_charptr(sqlite3 db, int op, byte* val);
        public readonly d_sqlite3_db_config_charptr sqlite3_db_config_charptr;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_db_config_charptr_arm64cc(sqlite3 db, int op, IntPtr dummy2, IntPtr dummy3, IntPtr dummy4, IntPtr dummy5, IntPtr dummy6, IntPtr dummy7, byte* val);
        public readonly d_sqlite3_db_config_charptr_arm64cc sqlite3_db_config_charptr_arm64cc;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_db_config_int_outint(sqlite3 db, int op, int val, int* result);
        public readonly d_sqlite3_db_config_int_outint sqlite3_db_config_int_outint;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_db_config_int_outint_arm64cc(sqlite3 db, int op, IntPtr dummy2, IntPtr dummy3, IntPtr dummy4, IntPtr dummy5, IntPtr dummy6, IntPtr dummy7, int val, int* result);
        public readonly d_sqlite3_db_config_int_outint_arm64cc sqlite3_db_config_int_outint_arm64cc;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_db_config_intptr_int_int(sqlite3 db, int op, IntPtr ptr, int int0, int int1);
        public readonly d_sqlite3_db_config_intptr_int_int sqlite3_db_config_intptr_int_int;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_db_config_intptr_int_int_arm64cc(sqlite3 db, int op, IntPtr dummy2, IntPtr dummy3, IntPtr dummy4, IntPtr dummy5, IntPtr dummy6, IntPtr dummy7, IntPtr ptr, int int0, int int1);
        public readonly d_sqlite3_db_config_intptr_int_int_arm64cc sqlite3_db_config_intptr_int_int_arm64cc;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_create_collation(sqlite3 db, byte[] strName, int nType, hook_handle pvUser, callback_collation func);
        public readonly d_sqlite3_create_collation sqlite3_create_collation;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr d_sqlite3_update_hook(sqlite3 db, callback_update func, hook_handle pvUser);
        public readonly d_sqlite3_update_hook sqlite3_update_hook;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr d_sqlite3_commit_hook(sqlite3 db, callback_commit? func, hook_handle pvUser);
        public readonly d_sqlite3_commit_hook sqlite3_commit_hook;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr d_sqlite3_profile(sqlite3 db, callback_profile func, hook_handle pvUser);
        public readonly d_sqlite3_profile sqlite3_profile;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void d_sqlite3_progress_handler(sqlite3 db, int instructions, callback_progress_handler func, hook_handle pvUser);
        public readonly d_sqlite3_progress_handler sqlite3_progress_handler;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr d_sqlite3_trace(sqlite3 db, callback_trace func, hook_handle pvUser);
        public readonly d_sqlite3_trace sqlite3_trace;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr d_sqlite3_rollback_hook(sqlite3 db, callback_rollback func, hook_handle pvUser);
        public readonly d_sqlite3_rollback_hook sqlite3_rollback_hook;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr d_sqlite3_next_stmt(sqlite3 db, IntPtr stmt);
        public readonly d_sqlite3_next_stmt sqlite3_next_stmt;


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_exec(sqlite3 db, byte* strSql, callback_exec? cb, hook_handle pvParam, out IntPtr errMsg);
        public readonly d_sqlite3_exec sqlite3_exec;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_extended_result_codes(sqlite3 db, int onoff);
        public readonly d_sqlite3_extended_result_codes sqlite3_extended_result_codes;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void d_sqlite3_log(int iErrCode, byte* zFormat);
        public readonly d_sqlite3_log sqlite3_log;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_file_control(sqlite3 db, byte[] zDbName, int op, IntPtr pArg);
        public readonly d_sqlite3_file_control sqlite3_file_control;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate sqlite3_backup d_sqlite3_backup_init(sqlite3 destDb, byte* zDestName, sqlite3 sourceDb, byte* zSourceName);
        public readonly d_sqlite3_backup_init sqlite3_backup_init;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_backup_step(sqlite3_backup backup, int nPage);
        public readonly d_sqlite3_backup_step sqlite3_backup_step;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_backup_to_int(sqlite3_backup backup);
        public readonly d_sqlite3_backup_to_int sqlite3_backup_remaining,
            sqlite3_backup_pagecount;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_backup_finish(IntPtr backup);
        public readonly d_sqlite3_backup_finish sqlite3_backup_finish;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_snapshot_get(sqlite3 db, byte* schema, out IntPtr snap);
        public readonly d_sqlite3_snapshot_get sqlite3_snapshot_get;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_snapshot_open(sqlite3 db, byte* schema, sqlite3_snapshot snap);
        public readonly d_sqlite3_snapshot_open sqlite3_snapshot_open;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_snapshot_recover(sqlite3 db, byte* name);
        public readonly d_sqlite3_snapshot_recover sqlite3_snapshot_recover;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_snapshot_cmp(sqlite3_snapshot p1, sqlite3_snapshot p2);
        public readonly d_sqlite3_snapshot_cmp sqlite3_snapshot_cmp;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_blob_open(sqlite3 db, byte* sdb, byte* table, byte* col, long rowid, int flags, out sqlite3_blob blob);
        public readonly d_sqlite3_blob_open sqlite3_blob_open;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_blob_readwrite(sqlite3_blob blob, byte* b, int n, int offset);
        public readonly d_sqlite3_blob_readwrite sqlite3_blob_write, sqlite3_blob_read;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_blob_bytes(sqlite3_blob blob);
        public readonly d_sqlite3_blob_bytes sqlite3_blob_bytes;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_blob_reopen(sqlite3_blob blob, long rowid);
        public readonly d_sqlite3_blob_reopen sqlite3_blob_reopen;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_wal_autocheckpoint(sqlite3 db, int n);
        public readonly d_sqlite3_wal_autocheckpoint sqlite3_wal_autocheckpoint;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_wal_checkpoint(sqlite3 db, byte* dbName);
        public readonly d_sqlite3_wal_checkpoint sqlite3_wal_checkpoint;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_wal_checkpoint_v2(sqlite3 db, byte* dbName, int eMode, out int logSize, out int framesCheckPointed);
        public readonly d_sqlite3_wal_checkpoint_v2 sqlite3_wal_checkpoint_v2;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_set_authorizer(sqlite3 db, callback_authorizer cb, hook_handle pvUser);
        public readonly d_sqlite3_set_authorizer sqlite3_set_authorizer;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_win32_set_directory8(uint directoryType, byte* directoryPath);
        public readonly d_sqlite3_win32_set_directory8 sqlite3_win32_set_directory8;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int d_sqlite3_create_function_v2(sqlite3 db, byte[] strName, int nArgs, int nType, hook_handle pvUser, callback_scalar_function? func, callback_agg_function_step? fstep, callback_agg_function_final? ffinal, callback_destroy? fdestroy);
        public readonly d_sqlite3_create_function_v2 sqlite3_create_function_v2;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int d_sqlite3_keyword_name(int i, out byte* name, out int length);
        public readonly d_sqlite3_keyword_name sqlite3_keyword_name;
    }
}
