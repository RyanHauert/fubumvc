﻿using System.Linq;
using FubuMVC.Core;
using FubuMVC.Core.Runtime.Aggregation;
using FubuTestingSupport;
using HtmlTags;
using NUnit.Framework;

namespace FubuMVC.IntegrationTesting.Aggregation
{
    [TestFixture]
    public class aggregator_integration_tester
    {
        [Test]
        public void aggregate_can_use_a_query()
        {
            TestHost.Scenario(_ =>
            {
                _.Get.Action<AggregationEndpoint>(x => x.get_aggregation());

                var json = _.Response.Body.ReadAsText();
                string[] values = JsonUtil.Get<string[]>(json);

                values.ShouldContain("Resource1: Jeremy Maclin");
            });
        }

        [Test]
        public void aggregate_by_resource_type()
        {
            TestHost.Scenario(_ =>
            {
                _.Get.Action<AggregationEndpoint>(x => x.get_aggregation());

                var json = _.Response.Body.ReadAsText();
                string[] values = JsonUtil.Get<string[]>(json);

                values.ShouldContain("I am Resource2");
            });
        }

        
        [Test]
        public void aggregate_selection_by_input_type()
        {
            TestHost.Scenario(_ =>
            {
                _.Get.Action<AggregationEndpoint>(x => x.get_aggregation());

                var json = _.Response.Body.ReadAsText();
                string[] values = JsonUtil.Get<string[]>(json);

                values.ShouldContain("Resource3 from input type 2");
            });
        }

        [Test]
        public void aggregate_selection_by_method()
        {
            TestHost.Scenario(_ =>
            {
                _.Get.Action<AggregationEndpoint>(x => x.get_aggregation());

                var json = _.Response.Body.ReadAsText();
                string[] values = JsonUtil.Get<string[]>(json);

                values.ShouldContain("Resource4 from an action identified by expression");
            });
        }


        [Test]
        public void can_still_call_a_data_action_independently()
        {
            TestHost.Scenario(_ =>
            {
                _.Get.Input(new AggregationEndpoint.Query1{Name = "Joe"});

                var json = _.Response.Body.ReadAsText();

                var resource = JsonUtil.Get<AggregationEndpoint.Resource1>(json);
                resource.Name.ShouldEqual("Joe");
            });
        }

        [Test]
        public void client_message_cache_knows_its_chains()
        {
            var cache = TestHost.Service<IClientMessageCache>();
            cache.AllClientMessages()
                .ShouldHaveTheSameElementsAs(
                    new ClientMessagePath { Message = "query-1", InputType = typeof(AggregationEndpoint.Query1), ResourceType = typeof(AggregationEndpoint.Resource1)},
                    new ClientMessagePath { Message = "resource-2", InputType = null, ResourceType = typeof(AggregationEndpoint.Resource2)},
                    new ClientMessagePath { Message = "input-2", InputType = typeof(AggregationEndpoint.Input2), ResourceType = typeof(AggregationEndpoint.Resource3)},
                    new ClientMessagePath { Message = "resource-4", InputType = null, ResourceType = typeof(AggregationEndpoint.Resource4)}
                );
        }

        [Test]
        public void find_chain_by_message_type()
        {
            var cache = TestHost.Service<IClientMessageCache>();
            cache.FindChain("query-1").InputType().ShouldEqual(typeof(AggregationEndpoint.Query1));
            cache.FindChain("resource-4").ResourceType().ShouldEqual(typeof (AggregationEndpoint.Resource4));
        }
    }

    public class AggregationEndpoint
    {
        private readonly IAggregator _aggregator;

        public AggregationEndpoint(IAggregator aggregator)
        {
            _aggregator = aggregator;
        }

        public Resource1 get_query1_Name(Query1 query)
        {
            return new Resource1 {Name = query.Name};
        }

        public Resource2 get_second_resource()
        {
            return new Resource2();
        }

        public Resource3 get_third_resource(Input2 input)
        {
            return new Resource3();
        }

        public Resource4 get_fourth_resource()
        {
            return new Resource4();
        }

        public string[] get_aggregation()
        {
            // The call to IAggregator.Fetch() will return an array
            // of objects. My assumption now is that you'd do this in
            // the main endpoint method, tack the data store data on
            // to the view model sent to the main spark view, and
            // render it to the view with a page helper there.
            // This is so common now that I think we put a json variable
            // helper into FubuMVC.Core. 
            return _aggregator.Fetch(_ =>
            {
                // By an input query
                _.Query(new Query1{Name = "Jeremy Maclin"});

                // By the resource type
                _.Resource<Resource2>();

                // By the input type as a marker
                _.Input<Input2>();

                // By action method
                _.Action<AggregationEndpoint>(x => x.get_fourth_resource());
            })
            .Select(x => x.ToString())
            .ToArray();
        }

        [ClientMessage]
        public class Input1
        {
            
        }

        [ClientMessage]
        public class Resource2
        {
            public override string ToString()
            {
                return "I am Resource2";
            }
        }

        [ClientMessage]
        public class Input2 { }

        [ClientMessage]
        public class Resource3
        {
            public override string ToString()
            {
                return "Resource3 from input type 2";
            }
        }

        [ClientMessage]
        public class Resource4
        {
            public override string ToString()
            {
                return "Resource4 from an action identified by expression";
            }
        }

        [ClientMessage]
        public class Query1
        {
            public string Name { get; set; }
        }

        [ClientMessage]
        public class Resource1
        {
            public string Name { get; set; }

            public override string ToString()
            {
                return "Resource1: " + Name;
            }
        }
    }


}