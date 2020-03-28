//using System;
//using System.Runtime.CompilerServices;

//namespace CodeOwls.PowerShell.Provider
//{
//    public sealed class ProviderLogger
//    {
//        public static ProviderMethodLogger StepIn([CallerMemberName] string memberName = "")
//        {
//            var logger = new ProviderMethodLogger(memberName);
//            logger.Log($"{memberName}:StepIn");
//            return logger;
//        }
//    }

//    public readonly struct ProviderMethodLogger : IDisposable
//    {
//        private readonly string memberName;

//        internal ProviderMethodLogger(string memberName)
//        {
//            this.memberName = memberName;
//        }

//        public void Debug(WriteDebug(String.Format(format, args));

//        public void Dispose() => $"{this.memberName}:StepOut";

//        public void Log(string message, params string[] p) => (message + string.Join(" ", p));
//    }
//}