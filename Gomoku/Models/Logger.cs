using System.IO;
using System.Runtime.CompilerServices;

namespace Gomoku.Models
{
    public enum LogType
    {
        Info,
        Debug,
        Error,
        System
    }
    public static class Logger
    {
        public static event Action<string, LogType>? OnLogReceived;

        // 호출자 파일 이름, 메서드 이름, 줄번호

        public static void Info(
            string msg,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = -1)
            => WriteLog(msg, LogType.Info, file, member, line);
        public static void Debug(
            string msg,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = -1)
            => WriteLog(msg, LogType.Debug, file, member, line);
        public static void Error(
            string msg,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = -1)
            => WriteLog(msg, LogType.Error, file, member, line);
        public static void System(
            string msg, 
            [CallerFilePath] string file = "", 
            [CallerMemberName] string member = "", 
            [CallerLineNumber] int line = -1)
            => WriteLog(msg, LogType.System, file, member, line);

        private static void WriteLog(
            string msg,
            LogType type,
            string file,
            string member,
            int line)
        {
            string classname = Path.GetFileNameWithoutExtension(file);
            string format = $"[{classname}.{member}:{line}] {msg}";
            OnLogReceived?.Invoke(format, type);
        }
    }
}
