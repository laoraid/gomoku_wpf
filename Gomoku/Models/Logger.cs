using System;
using System.Collections.Generic;
using System.Text;

namespace Gomoku.Models
{
    public enum LogType
    {
        Info, // 로그 O, 화면 X
        Debug, // 로그 O, 화면 X
        Error, // 로그 O, 화면 O
        System // 로그 X, 화면 O
    }
    public static class Logger
    {
        public static event Action<string, LogType>? OnLogReceived;

        public static void Info(string msg) => OnLogReceived?.Invoke(msg, LogType.Info);
        public static void Debug(string msg) => OnLogReceived?.Invoke(msg, LogType.Debug);
        public static void Error(string msg) => OnLogReceived?.Invoke(msg, LogType.Error);
        public static void System(string msg) => OnLogReceived?.Invoke(msg, LogType.System);
    }
}
