// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Microsoft.Build.Logging.FancyLogger
{ 
    internal class FancyLoggerProjectNode
    {
        /// <summary>
        /// Given a list of paths, this method will get the shortest not ambiguous path for a project.
        /// Example: for `/users/documents/foo/project.csproj` and `/users/documents/bar/project.csproj`, the respective non ambiguous paths would be `foo/project.csproj` and `bar/project.csproj`
        /// Still work in progress...
        /// </summary>
        private static string GetUnambiguousPath(string path)
        {
            return Path.GetFileName(path);
        }

        internal int Id;
        internal string ProjectPath;
        internal string TargetFramework;
        internal bool Finished;
        // Line to display project info
        internal FancyLoggerBufferLine? Line;
        // Targets
        internal int FinishedTargets;
        internal FancyLoggerBufferLine? CurrentTargetLine;
        internal FancyLoggerTargetNode? CurrentTargetNode;
        // Messages, errors and warnings
        internal List<FancyLoggerMessageNode> AdditionalDetails = new();
        // Count messages, warnings and errors
        internal int MessageCount = 0;
        internal int WarningCount = 0;
        internal int ErrorCount = 0;
        internal FancyLoggerProjectNode(ProjectStartedEventArgs args)
        {
            Id = args.ProjectId;
            ProjectPath = args.ProjectFile!;
            Finished = false;
            FinishedTargets = 0;
            if (args.GlobalProperties != null && args.GlobalProperties.ContainsKey("TargetFramework"))
            {
                TargetFramework = args.GlobalProperties["TargetFramework"];
            }
            else
            {
                TargetFramework = "";
            }
        }

        internal void Log()
        {
            // Project details
            string lineContents = ANSIBuilder.Alignment.SpaceBetween(
                // Show indicator
                (Finished ? ANSIBuilder.Formatting.Color("✓", ANSIBuilder.Formatting.ForegroundColor.Green) : ANSIBuilder.Formatting.Blinking(ANSIBuilder.Graphics.Spinner())) +
                // Project
                ANSIBuilder.Formatting.Dim("Project: ") +
                // Project file path with color
                $"{ANSIBuilder.Formatting.Color(ANSIBuilder.Formatting.Bold(GetUnambiguousPath(ProjectPath)), Finished ? ANSIBuilder.Formatting.ForegroundColor.Green : ANSIBuilder.Formatting.ForegroundColor.Default )} [{TargetFramework ?? "*"}]",
                $"({MessageCount} Messages, {WarningCount} Warnings, {ErrorCount} Errors)",
                Console.WindowWidth
            );
            // Create or update line
            if (Line == null) Line = FancyLoggerBuffer.WriteNewLine(lineContents, false);
            else FancyLoggerBuffer.UpdateLine(Line.Id, lineContents);

            // For finished projects
            if (Finished)
            {
                if (CurrentTargetLine != null) FancyLoggerBuffer.DeleteLine(CurrentTargetLine.Id);
                foreach (FancyLoggerMessageNode node in AdditionalDetails.ToList())
                {
                    // Only delete high priority messages
                    if (node.Type != FancyLoggerMessageNode.MessageType.HighPriorityMessage) continue;
                    if (node.Line != null) FancyLoggerBuffer.DeleteLine(node.Line.Id);
                    // AdditionalDetails.Remove(node);
                }
            }

            // Current target details
            if (CurrentTargetNode == null) return;
            string currentTargetLineContents = $"    └── {CurrentTargetNode.TargetName} : {CurrentTargetNode.CurrentTaskName}";
            if (CurrentTargetLine == null) CurrentTargetLine = FancyLoggerBuffer.WriteNewLineAfter(Line!.Id, currentTargetLineContents);
            else FancyLoggerBuffer.UpdateLine(CurrentTargetLine.Id, currentTargetLineContents);

            // Messages, warnings and errors
            foreach (FancyLoggerMessageNode node in AdditionalDetails)
            {
                if (Finished && node.Type == FancyLoggerMessageNode.MessageType.HighPriorityMessage) continue;
                if (node.Line == null) node.Line = FancyLoggerBuffer.WriteNewLineAfter(Line!.Id, "Message");
                node.Log();
            }
        }

        internal FancyLoggerTargetNode AddTarget(TargetStartedEventArgs args)
        {
            CurrentTargetNode = new FancyLoggerTargetNode(args);
            return CurrentTargetNode;
        }
        internal void AddTask(TaskStartedEventArgs args)
        {
            // Get target id
            int targetId = args.BuildEventContext!.TargetId;
            if (CurrentTargetNode?.Id == targetId) CurrentTargetNode.AddTask(args);
        }
        internal FancyLoggerMessageNode? AddMessage(BuildMessageEventArgs args)
        {
            if (args.Importance != MessageImportance.High) return null;
            MessageCount++;
            FancyLoggerMessageNode node = new FancyLoggerMessageNode(args);
            AdditionalDetails.Add(node);
            return node;
        }
        internal FancyLoggerMessageNode? AddWarning(BuildWarningEventArgs args)
        {
            WarningCount++;
            FancyLoggerMessageNode node = new FancyLoggerMessageNode(args);
            AdditionalDetails.Add(node);
            return node;
        }
        internal FancyLoggerMessageNode? AddError(BuildErrorEventArgs args)
        {
            ErrorCount++;
            FancyLoggerMessageNode node = new FancyLoggerMessageNode(args);
            AdditionalDetails.Add(node);
            return node;
        }
    }
}
