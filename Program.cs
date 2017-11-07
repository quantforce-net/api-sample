using System;
using System.Collections.Generic;

namespace quantforce
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get token from environment or from command line
            string token = Environment.ExpandEnvironmentVariables("%quantforce-token%");
            string baseUrl = Environment.ExpandEnvironmentVariables("%quantforce-url%");
            string version = "/api/v1";
            // Prepare Rest class. The token will ba added to header each call
            common.Helpers.Rest rest = new common.Helpers.Rest(token);

            // Retreive project node type
            var types = rest.GetAsync<List<NodeType>>(baseUrl + version + "/helper/types").Result;

            // Find all the node I have access to
            var nodes = rest.GetAsync<List<NodeView>>(baseUrl + version + "/node").Result;

            if (nodes.Count > 0)
            {
                // Use the root node and see if the project exists
                var projects = rest.GetAsync<List<NodeView>>(baseUrl + version + "/node/" + nodes[0].id + "/childs?type=" + types.Find(a => a.name == "Project").type.ToString()).Result;
                // Does my test project exists?
                var project = projects.Find(a => a.name == "Test project");
                if (project==null) // Create the project
                {
                    // The header is
                    // Nummer,Factuurnummer,Factuurdatum,Vervaldatum,Bedrag,Bedrag_open,Bedrag_betaald,Datum_betaald
                    project = rest.PostAsync<NodeView>(baseUrl + version + "/project/createQQ?parentId=" + nodes[0].id, new QQ()
                    {
                        projectName = "Test project",
                        ClientIdColumnName = "Nummer",
                        InvoiceIdColumnName = "Factuurnummer",
                        InvoiceDateColumnName = "Factuurdatum",
                        InvoiceTermColumnName = "Vervaldatum",
                        InvoiceAmountColumnName = "Bedrag",
                        InvoicePaymentColumnName = "Datum_betaald"
                    }).Result;

                    // Upload the data
                    // 1) Create the node and get data serveur URI
                    dynamic file = rest.PostAsync<NodeView>(baseUrl + version + "/file?name=" + System.Net.WebUtility.UrlEncode("My file") + "&parentId=" + project.id).Result;
                    string dataURI = file.dataURI;

                    // 2) Upload the file in chunk of 1 MB
                    string fileName = "qq_model_creation.csv";
                    System.IO.FileInfo fi = new System.IO.FileInfo(fileName);
                    using (var stream = System.IO.File.OpenRead(fileName))
                    {
                        byte[] data = new byte[1024 * 1024];
                        long size = fi.Length;
                        int chunk = 1;
                        while (size>0)
                        {
                            int read = stream.Read(data, 0, 1024 * 1024);
                            size -= read;
                            dynamic tmp = rest.PostFileAsync<Newtonsoft.Json.Linq.JToken>(dataURI + version + "/" + file.id + "/" + chunk, data, 0, read);
                            chunk++;
                            Console.WriteLine("Chunk {0} uploaded, MD5 = {1}", tmp.chunk, tmp.MD5);
                        }
                    }

                    // 3) Attach the file to the node
                    dynamic task = rest.PostAsync<Newtonsoft.Json.Linq.JToken>(dataURI + version + "/" + file.id + "/process");

                    // 4) Wait for the file integration
                    int status = 0;
                    while (status<400)
                    {
                        System.Threading.Thread.Sleep(1000);
                        dynamic taskResult = rest.GetAsync<Newtonsoft.Json.Linq.JToken>(dataURI + "/" + task.taskId);
                        status = taskResult.status;
                    }

                    if (status==400)
                    {

                    }
                }
            }

        }
    }
}
