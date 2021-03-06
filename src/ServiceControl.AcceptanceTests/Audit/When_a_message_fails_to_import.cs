﻿namespace ServiceBus.Management.AcceptanceTests.Audit
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTests;
    using NUnit.Framework;
    using ServiceControl.CompositeViews.Messages;
    using ServiceControl.Operations;

    class When_a_message_fails_to_import : AcceptanceTest
    {
        [Test]
        public async Task It_can_be_reimported()
        {
            //Make sure the audit import attempt fails
            CustomConfiguration = config => { config.RegisterComponents(c => c.ConfigureComponent<FailOnceEnricher>(DependencyLifecycle.SingleInstance)); };

            FailedAuditsCountReponse failedAuditsCountReponse;

            await Define<MyContext>()
                .WithEndpoint<Sender>(b => b.When((bus, c) => bus.Send(new MyMessage())))
                .WithEndpoint<Receiver>()
                .Done(async c =>
                {
                    if (c.MessageId == null)
                    {
                        return false;
                    }

                    if (!c.WasReimported)
                    {
                        var result = await this.TryGet<FailedAuditsCountReponse>("/api/failedaudits/count");
                        failedAuditsCountReponse = result;
                        if (result)
                        {
                            if (failedAuditsCountReponse.Count > 0)
                            {
                                c.FailedImport = true;
                                await this.Post<object>("/api/failedaudits/import", null, code =>
                                {
                                    if (code == HttpStatusCode.OK)
                                    {
                                        c.WasReimported = true;
                                        return false;
                                    }

                                    return true;
                                });
                            }
                        }

                        return false;
                    }

                    return await this.TryGetMany<MessagesView>($"/api/messages/search/{c.MessageId}");
                })
                .Run(TimeSpan.FromSeconds(40));
        }

        class FailOnceEnricher : ImportEnricher
        {
            public MyContext Context { get; set; }

            public override Task Enrich(IReadOnlyDictionary<string, string> headers, IDictionary<string, object> metadata)
            {
                if (!Context.FailedImport)
                {
                    TestContext.WriteLine("Simulating message processing failure");
                    throw new MessageDeserializationException("ID", null);
                }

                TestContext.WriteLine("Message processed correctly");
                return Task.FromResult(0);
            }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServerWithoutAudit>(c =>
                {
                    var routing = c.ConfigureTransport().Routing();
                    routing.RouteToEndpoint(typeof(MyMessage), typeof(Receiver));
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServerWithAudit>();
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyContext Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Context.MessageId = context.MessageId;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessage : ICommand
        {
        }

        public class MyContext : ScenarioContext
        {
            public bool FailedImport { get; set; }
            public bool WasReimported { get; set; }
            public string MessageId { get; set; }
        }
    }
}