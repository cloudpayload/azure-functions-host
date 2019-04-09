﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Script.Rpc
{
    public class RpcInitializationService : IHostedService
    {
        private readonly IOptionsMonitor<ScriptApplicationHostOptions> _applicationHostOptions;
        private readonly IEnvironment _environment;
        private readonly ILanguageWorkerChannelManager _languageWorkerChannelManager;
        private readonly IRpcServer _rpcServer;
        private readonly ILogger _logger;
        private readonly string _workerRuntime;

        private Dictionary<string, List<string>> _osToLanguagesMapping = new Dictionary<string, List<string>>()
        {
            {
                LanguageWorkerConstants.WindowsOperatingSystemName,
                new List<string>() { LanguageWorkerConstants.JavaLanguageWorkerName }
            },
            {
                LanguageWorkerConstants.LinuxOperatingSystemName,
                new List<string>() { LanguageWorkerConstants.PythonLanguageWorkerName }
            }
        };

        public RpcInitializationService(IOptionsMonitor<ScriptApplicationHostOptions> applicationHostOptions, IEnvironment environment, IRpcServer rpcServer, ILanguageWorkerChannelManager languageWorkerChannelManager, ILoggerFactory loggerFactory)
        {
            _applicationHostOptions = applicationHostOptions ?? throw new ArgumentNullException(nameof(applicationHostOptions));
            _logger = loggerFactory.CreateLogger(ScriptConstants.LogCategoryRpcInitializationService);
            _rpcServer = rpcServer;
            _environment = environment;
            _languageWorkerChannelManager = languageWorkerChannelManager ?? throw new ArgumentNullException(nameof(languageWorkerChannelManager));
            _workerRuntime = _environment.GetEnvironmentVariable(LanguageWorkerConstants.FunctionWorkerRuntimeSettingName);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (Utility.CheckAppOffline(_applicationHostOptions.CurrentValue.ScriptPath))
            {
                return;
            }
            _logger.LogInformation("Starting Rpc Initialization Service.");
            await InitializeRpcServerAsync();
            await InitializeChannelsAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Shuttingdown Rpc Channels Manager");
            _languageWorkerChannelManager.ShutdownChannels();
            await _rpcServer.KillAsync();
        }

        internal async Task InitializeRpcServerAsync()
        {
            try
            {
                _logger.LogInformation("Initializing RpcServer");
                await _rpcServer.StartAsync();
            }
            catch (Exception grpcInitEx)
            {
                var hostInitEx = new HostInitializationException($"Failed to start Rpc Server. Check if your app is hitting connection limits.", grpcInitEx);
            }
        }

        internal Task InitializeChannelsAsync()
        {
            if (IsLinuxEnvironment())
            {
                InitializeChannelsAsync(LanguageWorkerConstants.LinuxOperatingSystemName, _workerRuntime);
            }
            else
            {
                InitializeChannelsAsync(LanguageWorkerConstants.WindowsOperatingSystemName, _workerRuntime);
            }

            return Task.CompletedTask;
        }

        private bool IsLinuxEnvironment()
        {
            return _environment.IsLinuxContainerEnvironment() || _environment.IsLinuxAppServiceEnvironment();
        }

        private Task InitializeChannelsAsync(string os, string workerRuntime)
        {
            if (StartInPlaceholderMode(workerRuntime))
            {
                return Task.WhenAll(_osToLanguagesMapping[os].Select(runtime =>
                    _languageWorkerChannelManager.InitializeChannelAsync(runtime)));
            }
            if (_osToLanguagesMapping[os].Contains(workerRuntime))
            {
                return _languageWorkerChannelManager.InitializeChannelAsync(workerRuntime);
            }
            return Task.CompletedTask;
        }

        private bool StartInPlaceholderMode(string workerRuntime)
        {
            return string.IsNullOrEmpty(workerRuntime) && _environment.IsPlaceholderModeEnabled();
        }

        // To help with unit tests
        internal void AddSupportedWindowsRuntime(string language) =>
            _osToLanguagesMapping[LanguageWorkerConstants.WindowsOperatingSystemName].Add(language);
    }
}
