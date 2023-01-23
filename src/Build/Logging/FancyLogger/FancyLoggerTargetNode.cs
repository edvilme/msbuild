// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Microsoft.Build.Framework;

namespace Microsoft.Build.Logging.FancyLogger
{ 

    internal class FancyLoggerTargetNode
    {
        internal int Id;
        internal string TargetName;
        internal string CurrentTaskName;
        internal FancyLoggerTargetNode(TargetStartedEventArgs args)
        {
            Id = args.BuildEventContext!.TargetId;
            TargetName = args.TargetName;
            CurrentTaskName = string.Empty;
        }
        internal void AddTask(TaskStartedEventArgs args)
        {
            CurrentTaskName = args.TaskName;
        }
    }
}
