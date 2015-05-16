// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime.Caching;
using Microsoft.Framework.Runtime.Common.DependencyInjection;
using Microsoft.Framework.Runtime.Compilation;
using Microsoft.Framework.Runtime.FileSystem;
using Microsoft.Framework.Runtime.Infrastructure;
using Microsoft.Framework.Runtime.Loader;
using NuGet;

namespace Microsoft.Framework.Runtime
{
    public class DefaultHost : IDisposable
    {
        private ApplicationHostContext _applicationHostContext;

        private IFileWatcher _watcher;
        private readonly string _projectDirectory;
        private readonly FrameworkName _targetFramework;
        private readonly ApplicationShutdown _shutdown = new ApplicationShutdown();

        private Project _project;

        public DefaultHost(DefaultHostOptions options,
                           IServiceProvider hostServices)
        {
            _projectDirectory = Path.GetFullPath(options.ApplicationBaseDirectory);
            _targetFramework = options.TargetFramework;

            Initialize(options, hostServices);
        }

        public IServiceProvider ServiceProvider
        {
            get { return _applicationHostContext.ServiceProvider; }
        }

        public Project Project
        {
            get { return _project; }
        }

        public Assembly GetEntryPoint(string applicationName)
        {
            var sw = Stopwatch.StartNew();

            if (Project == null)
            {
                return null;
            }

            Initialize();

            var unresolvedLibs = _applicationHostContext.DependencyWalker.Libraries.Where(l => !l.Resolved);

            // If there's any unresolved dependencies then complain
            if (unresolvedLibs.Any())
            {
                string exceptionMsg;

                // If the main project cannot be resolved, it means the app doesn't support current target framework
                // (i.e. project.json doesn't contain a framework that is compatible with target framework of current runtime)
                if (unresolvedLibs.Any(l => string.Equals(l.Identity.Name, Project.Name)))
                {
                    var runtimeEnv = ServiceProvider.GetService(typeof(IRuntimeEnvironment)) as IRuntimeEnvironment;
                    var shortName = VersionUtility.GetShortFrameworkName(_targetFramework);
                    exceptionMsg = $@"The current runtime target framework is not compatible with '{Project.Name}'.

Current runtime Target Framework: '{_targetFramework} ({shortName})'
  Type: {runtimeEnv.RuntimeType}
  Architecture: {runtimeEnv.RuntimeArchitecture}
  Version: {runtimeEnv.RuntimeVersion}

Please make sure the runtime matches a framework specified in {Project.ProjectFileName}";
                }
                else
                {
                    exceptionMsg = _applicationHostContext.DependencyWalker.GetMissingDependenciesWarning(
                        _targetFramework);
                }

                throw new InvalidOperationException(exceptionMsg);
            }

            var accessor = (IAssemblyLoadContextAccessor)ServiceProvider.GetService(typeof(IAssemblyLoadContextAccessor));

            return accessor.Default.Load(applicationName);
        }

        public void Initialize()
        {
            AddRuntimeServiceBreadcrumb();

            _applicationHostContext.DependencyWalker.Walk(Project.Name, Project.Version, _targetFramework);

            Servicing.Breadcrumbs.Instance.WriteAllBreadcrumbs(background: true);
        }

        public IDisposable AddLoaders(IAssemblyLoaderContainer container)
        {
            var loaders = new[]
            {
                typeof(ProjectAssemblyLoader),
                typeof(NuGetAssemblyLoader),
            };

            var disposables = new List<IDisposable>();
            foreach (var loaderType in loaders)
            {
                var loader = (IAssemblyLoader)ActivatorUtilities.CreateInstance(ServiceProvider, loaderType);
                disposables.Add(container.AddLoader(loader));
            }

            return new DisposableAction(() =>
            {
                foreach (var d in Enumerable.Reverse(disposables))
                {
                    d.Dispose();
                }
            });
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }

        private void Initialize(DefaultHostOptions options, IServiceProvider hostServices)
        {
            var cacheContextAccessor = new CacheContextAccessor();
            var cache = new Cache(cacheContextAccessor);
            var namedCacheDependencyProvider = new NamedCacheDependencyProvider();
            var diagnostics = new List<ICompilationMessage>();

            _applicationHostContext = new ApplicationHostContext(
                hostServices,
                _projectDirectory,
                options.PackageDirectory,
                options.Configuration,
                _targetFramework,
                cache,
                cacheContextAccessor,
                namedCacheDependencyProvider,
                loadContextFactory: null,
                diagnostics: diagnostics);

            Logger.TraceInformation("[{0}]: Project path: {1}", GetType().Name, _projectDirectory);
            Logger.TraceInformation("[{0}]: Project root: {1}", GetType().Name, _applicationHostContext.RootDirectory);
            Logger.TraceInformation("[{0}]: Packages path: {1}", GetType().Name, _applicationHostContext.PackagesDirectory);

            if (diagnostics.Any())
            {
                throw new InvalidOperationException(diagnostics.First().FormattedMessage);
            }

            _project = _applicationHostContext.Project;

            if (Project == null)
            {
                throw new Exception("Unable to locate " + Project.ProjectFileName);
            }

            if (options.WatchFiles)
            {
                var watcher = new FileWatcher(_applicationHostContext.RootDirectory);
                _watcher = watcher;
                watcher.OnChanged += _ =>
                {
                    _shutdown.RequestShutdownWaitForDebugger();
                };
            }
            else
            {
                _watcher = NoopWatcher.Instance;
            }

            _applicationHostContext.AddService(typeof(IApplicationShutdown), _shutdown);
            _applicationHostContext.AddService(typeof(IRuntimeOptions), options);

            // TODO: Get rid of this and just use the IFileWatcher
            _applicationHostContext.AddService(typeof(IFileMonitor), _watcher);
            _applicationHostContext.AddService(typeof(IFileWatcher), _watcher);

            if (options.CompilationServerPort.HasValue)
            {
                // Change the project reference provider
                Project.DefaultCompiler = Project.DefaultDesignTimeCompiler;
            }

            CallContextServiceLocator.Locator.ServiceProvider = ServiceProvider;
        }

        private void AddRuntimeServiceBreadcrumb()
        {
#if DNX451
            var runtimeAssembly = typeof(Servicing.Breadcrumbs).Assembly;
#else
            var runtimeAssembly = typeof(Servicing.Breadcrumbs).GetTypeInfo().Assembly;
#endif

            var version = runtimeAssembly
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(version))
            {
                var semanticVersion = new NuGet.SemanticVersion(version);
                Servicing.Breadcrumbs.Instance.AddBreadcrumb(runtimeAssembly.GetName().Name, semanticVersion);
            }
        }

    }
}
