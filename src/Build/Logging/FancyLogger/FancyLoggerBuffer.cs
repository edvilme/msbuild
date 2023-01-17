﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// using System.Threading.Tasks;

namespace Microsoft.Build.Logging.FancyLogger
{
    public class FancyLoggerBufferLine
    {
        private static int Counter = 0;
        private string _text = string.Empty;
        public List<string> WrappedText { get; private set; } = new();
        public int Id;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                WrappedText = ANSIBuilder.ANSIWrap(value, Console.BufferWidth);
            }
        }

        public FancyLoggerBufferLine()
        {
            Id = Counter++;
            Text = string.Empty;
        }
        public FancyLoggerBufferLine(string text)
            : this()
        {
            Text = text;
        }
    }

    public class FancyLoggerBuffer
    {
        private static List<FancyLoggerBufferLine> Lines = new();
        public static int TopLineIndex = 0;
        public static string Footer = string.Empty;
        private static bool IsTerminated = false;
        // private static bool AutoScrollEnabled = true;
        public static void Initialize()
        {
            // Use encoding
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            // Use alternate buffer
            // TODO: Remove. Tries to solve a bug when switching from and to the alternate buffer
            Console.Write(ANSIBuilder.Buffer.UseMainBuffer());
            Console.Write(ANSIBuilder.Buffer.UseAlternateBuffer());

            Task.Run(() =>
            {
                // Use encoding
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                while (!IsTerminated)
                {
                    Task.Delay((1 / 60) * 1_000).ContinueWith((t) =>
                    {
                        Render();
                    });
                    if (Console.KeyAvailable)
                    {
                        ConsoleKey key = Console.ReadKey().Key;
                        if (key == ConsoleKey.UpArrow && TopLineIndex > 0) TopLineIndex--;
                        else if (key == ConsoleKey.DownArrow) TopLineIndex++;
                    }
                }
            });
        }

        public static void Terminate()
        {
            IsTerminated = true;
            // TODO: Remove. Tries to solve a bug when switching from and to the alternate buffer
            Console.Write(ANSIBuilder.Buffer.UseMainBuffer());
            Console.Write(ANSIBuilder.Eraser.Display());
            Lines = new();
        }

        #region Rendering
        public static void Render()
        {
            // Write Header
            Console.Write(
                // Write header
                ANSIBuilder.Cursor.Home() +
                ANSIBuilder.Eraser.LineCursorToEnd() + ANSIBuilder.Formatting.Inverse(ANSIBuilder.Alignment.Center("MSBuild - Build in progress")) +
                // Write footer
                ANSIBuilder.Eraser.LineCursorToEnd() + ANSIBuilder.Cursor.Position(Console.BufferHeight - 1, 0) +
                // TODO: Remove and replace with actual footer
                new string('-', Console.BufferWidth) +$"\nBuild progress: XX%\tTopLineIndex={TopLineIndex}"
            );
            if (Lines.Count == 0) return;
            // Iterate over lines and display on terminal
            // TODO: Delimit range to improve performance 
            int accumulatedLineCount = 0;
            foreach (FancyLoggerBufferLine line in Lines)
            {
                foreach (string s in line.WrappedText) {
                    // Get line index relative to scroll area
                    int lineIndex = accumulatedLineCount - TopLineIndex;
                    // Print if line in scrolling area
                    if (lineIndex >= 0 && lineIndex < Console.BufferHeight - 3)
                    {
                        Console.Write(ANSIBuilder.Cursor.Position(lineIndex + 2, 0) + ANSIBuilder.Eraser.LineCursorToEnd() + s);
                    }
                    accumulatedLineCount++;
                }
            }
        }
        #endregion
        #region Line identification
        public static int GetLineIndexById(int lineId)
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                if (Lines[i].Id == lineId) return i;
            }
            return -1;
        }

        public static FancyLoggerBufferLine? GetLineById(int lineId)
        {
            int index = GetLineIndexById(lineId);
            if (index == -1) return null;
            return Lines[index];
        }
        #endregion

        #region Line create, update and delete
        // Write new line
        public static FancyLoggerBufferLine? WriteNewLineAfter(int lineId, string text)
        {
            FancyLoggerBufferLine line = new FancyLoggerBufferLine(text);
            return WriteNewLineAfter(lineId, line);
        }
        public static FancyLoggerBufferLine? WriteNewLineAfter(int lineId, FancyLoggerBufferLine line, bool overrideOverflowLines = false)
        {
            // Save top line (current if no lines)
            // int topLineId = Lines.Count > 0 ? Lines[TopLineIndex].Id : line.Id;
            if (lineId != -1)
            {
                // Get line index
                int lineIndex = GetLineIndexById(lineId);
                if (lineIndex == -1) return null;
                // Get line end index
                Lines.Insert(lineIndex, line);
            }
            else
            {
                Lines.Add(line);
            }
            // Get updated top line index
            // TopLineIndex = GetLineIndexById(topLineId);
            // Return
            return line;
        }

        public static FancyLoggerBufferLine? WriteNewLine(string text)
        {
            FancyLoggerBufferLine line = new FancyLoggerBufferLine(text);
            return WriteNewLine(line);
        }
        public static FancyLoggerBufferLine? WriteNewLine(FancyLoggerBufferLine line)
        {
            return WriteNewLineAfter(Lines.Count > 0 ? Lines.Last().Id : -1, line);
        }

        // Update line
        public static FancyLoggerBufferLine? UpdateLine(int lineId, string text)
        {
            // Get line
            FancyLoggerBufferLine? line = GetLineById(lineId);
            if (line == null) return null;
            line.Text = text;
            // Return
            return line;
        }

        // Delete line
        public static void DeleteLine(int lineId)
        {
            // TODO: What if line id is equal to topLineId?????
            // Get line index
            int lineIndex = GetLineIndexById(lineId);
            if (lineIndex == -1) return;
            // Save top line
            int topLineId = Lines[TopLineIndex].Id;
            // Delete
            Lines.RemoveAt(lineIndex);
            // Get updated top line index
            if (topLineId != lineId)
            {
                // TopLineIndex = GetLineIndexById(topLineId);
            }
        }
        #endregion
    }
}
