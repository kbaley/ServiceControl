namespace ServiceControl.Recoverability
{
    using System.Collections.Generic;
    using NServiceBus;
    using NServiceBus.Logging;

    internal class CorruptedReplyToHeaderStrategy
    {
        private string localMachineName;

        public CorruptedReplyToHeaderStrategy(string localMachineName)
        {
            this.localMachineName = localMachineName;
        }

        public void FixCorruptedReplyToHeader(IDictionary<string, string> headers)
        {
            string replyToAddress;
            if (!headers.TryGetValue(Headers.ReplyToAddress, out replyToAddress))
            {
                return;
            }

            string originatingMachine;
            if (!headers.TryGetValue(Headers.OriginatingMachine, out originatingMachine))
            {
                return;
            }

            var split = replyToAddress.Split('@');
            if (split.Length != 2)
            {
                return;
            }

            var queueName = split[0];
            var machineName = split[1];

            if (machineName == localMachineName && machineName != originatingMachine)
            {
                var fixedReplyToAddress = $"{queueName}@{originatingMachine}";
                log.Info($"Detected corrupted ReplyToAddress `{replyToAddress}`. Correcting to `{fixedReplyToAddress}`.");
                headers["ServiceControl.OldReplyToAddress"] = replyToAddress;
                headers[Headers.ReplyToAddress] = fixedReplyToAddress;
            }
        }

        private static ILog log = LogManager.GetLogger(typeof(RetryProcessor));
    }
}