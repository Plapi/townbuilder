using System;
using System.ComponentModel;
using System.IO;
using System.Net.WebSockets;
using System.Text;

namespace Cysharp.Threading.Tasks
{
    public static class ExceptionExtensions
    {
        public static bool IsOperationCanceledException(this Exception exception)
        {
            return exception is OperationCanceledException;
        }
        
        public static string UnrollExceptionMessage(this Exception exception)
        {
            var builder = new StringBuilder();

            var ex = exception;
            
            while (ex != null)
            {
                if (builder.Length > 0)
                    builder.Append(" > ");

                builder.Append(ex.Message);
                ex = ex.InnerException;
            }

            return builder.ToString();
        }
        
        public static string UnrollExceptionType(this Exception exception, bool withErrorCodes = false)
        {
            var builder = new StringBuilder();

            var ex = exception;
            
            while (ex != null)
            {
                if (builder.Length > 0)
                    builder.Append(" > ");

                builder.Append(ex.GetType().Name);

                if (withErrorCodes)
                {
                    if (ex is WebSocketException wsEx)
                    {
                        builder.Append($"({wsEx.WebSocketErrorCode}:{wsEx.NativeErrorCode})");
                    }
                    else if (ex is System.Net.Sockets.SocketException sockEx)
                    {
                        builder.Append($"({sockEx.SocketErrorCode}:{sockEx.NativeErrorCode})");
                    }
                    else if (ex is IOException ioEx)
                    {
                        builder.Append($"({ioEx.HResult})");
                    }
                    else if (ex is Win32Exception win32Ex)
                    {
                        builder.Append($"({win32Ex.ErrorCode})");
                    }
                }
                
                ex = ex.InnerException;
            }

            return builder.ToString();
        }
    }
}

