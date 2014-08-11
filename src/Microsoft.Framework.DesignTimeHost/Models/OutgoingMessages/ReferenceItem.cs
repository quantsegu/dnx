// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.DesignTimeHost.Models.OutgoingMessages
{
    public class ReferenceItem
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as ReferenceItem;
            return other != null &&
                   Name.Equals(other.Name) &&
                   Version.Equals(other.Version);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}