﻿namespace ServiceControl.Contracts.MessageRedirects
{
    using System;
    using NServiceBus;

    public class MessageRedirectCreated : IEvent
    {
        public Guid MessageRedirectId { get; set; }
        public string FromPhysicalAddress { get; set; }
        public string ToPhysicalAddress { get; set; }
    }
}
