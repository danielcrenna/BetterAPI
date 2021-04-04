// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Logging
{
    public sealed class LoggingEntry
    {
        // ReSharper disable once UnusedMember.Global (Serialization)
        public LoggingEntry() { }

        public LoggingEntry(Guid id, LoggingDeserializeContext context)
        {
            Id = id;
            LogLevel = (LogLevel) context.br.ReadByte();
            EventId = new EventId(context.br.ReadInt32(), context.br.ReadNullableString());
            Message = context.br.ReadNullableString();

            if (context.br.ReadBoolean()) Exception = new LoggingException(context);
        }

        public LoggingEntry(Guid id, Exception? exception)
        {
            Id = id;
            if (exception != default) Exception = new LoggingException(exception);
        }

        public Guid Id { get; set; }
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public string? Message { get; set; }
        public LoggingException? Exception { get; set; }

        public void Serialize(LoggingSerializeContext context)
        {
            context.bw.Write(Id);
            context.bw.Write((byte) LogLevel);
            context.bw.Write(EventId.Id);
            context.bw.WriteNullableString(EventId.Name);
            context.bw.WriteNullableString(Message);

            if (context.bw.WriteBoolean(Exception != default))
                Exception?.Serialize(context);
        }
    }
}