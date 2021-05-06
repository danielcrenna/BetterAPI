// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.SourceGenerators
{
    public interface IStringBuilder
    {
        int Indent { get; set; }
        IStringBuilder OpenNamespace(string @namespace);
        IStringBuilder CloseNamespace();

        int Length { get; set; }
        IStringBuilder AppendLine(string message);
        IStringBuilder AppendLine();
        IStringBuilder Clear();
        IStringBuilder Insert(int index, object value);
        IStringBuilder Append(string value);
    }
}