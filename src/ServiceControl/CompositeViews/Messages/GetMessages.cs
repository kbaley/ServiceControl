namespace ServiceControl.CompositeViews.Messages
{
    using System.Collections.Generic;
    using Infrastructure.Extensions;
    using Raven.Client;
    using Raven.Client.Linq;
    using ServiceBus.Management.Infrastructure.Nancy.Modules;

    public class GetMessages : BaseModule
    {
        public GetMessages() 
        {
            Get["/messages", true] = async (parameters, token) =>
            {
                IList<MessagesView> results;
                RavenQueryStatistics stats;
                using (var session = Store.OpenAsyncSession())
                {
                    results = await session.Query<MessagesViewIndex.SortAndFilterOptions, MessagesViewIndex>()
                        .IncludeSystemMessagesWhere(Request)
                        .Statistics(out stats)
                        .Sort(Request)
                        .Paging(Request)
                        .TransformWith<MessagesViewTransformer, MessagesView>()
                        .ToListAsync()
                        .ConfigureAwait(false);
                }

                return await this.CombineWithRemoteResults(results, stats.TotalResults, stats.IndexEtag, stats.IndexTimestamp).ConfigureAwait(false);
            };


            Get["/endpoints/{name}/messages", true] = async (parameters, token) =>
            {
                IList<MessagesView> results;
                RavenQueryStatistics stats;
                using (var session = Store.OpenAsyncSession())
                {
                    string endpoint = parameters.name;

                    results = await session.Query<MessagesViewIndex.SortAndFilterOptions, MessagesViewIndex>()
                        .IncludeSystemMessagesWhere(Request)
                        .Where(m => m.ReceivingEndpointName == endpoint)
                        .Statistics(out stats)
                        .Sort(Request)
                        .Paging(Request)
                        .TransformWith<MessagesViewTransformer, MessagesView>()
                        .ToListAsync()
                        .ConfigureAwait(false);
                }

                return await this.CombineWithRemoteResults(results, stats.TotalResults, stats.IndexEtag, stats.IndexTimestamp).ConfigureAwait(false);
            };
        }
    }
}