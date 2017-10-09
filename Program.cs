using System;
using System.Collections.Generic;

namespace quantforce_api_sample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get token from environment or from command line
            string token = Environment.ExpandEnvironmentVariables("%quantforce-token%");
            string baseUrl = Environment.ExpandEnvironmentVariables("%quantforce-url%");
            // Prepare Rest class. The token will ba added to header each call
            common.Helpers.Rest rest = new common.Helpers.Rest(token);

            // Find all the node I have access to
            var nodes = rest.GetAsync<List<NodeView>>(baseUrl + "/api/v1/Node").Result;

        }
    }
}
