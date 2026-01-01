using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Gomoku
{
    public enum RequestType
    {
        Move, Chat
    }
    public abstract class  GameData
    {
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }

    public class PositionData : GameData
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class ChatData : GameData
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ResponseData : GameData
    {
        public bool Accepted { get; set; }
        public string Message { get; set; } = string.Empty;

        public RequestType TargetRequest { get; set; }
    }
}
