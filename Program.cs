using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.IO;

namespace quantforce
{
    class Program
    {
        static string GetFileMD5(string fileName)
        {
            string md5 = "";
            using (var targetStream = System.IO.File.Open(fileName, FileMode.Open))
                md5 = GetHexa(System.Security.Cryptography.MD5.Create().ComputeHash(targetStream));
            return md5;
        }

        static public async Task Downloadfile(string token, string nodeUri, string fileId, string fileName)
        {
            // Find the dataURI
            // dataURI is in the node
            //{
            //    "v" : "1.0",
            //    "dataURI" : "http://data.m4f.eu:8080"
            //}

            var rest = new common.Helpers.Rest(token);
            NodeView node = rest.GetAsync<NodeView>(nodeUri +"/node/" + fileId).Result;
            string dataUri = null;
            if (node != null && node.inheritedJson != null)
                dataUri = node.inheritedJson.Value<string>("dataURI");
            if (dataUri != null)
            {
                dynamic task = await rest.GetAsync<JObject>(dataUri + "/api/v1/file/" + fileId + "/checkout");
                while (task.taskId > 0 && task.state < 400)
                {
                    await Task.Delay(1000);
                    task = await rest.GetAsync<JObject>(dataUri + "/api/v1/task/" + task.taskId);
                }
                if (task.state == 400)
                {
                    JToken taskResult = await rest.GetAsync<JToken>(dataUri + "/api/v1/task/" + task.taskId + "/result");
                    DataCheckout_Out co = taskResult["result"]["DataCheckout_Out"].ToObject<DataCheckout_Out>();
                    try
                    {
                        using (System.IO.FileStream streamOut = System.IO.File.Create(fileName))
                        {
                            foreach (DataOut _do in co.dataOut)
                            {
                                var chunk = await rest.SendAsync(System.Net.Http.HttpMethod.Get, dataUri + "/api/v1/file/" + fileId + "/chunk/" + _do.chunkId);
                                chunk.EnsureSuccessStatusCode();
                                var streamIn = await chunk.Content.ReadAsStreamAsync();
                                await streamIn.CopyToAsync(streamOut);
                            }
                        }
                        // Check the MD5
                        if (co.MD5_Final != GetFileMD5(fileName))
                            throw new Exception("MD5 is not correct");
                    }
                    catch (Exception ex)
                    {
                        System.IO.File.Delete(fileName);
                        throw ex;
                    }
                }
                await rest.GetAsync<JObject>(dataUri + "/api/v1/file/" + fileId + "/finished");
            }
        }

