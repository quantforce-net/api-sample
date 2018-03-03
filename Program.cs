using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace quantforce
{
    class Program
    {
        static async Task<int> WaitForTaskToCompleteAsync(dynamic task, common.Helpers.Rest rest, string uri)
        {
            int status = 0;
            while (status < 400)
            {
                await Task.Delay(500);
                dynamic taskResult = await rest.GetAsync<JToken>(uri + "/task/" + task.taskId);
                status = taskResult.status;
            }
            return status;
        }

        static public string GetHexa(byte[] hash)
        {
            string sb = "";
            foreach (byte h in hash)
                sb += h.ToString("x2");
            return sb;
        }

        static void CreateAccount(string nodeUrl, common.Helpers.Rest rest)
        {
        }

        static void Main(string[] args)
        {
            // Get token from environment or from command line
            string token = Environment.ExpandEnvironmentVariables("%quantforce-token%");
            string nodeUrl = Environment.ExpandEnvironmentVariables("%quantforce-url%");
            string version = "/api/v1";
            // Prepare Rest class. The token will ba added to header each call
            common.Helpers.Rest rest = new common.Helpers.Rest(token);

            NodeView account = new NodeView();
            account.publicJson = new JObject();
            account.publicJson["email"] = "test@test.com";
            account.publicJson["MD5password"] = "111"; // tbd
            account.publicJson["companyName"] = "acme";
            account.publicJson["lastName"] = "tom";
            account.publicJson["firstName"] = "Ber";
            account.publicJson["sex"] = "M";
            account.publicJson["phone"] = "+33123456789";

            var acc = rest.PostAsync<NodeView>(nodeUrl + version + "/account", account).Result;

            // Retreive project node type
            var types = rest.GetAsync<List<NodeType>>(nodeUrl + version + "/helper/types").Result;

            // Find all the node I have access to
            var nodes = rest.GetAsync<List<NodeView>>(nodeUrl + version + "/node").Result;
            string projectName = "QQ Project";

            if (nodes.Count > 0)
            {
                // Use the root node and see if the project exists
                var projects = rest.GetAsync<List<NodeView>>(nodeUrl + version + "/node/" + nodes[0].id + "/childs?type=" + types.Find(a => a.name == "Project").type.ToString()).Result;
                // Does my test project exists?
                var project = projects.Find(a => a.name == projectName);
                if (project == null) // Create the project
                    project = rest.PostAsync<NodeView>(nodeUrl + version + "/project/create?parentId=" + nodes[0].id + "&subType=1&name=" + Uri.EscapeDataString(projectName), new { comment = "My comment " }).Result;

                // Get the full node to get processURI
                NodeView fullNode = rest.GetAsync<NodeView>(nodeUrl + version + "/node/" + project.id).Result;
                string dataURI = ((dynamic)fullNode.inheritedJson).dataURI;
                string processURI = ((dynamic)fullNode.inheritedJson).processURI;

                // Upload the data.
                // 1) Create the file and get data serveur URI
                string fileName = "qq_model_creation.csv";
                dynamic file = rest.PostAsync<NodeView>(nodeUrl + version + "/file?name=" + Uri.EscapeDataString(fileName) + "&parentId=" + project.id).Result;

                // 2) Upload the file in chunk of 1 MB
                System.IO.FileInfo fi = new System.IO.FileInfo(fileName);
                using (var stream = System.IO.File.OpenRead(fileName))
                {
                    byte[] data = new byte[1024 * 1024];
                    long size = fi.Length;
                    int chunk = 1;
                    while (size > 0)
                    {
                        int read = stream.Read(data, 0, 1024 * 1024);
                        var md5 = GetHexa(System.Security.Cryptography.MD5.Create().ComputeHash(data, 0, read));
                        size -= read;
                        dynamic tmp = rest.PostFileAsync<JToken>(dataURI + version + "/file/" + file.id + "/" + chunk, data, 0, read).Result;
                        chunk++;
                        Console.WriteLine("Chunk {0} uploaded, MD5 = {1} :{2}", tmp.chunk, tmp.md5, tmp.md5==md5);
                    }
                }

                // 3) Attach the file to the node
                dynamic task = rest.GetAsync<JToken>(dataURI + version + "/file/" + file.id + "/process").Result;
                int status = WaitForTaskToCompleteAsync(task, rest, dataURI + version).Result;
                if (status == 400)
                {
                    // 5) Execute QQ
                    ActionView av = new ActionView()
                    {
                        name = "TMPROCESS",
                        verb = "",
                        parameters = JObject.FromObject(new { DeliquencyDays = 90, DataFile = file.id })
                    };
                    dynamic process = rest.PostAsync<JToken>(processURI + version + "/process/" + project.id + "/action", av).Result;
                    status = WaitForTaskToCompleteAsync(process, rest, processURI + version);
                    // Get the result json
                    Process_Out result = rest.GetAsync<Process_Out>(processURI + version + "/task/" + process.taskId + "/result").Result;
                    Console.WriteLine(result.result.ToString());
                }
            }
        }
    }
}
