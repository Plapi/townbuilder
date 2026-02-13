using System;

namespace Cysharp.Threading.Tasks
{
    public class ForgotTaskException : Exception
    {
        public string Context { get; }

        public ForgotTaskException(Exception ex, string context) : base( nameof(ForgotTaskException), ex )
        {
            Context = context;
        }
    }
}