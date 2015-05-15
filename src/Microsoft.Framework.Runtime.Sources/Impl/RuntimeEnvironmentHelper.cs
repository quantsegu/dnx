// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Runtime
{
    internal static class RuntimeEnvironmentHelper
    {
        private static Lazy<bool> _isMono = new Lazy<bool>(() => ((IRuntimeEnvironment)Services.Value.GetService(typeof(IRuntimeEnvironment))).RuntimeType == "Mono");
        private static Lazy<bool> _isWindows = new Lazy<bool>(() => ((IRuntimeEnvironment)Services.Value.GetService(typeof(IRuntimeEnvironment))).OperatingSystem == "Windows");

        private static Lazy<IServiceProvider> Services = new Lazy<IServiceProvider>(() => Infrastructure.CallContextServiceLocator.Locator.ServiceProvider);
        //private static bool? _isWindows = null;
        //private static bool? _isMono = null;

        public static bool IsWindows
        {
            get
            {
                //if(_isWindows == null)
                {
                    if(Services.Value == null)
                    {
                        Console.WriteLine("ServiceProvider is null");
                        throw new ArgumentNullException("ServiceProvider");
                    }

                    if(Services.Value.GetService(typeof(IRuntimeEnvironment)) == null)
                    {
                        Console.WriteLine("IRuntimeEnvironment is null");
                        throw new ArgumentNullException("IRuntimeEnvironment");
                    }

                    //_isWindows = ((IRuntimeEnvironment)Services.Value.GetService(typeof(IRuntimeEnvironment))).OperatingSystem == "Windows";
                }

                return _isWindows.Value;
            }
        }

        public static bool IsMono
        {
            get
            {
                //if(_isMono == null)
                {
                    //_isMono = ((IRuntimeEnvironment)Services.Value.GetService(typeof(IRuntimeEnvironment))).RuntimeType == "Mono";
                }

                if (Services.Value == null)
                {
                    Console.WriteLine("ServiceProvider is null");
                    throw new ArgumentNullException("ServiceProvider");
                }

                if (Services.Value.GetService(typeof(IRuntimeEnvironment)) == null)
                {
                    Console.WriteLine("IRuntimeEnvironment is null");
                    throw new ArgumentNullException("IRuntimeEnvironment");
                }

                return _isMono.Value;
            }
        }
    }
}
