﻿using Microsoft.Win32.TaskScheduler;
using System;
using System.Reflection;
using PKISharp.WACS.Extensions;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Services
{
    internal class TaskSchedulerService 
    {
        private MainArguments _options;
        private readonly ISettingsService _settings;
        private readonly IInputService _input;
        private readonly ILogService _log;

        public TaskSchedulerService(
            ISettingsService settings, 
            IArgumentsService options,
            IInputService input, 
            ILogService log)
        {
            _options = options.MainArguments;
            _settings = settings;
            _input = input;
            _log = log;
        }
        private string TaskName(string clientName)
        {
            return $"{clientName} renew ({_options.GetBaseUri().CleanBaseUri()})";
        }

        private string Path
        {
            get
            {
                return Assembly.GetExecutingAssembly().Location;
            }
        }

        private string WorkingDirectory
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Path);
            }
        }

        private string ExecutingFile
        {
            get
            {
                return System.IO.Path.GetFileName(Path);
            }
        }

        private Task ExistingTask
        {
            get
            {
                using (var taskService = new TaskService())
                {
                    foreach (var clientName in _settings.ClientNames.Reverse())
                    {
                        var taskName = TaskName(clientName);
                        var existingTask = taskService.GetTask(taskName);
                        if (existingTask != null)
                        {
                            return existingTask;
                        }
                    }
                }
                return null;
            }
        }

        public bool ConfirmTaskScheduler()
        {
            var existingTask = ExistingTask;
            if (existingTask != null)
            {
                return IsHealthy(existingTask);
            }
            else
            {
                _log.Warning("Scheduled task not configured yet");
                return false;
            }
        }

        private bool IsHealthy(Task task)
        {
            var healthy = true;
            healthy = healthy && task.Definition.Actions.OfType<ExecAction>().Any(action => action.Path == Path && action.WorkingDirectory == WorkingDirectory);
            healthy = healthy && task.Enabled;
            if (healthy)
            {
                _log.Information("Scheduled task looks healthy");
                return true;
            }
            else
            {
                _log.Warning("Scheduled task exists but does not look healthy");
                return false;
            }
        }

        public void EnsureTaskScheduler(RunLevel runLevel)
        {
            string taskName;
            var existingTask = ExistingTask;
            if (existingTask != null)
            {
                taskName = existingTask.Name;
            }
            else
            {
                taskName = TaskName(_settings.ClientNames.First());
            }

            using (var taskService = new TaskService())
            {             
                if (existingTask != null)
                {
                    var healthy = IsHealthy(existingTask);
                    if (healthy && !runLevel.HasFlag(RunLevel.Advanced)) {
                        return;
                    }

                    if (!_input.PromptYesNo($"Do you want to replace the existing task?", false)) {
                        return;
                    }

                    _log.Information("Deleting existing task {taskName} from Windows Task Scheduler.", taskName);
                    taskService.RootFolder.DeleteTask(taskName, false);
                }

                var actionString = $"--{nameof(MainArguments.Renew).ToLowerInvariant()} --{nameof(MainArguments.BaseUri).ToLowerInvariant()} \"{_options.GetBaseUri()}\"";

                _log.Information("Adding Task Scheduler entry with the following settings", taskName);
                _log.Information("- Name {name}", taskName);
                _log.Information("- Path {action}", WorkingDirectory);
                _log.Information("- Command {exec} {action}", ExecutingFile, actionString);
                _log.Information("- Start at {start}", _settings.ScheduledTaskStartBoundary);
                if (_settings.ScheduledTaskRandomDelay.TotalMinutes > 0)
                {
                    _log.Information("- Random delay {delay}", _settings.ScheduledTaskRandomDelay);
                }
                _log.Information("- Time limit {limit}", _settings.ScheduledTaskExecutionTimeLimit);

                // Create a new task definition and assign properties
                var task = taskService.NewTask();
                task.RegistrationInfo.Description = "Check for renewal of ACME certificates.";

                var now = DateTime.Now;
                var runtime = new DateTime(now.Year, now.Month, now.Day, 
                    _settings.ScheduledTaskStartBoundary.Hours, 
                    _settings.ScheduledTaskStartBoundary.Minutes,
                    _settings.ScheduledTaskStartBoundary.Seconds);

                task.Triggers.Add(new DailyTrigger {
                    DaysInterval = 1,
                    StartBoundary = runtime,
                    RandomDelay = _settings.ScheduledTaskRandomDelay
                });
                task.Settings.ExecutionTimeLimit = _settings.ScheduledTaskExecutionTimeLimit;
                task.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                task.Settings.RunOnlyIfNetworkAvailable = true;
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Settings.StopIfGoingOnBatteries = false;
                task.Settings.StartWhenAvailable = true;

                // Create an action that will launch the app with the renew parameters whenever the trigger fires
                task.Actions.Add(new ExecAction(Path, actionString, WorkingDirectory));

                task.Principal.RunLevel = TaskRunLevel.Highest; 
                while (true)
                {
                    try
                    {
                        if (!_options.UseDefaultTaskUser &&
                            runLevel.HasFlag(RunLevel.Advanced) && 
                            _input.PromptYesNo($"Do you want to specify the user the task will run as?", false))
                        {
                            // Ask for the login and password to allow the task to run 
                            var username = _input.RequestString("Enter the username (Domain\\username)");
                            var password = _input.ReadPassword("Enter the user's password");
                            _log.Debug("Creating task to run as {username}", username);
                            taskService.RootFolder.RegisterTaskDefinition(
                                taskName,
                                task,
                                TaskCreation.Create,
                                username,
                                password,
                                TaskLogonType.Password);
                        }
                        else if (existingTask != null)
                        {
                            _log.Debug("Creating task to run with previously chosen credentials");
                            string password = null;
                            string username = null;
                            if (existingTask.Definition.Principal.LogonType == TaskLogonType.Password)
                            {
                                username = existingTask.Definition.Principal.UserId;
                                password = _input.ReadPassword($"Password for {username}");
                            }
                            task.Principal.UserId = existingTask.Definition.Principal.UserId;
                            task.Principal.LogonType = existingTask.Definition.Principal.LogonType;
                            taskService.RootFolder.RegisterTaskDefinition(
                                taskName,
                                task,
                                TaskCreation.CreateOrUpdate,
                                username,
                                password,
                                existingTask.Definition.Principal.LogonType);
                        }
                        else
                        {
                            _log.Debug("Creating task to run as system user");
                            task.Principal.UserId = "SYSTEM";
                            task.Principal.LogonType = TaskLogonType.ServiceAccount;
                            taskService.RootFolder.RegisterTaskDefinition(
                                taskName,
                                task,
                                TaskCreation.CreateOrUpdate,
                                null,
                                null,
                                TaskLogonType.ServiceAccount);
                        }
                        break;
                    }
                    catch (COMException cex)
                    {
                        if (cex.HResult == -2147023570)
                        {
                            _log.Warning("Invalid username/password, please try again");
                        }
                        else
                        {
                            _log.Error(cex, "Failed to create task");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Failed to create task");
                        break;
                    }
                }
            }
        }
    }
}
