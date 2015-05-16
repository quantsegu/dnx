// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Runtime
{
    public class LockFileValidationMessage : ICompilationMessage
    {
        public LockFileValidationMessage(string message, string lockFilePath)
        {
            Message = message;
            SourceFilePath = lockFilePath;
        }

        public string FormattedMessage
        {
            get
            {
                return $"{SourceFilePath}: {Severity.ToString().ToLower()}: {Message}. Please run \"dnu restore\" to generate a new lock file.";
            }
        }

        public string Message { get; }

        public string SourceFilePath { get; }

        public CompilationMessageSeverity Severity { get; } = CompilationMessageSeverity.Error;

        public int StartLine { get; set; }

        public int StartColumn { get; set; }

        public int EndLine { get; set; }

        public int EndColumn { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as LockFileValidationMessage;

            return other != null &&
                Message.Equals(other.Message, StringComparison.Ordinal) &&
                SourceFilePath.Equals(other.SourceFilePath, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
