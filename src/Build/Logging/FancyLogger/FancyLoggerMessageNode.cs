﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace Microsoft.Build.Logging.FancyLogger
{ 

    public class FancyLoggerMessageNode
    {
        public enum MessageType
        {
            HighPriorityMessage,
            Warning,
            Error
        }

        public string Message;
        public FancyLoggerBufferLine? Line;
        public MessageType Type;
        public FancyLoggerMessageNode(LazyFormattedBuildEventArgs args)
        {
            // Get type
            if (args is BuildMessageEventArgs) Type = MessageType.HighPriorityMessage;
            else if (args is BuildWarningEventArgs) Type = MessageType.Warning;
            else if (args is BuildErrorEventArgs) Type = MessageType.Error;

            // TODO: Replace
            if (args.Message == null)
            {
                Message = string.Empty;
            }
            else if (args.Message.Length > Console.WindowWidth - 1)
            {
                Message = args.Message.Substring(0, Console.WindowWidth - 1);
            }
            else
            {
                Message = args.Message;
            }
        }

        public void Log()
        {
            if (Line == null) return;
            // Get color
            ANSIBuilder.Formatting.ForegroundColor foregroundColor = ANSIBuilder.Formatting.ForegroundColor.Default;
            if (Type == MessageType.HighPriorityMessage) foregroundColor = ANSIBuilder.Formatting.ForegroundColor.Default;
            else if (Type == MessageType.Warning) foregroundColor = ANSIBuilder.Formatting.ForegroundColor.Yellow;
            else if (Type == MessageType.Error) foregroundColor = ANSIBuilder.Formatting.ForegroundColor.Red;

            FancyLoggerBuffer.UpdateLine(Line.Id, $"    └── {ANSIBuilder.Formatting.Color(ANSIBuilder.Formatting.Italic(Message), foregroundColor)}");
        }
    }
}
