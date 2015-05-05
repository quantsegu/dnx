// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Runtime
{
    internal static class RuntimeEnvironmentHelper
    {
        public static bool IsWindows(IServiceProvider services)
        {
            return IsWindows(((IRuntimeEnvironment)services.GetService(typeof(IRuntimeEnvironment))));
        }

        public static bool IsWindows(IRuntimeEnvironment runtimeEnv)
        {
            return runtimeEnv.OperatingSystem == "Windows";
        }

        public static bool IsMono(IServiceProvider services)
        {
            return IsMono((IRuntimeEnvironment)services.GetService(typeof(IRuntimeEnvironment)));
        }

        public static bool IsMono(IRuntimeEnvironment runtimeEnv)
        {
            return runtimeEnv.RuntimeType == "Mono";
        }
    }
}
