using System;
using System.Collections.Generic;
using System.Text;

namespace Gomoku.Messages
{
    public class DialogMessage(string title, string message)
    {
        public string Title { get; } = title; public string Message { get; } = message;

        public bool? Result { get; set; }
    }
}