        static async Task<int> WaitForTaskToCompleteAsync(dynamic task, common.Helpers.Rest rest, string uri)
        {
            int state = 0;
            while (state < 400)
            {
                await Task.Delay(500);
                dynamic taskResult = await rest.GetAsync<JToken>(uri + "/task/" + task.taskId);
                state = taskResult.state;
            }
            return state;
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
            account.publicJson["MD5password"] = "111"; // You only send password MD5, never send the real password
            account.publicJson["companyName"] = "acme";
            account.publicJson["lastName"] = "tom";
            account.publicJson["firstName"] = "Ber";
            account.publicJson["sex"] = "M";
            account.publicJson["phone"] = "+33123456789";
            account.publicJson["extrainfo"] = "Anything";

            // Does this user exist?
            JObject existingUser = rest.PostAsync<JObject>(nodeUrl + version + "/account/find", account).Result;
            if (existingUser["tokens"] != null)
            {
                Console.WriteLine(existingUser);
                /*
{{
  "user": {
    "email": "test@test.com",
    "MD5password": "111",
    "companyName": "acme",
    "lastName": "tom",
    "firstName": "Ber",
    "sex": "M",
    "phone": "+33123456789"
  },
  "tokens": [
    "91435f7d-297c-4bb6-887a-xx"
  ]
}}
                 */
                dynamic acc = existingUser;
                token = acc.tokens[0];
            }
            else
            {
                // Create this user
                // To update this user use the same method. Because you have special token that is allow to do it
                dynamic acc = rest.PostAsync<JObject>(nodeUrl + version + "/account", account).Result;
                Console.WriteLine(acc);
                /*
{{
  "user": {
    "email": "test@test.com",
    "MD5password": "111",
    "companyName": "acme",
    "lastName": "tom",
    "firstName": "Ber",
    "sex": "M",
    "phone": "+33123456789"
  },
  "tokens": [
    "91435f7d-297c-4bb6-887a-xx"
  ]
}}
                 */
                token = acc.tokens[0];
            }
            rest = new common.Helpers.Rest(token);

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


                //// 5) Execute QQ
                //ActionView av = new ActionView()
                //{
                //    name = "TMPROCESS",
                //    verb = "",
                //    parameters = JObject.FromObject(new { DeliquencyDays = 90, DataFile = 0 })
                //};
                //dynamic process = rest.PostAsync<JToken>(processURI + version + "/process/" + project.id + "/action", av).Result;
                //WaitForTaskToCompleteAsync(process, rest, processURI + version).Wait();
                //// Get the result json
                //Process_Out result = rest.GetAsync<Process_Out>(processURI + version + "/task/" + process.taskId + "/result").Result;
                //Console.WriteLine(result.result.ToString());
                //// Find the files Excel et Json child from project
                //var files = rest.GetAsync<List<NodeView>>(nodeUrl + version + "/file/" + project.id).Result;
                //foreach (NodeView nv in files)
                //{
                //    if (nv.name == "results.xlsx")
                //        Downloadfile(token, nodeUrl + version, nv.id, "results.xlsx").Wait();
                //    if (nv.name == "results.json")
                //        Downloadfile(token, nodeUrl + version, nv.id, "results.json").Wait();
                //}


                // Upload the data.
                // 1) Create the file and get data serveur URI
                string fileName = "qq_model_creation.csv";
                dynamic file = rest.PostAsync<NodeView>(nodeUrl + version + "/file?name=" + Uri.EscapeDataString(fileName) + "&parentId=" + project.id).Result;

                // 2) Upload the file in chunk of 1 MB
                int chunk = 1;
                System.IO.FileInfo fi = new System.IO.FileInfo(fileName);
                using (var stream = System.IO.File.OpenRead(fileName))
                {
                    byte[] data = new byte[1024 * 1024];
                    long size = fi.Length;
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
                dynamic task = rest.GetAsync<JToken>(dataURI + version + "/file/" + file.id + "/process?chunkCount=" + (chunk-1).ToString()).Result;
                int state = WaitForTaskToCompleteAsync(task, rest, dataURI + version).Result;
                if (state == 400)
                {
                    // 5) Execute QQ
                    ActionView av2 = new ActionView()
                    {
                        name = "TMPROCESS",
                        verb = "",
                        parameters = JObject.FromObject(new { DeliquencyDays = 90, DataFile = file.id })
                    };
                    dynamic process2 = rest.PostAsync<JToken>(processURI + version + "/process/" + project.id + "/action", av2).Result;
                    state = WaitForTaskToCompleteAsync(process2, rest, processURI + version).Result;
                    // Get the result json
                    JToken result2 = rest.GetAsync<JToken>(processURI + version + "/task/" + process2.taskId + "/result").Result;
                    Console.WriteLine(result2);
                    // Find the files Excel et Json child from project
                    var files2 = rest.GetAsync<List<NodeView>>(nodeUrl + version + "/file?parentId=" + project.id).Result;
                    var p = files2.Find(a => a.name == "project");
                    if (p!=null)
                    {
                        var files3 = rest.GetAsync<List<NodeView>>(nodeUrl + version + "/file?parentId=" + p.id).Result;
                        foreach (NodeView nv in files3)
                        {
                            if (nv.name == "results.xlsx")
                                Downloadfile(token, nodeUrl + version, nv.id, "results.xlsx").Wait();
                            if (nv.name == "results.json")
                                Downloadfile(token, nodeUrl + version, nv.id, "results.json").Wait();
                        }
                    }
                }
            }
        }
    }
}
