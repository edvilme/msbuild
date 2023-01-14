// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace Microsoft.Build.Logging.FancyLogger
{
    public class FancyLogger : ILogger
    {   
        private Dictionary<int, FancyLoggerProjectNode> projects = new Dictionary<int, FancyLoggerProjectNode>();

        private bool Succeeded;

        private float existingTasks = 1;
        private float completedTasks = 0;

        public string Parameters {  get; set; }

        public LoggerVerbosity Verbosity { get; set; }

        public FancyLogger()
        {
            Parameters = "";
        }

        public void Initialize(IEventSource eventSource)
        {
            // Register for different events
            // Started
            eventSource.BuildStarted += new BuildStartedEventHandler(eventSource_BuildStarted);
            eventSource.ProjectStarted += new ProjectStartedEventHandler(eventSource_ProjectStarted);
            eventSource.TargetStarted += new TargetStartedEventHandler(eventSource_TargetStarted);
            eventSource.TaskStarted += new TaskStartedEventHandler(eventSource_TaskStarted);
            // Finished
            eventSource.BuildFinished += new BuildFinishedEventHandler(eventSource_BuildFinished);
            eventSource.ProjectFinished += new ProjectFinishedEventHandler(eventSource_ProjectFinished);
            eventSource.TargetFinished += new TargetFinishedEventHandler(eventSource_TargetFinished);
            // eventSource.TaskFinished += new TaskFinishedEventHandler(eventSource_TaskFinished);
            // Raised
            eventSource.MessageRaised += new BuildMessageEventHandler(eventSource_MessageRaised);
            eventSource.WarningRaised += new BuildWarningEventHandler(eventSource_WarningRaised);
            eventSource.ErrorRaised += new BuildErrorEventHandler(eventSource_ErrorRaised);
            // Initialize FancyLoggerBuffer
            FancyLoggerBuffer.Initialize();
            // TODO: Fix. First line does not appear at top. Leaving empty line for now
            FancyLoggerBuffer.WriteNewLine("");
            // Log all projects periodically
            Task.Run(() =>
            {
                /*while (true)
                {
                    await Task.Delay((1/60)*10);
                    foreach (var project in projects) project.Value.Log();
                }*/
            });
        }

        // Build
        void eventSource_BuildStarted(object sender, BuildStartedEventArgs e)
        {
        }

        void eventSource_BuildFinished(object sender, BuildFinishedEventArgs e)
        {
            Succeeded = e.Succeeded;
        }

        // Project
        void eventSource_ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            // Get project id
            int id = e.BuildEventContext!.ProjectInstanceId;
            // If id already exists...
            if (projects.ContainsKey(id)) return;
            // Add project
            FancyLoggerProjectNode node = new FancyLoggerProjectNode(e);
            projects[id] = node;
            // Log
            node.Log();
        }
        void eventSource_ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            // Get project id
            int id = e.BuildEventContext!.ProjectInstanceId;
            if (!projects.TryGetValue(id, out FancyLoggerProjectNode? node)) return;
            // Update line
            node.Finished = true;
            node.Log();
        }
        // Target
        void eventSource_TargetStarted(object sender, TargetStartedEventArgs e)
        {
            // Get project id
            int id = e.BuildEventContext!.ProjectInstanceId;
            if (!projects.TryGetValue(id, out FancyLoggerProjectNode? node)) return;
            // Update
            node.AddTarget(e);
        }
        void eventSource_TargetFinished(object sender, TargetFinishedEventArgs e)
        {
            // Get project id
            int id = e.BuildEventContext!.ProjectInstanceId;
            if (!projects.TryGetValue(id, out FancyLoggerProjectNode? node)) return;
            // Update
            node.FinishedTargets++;
        }

        // Task
        void eventSource_TaskStarted(object sender, TaskStartedEventArgs e)
        {
            // Get project id
            int id = e.BuildEventContext!.ProjectInstanceId;

            if (!projects.TryGetValue(id, out FancyLoggerProjectNode? node)) return;
            // Update
            node.AddTask(e);
            existingTasks++;
        }

        void eventSource_TaskFinished(object sender, TaskFinishedEventArgs e)
        {
            completedTasks++;
        }

        void eventSource_MessageRaised(object sender, BuildMessageEventArgs e)
        {
            // Get project id
            int id = e.BuildEventContext!.ProjectInstanceId;
            if (!projects.TryGetValue(id, out FancyLoggerProjectNode? node)) return;
            // Update
            node.AddMessage(e);
        }
        void eventSource_WarningRaised(object sender, BuildWarningEventArgs e)
        {
            // Get project id
            int id = e.BuildEventContext!.ProjectInstanceId;
            if (!projects.TryGetValue(id, out FancyLoggerProjectNode? node)) return;
            // Update
            node.AddWarning(e);
        }
        void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            // Get project id
            int id = e.BuildEventContext!.ProjectInstanceId;
            if (!projects.TryGetValue(id, out FancyLoggerProjectNode? node)) return;
            // Update
            node.AddError(e);
        }


        public void Shutdown()
        {
            FancyLoggerBuffer.Terminate();
            // TODO: Remove. There is a bug that causes switching to main buffer without deleting the contents of the alternate buffer
            Console.Clear();
            if (Succeeded)
            {
                Console.WriteLine(ANSIBuilder.Formatting.Color("Build succeeded.", ANSIBuilder.Formatting.ForegroundColor.Green));
                Console.WriteLine("\tX Warning(s)");
            }
            else
            {
                Console.WriteLine(ANSIBuilder.Formatting.Color("Build failed.", ANSIBuilder.Formatting.ForegroundColor.Red));
                Console.WriteLine("\tX Warnings(s)");
                Console.WriteLine("\tX Errors(s)");
            }
        }
    }
}
