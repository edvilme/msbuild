﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Microsoft.Build.Framework;

namespace Microsoft.Build.Logging.FancyLogger
{ 

    internal class FancyLoggerMessageNode
    {
        // Use this to change the max lenngth (relative to screen size) of messages
        private static int MAX_LENGTH = 3 * Console.BufferWidth;
        internal enum MessageType
        {
            HighPriorityMessage,
            Warning,
            Error
        }
        internal string Message;
        internal FancyLoggerBufferLine? Line;
        internal MessageType Type;
        internal string? Code;
        internal string? FilePath;
        internal int? LineNumber;
        internal int? ColumnNumber;
        public FancyLoggerMessageNode(LazyFormattedBuildEventArgs args)
        {
            Message = args.Message ?? string.Empty;
            if (Message.Length > MAX_LENGTH) Message = Message.Substring(0, MAX_LENGTH - 1) + "…";
            // Get type
            switch (args)
            {
                case BuildMessageEventArgs:
                    Type = MessageType.HighPriorityMessage;
                    break;
                case BuildWarningEventArgs warning:
                    Type = MessageType.Warning;
                    Code = warning.Code;
                    FilePath = warning.File;
                    LineNumber = warning.LineNumber;
                    ColumnNumber = warning.ColumnNumber;
                    break;
                case BuildErrorEventArgs error:
                    Type = MessageType.Error;
                    Code = error.Code;
                    FilePath = error.File;
                    LineNumber = error.LineNumber;
                    ColumnNumber = error.ColumnNumber;
                    break;
            }
        }

        internal string ToANSIString()
        {
            switch (Type)
            {
                case MessageType.Warning:
                    return $"⚠️ {ANSIBuilder.Formatting.Color(
                        $"Warning {Code}: {FilePath}({LineNumber},{ColumnNumber}) {Message}",
                        ANSIBuilder.Formatting.ForegroundColor.Yellow)}";
                case MessageType.Error:
                    return $"❌ {ANSIBuilder.Formatting.Color(
                        $"Error {Code}: {FilePath}({LineNumber},{ColumnNumber}) {Message}",
                        ANSIBuilder.Formatting.ForegroundColor.Red)}";
                case MessageType.HighPriorityMessage:
                default:
                    return $"ℹ️ {ANSIBuilder.Formatting.Italic(Message)}";
            }
        }

        internal void Log()
        {
            if (Line == null) return;
            FancyLoggerBuffer.UpdateLine(Line.Id, $"    └── {ToANSIString()}");
        }
    }
}
