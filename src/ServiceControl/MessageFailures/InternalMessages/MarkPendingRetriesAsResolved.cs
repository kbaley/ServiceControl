﻿namespace ServiceControl.MessageFailures.InternalMessages
{
    using System;
    using NServiceBus;

    public class MarkPendingRetriesAsResolved : ICommand
    {
        public string QueueAddress { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
    }
}