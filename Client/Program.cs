using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

using AIO.Operations;
using AIO.ACES.Models;

namespace Client
{
    class Credentials
    {
        //get your ConsumerKey/ConsumerSecret at http://developer.autodesk.com
        public static string ConsumerKey = "P2O7u6AuSFvRhCCRNBGx0aAvMrjc1s61";
        public static string ConsumerSecret = "pO0jR9isyazVM4Od";
    }
    class Program
    {
        static readonly string PackageName = "MyTestPackage";
        static readonly string ActivityName = "MyTestActivity";

        static Container container;
        static void Main(string[] args)
        {
            //instruct client side library to insert token as Authorization value into each request
            container = new Container(new Uri("https://developer.api.autodesk.com/autocad.io/us-east/v2/"));
            var token = GetToken();
            container.SendingRequest2 += (sender, e) => e.RequestMessage.SetHeader("Authorization", token);

            //check if our app package exists
            AppPackage package = null;
            try { package = container.AppPackages.ByKey(PackageName).GetValue(); } catch { }
            string res = "New";
            if (package!=null)
            {
                // We have multiple versions of the is package already, retrieve all versions and show the current
                Console.WriteLine("AppPackage '{0}' already exists. The following versions are available:", PackageName);
                int min = int.MaxValue;
                int max = int.MinValue;

                var cont = new Container(new Uri("https://developer.api.autodesk.com/autocad.io/us-east/v2/"));
                cont.SendingRequest2 += (sender, e) => e.RequestMessage.SetHeader("Authorization", token);

                foreach (var app in cont.AppPackages.ByKey(PackageName).GetVersions())
                {
                    if (app.Version > max)
                        max = app.Version;
                    if (app.Version < min)
                        min = app.Version;
                    Console.WriteLine("Version #: {0}.Time Submitted: {1}.", app.Version, app.Timestamp);
                }
                Console.WriteLine("Current={0}", package.Version);
                res = Prompts.PromptForKeyword("What do you want to do? [New/SetCurrent/Leave]<New>");
                if (res=="SetCurrent")
                {
                    var ver = Prompts.PromptForNumber("Choose a version", min, max);
                    container.AppPackages.ByKey(PackageName).SetVersion(ver).Execute();
                }
            }
            if (res=="New")
                package = CreateOrUpdatePackage(CreateZip(), package);
            
            //check if our activity already exists
            Activity activity = null;
            try { activity = container.Activities.ByKey(ActivityName).GetValue(); }
            catch { }
            res = "New";
            if (activity != null)
            {
                Console.WriteLine("Activity '{0}' already exists. The following versions are available:", ActivityName);
                int min = int.MaxValue;
                int max = int.MinValue;

                var cont = new Container(new Uri("https://developer.api.autodesk.com/autocad.io/us-east/v2/"));
                cont.SendingRequest2 += (sender, e) => e.RequestMessage.SetHeader("Authorization", token);

                foreach (var act in cont.Activities.ByKey(ActivityName).GetVersions())
                {
                    if (act.Version > max)
                        max = act.Version;
                    if (act.Version < min)
                        min = act.Version;
                    Console.WriteLine("Version #: {0}.Time Submitted: {1}. Script={2}.", act.Version, act.Timestamp, act.Instruction.Script.Replace("\n","<enter>"));
                }
                Console.WriteLine("Current={0}", activity.Version);
                res = Prompts.PromptForKeyword("What do you want to do? [New/SetCurrent/Leave]<New>");
                if (res == "SetCurrent")
                {
                    var ver = Prompts.PromptForNumber("Choose a version", min, max);
                    container.Activities.ByKey(ActivityName).SetVersion(ver).Execute();
                }
            }
            if (res == "New")
                activity = CreateOrUpdateActivity(activity, PackageName);

            //save outstanding changes if any
            container.SaveChanges();

            //finally submit workitem against our activity
            SubmitWorkItem(activity);
        }
        static string GetToken()
        {
            Console.WriteLine("Getting authorization token...");
            using (var client = new HttpClient())
            {
                var values = new List<KeyValuePair<string, string>>();
                values.Add(new KeyValuePair<string, string>("client_id", Credentials.ConsumerKey));
                values.Add(new KeyValuePair<string, string>("client_secret", Credentials.ConsumerSecret));
                values.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
                var requestContent = new FormUrlEncodedContent(values);
                var response = client.PostAsync("https://developer.api.autodesk.com/authentication/v1/authenticate", requestContent).Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;
                var resValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
                return resValues["token_type"] + " " + resValues["access_token"];
            }
        }
        static string CreateZip()
        {
            Console.WriteLine("Generating autoloader zip...");
            string zip = "package.zip";
            if (File.Exists(zip))
                File.Delete(zip);
            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                string bundle = PackageName + ".bundle";
                string name = "PackageContents.xml";
                archive.CreateEntryFromFile(name, Path.Combine(bundle, name));
                name = "CrxApp.dll";
                archive.CreateEntryFromFile(name, Path.Combine(bundle, "Contents", name));
                name = "Newtonsoft.Json.dll";
                archive.CreateEntryFromFile(name, Path.Combine(bundle, "Contents", name));
            }
            return zip;

        }

        static AppPackage CreateOrUpdatePackage(string zip, AppPackage package)
        {
            Console.WriteLine("Creating/Updating AppPackage...");
            // First step -- query for the url to upload the AppPackage file
            var url = container.AppPackages.GetUploadUrl().GetValue();

            // Second step -- upload AppPackage file
            UploadObject(url, zip);

            if (package == null)
            {
                // third step -- after upload, create the AppPackage entity
                package = new AppPackage()
                {
                    Id = PackageName,
                    RequiredEngineVersion = "20.1",
                    Resource = url
                };
                container.AddToAppPackages(package);
            }
            else
            {
                //or update the existing one with the new url
                package.Resource = url;
                container.UpdateObject(package);
            }
            container.SaveChanges();
            return package;
        }

        static void UploadObject(string url, string filePath)
        {
            Console.WriteLine("Uploading autoloader zip...");
            var client = new HttpClient();
            client.PutAsync(url, new StreamContent(File.OpenRead(filePath))).Result.EnsureSuccessStatusCode();
        }


        static Activity CreateOrUpdateActivity(Activity activity, string packageName)
        {
            Console.WriteLine("Creating/Updating Activity...");
            bool newlyCreated = false;
            if (activity == null)
            {
                activity = new Activity() { Id = ActivityName };
                newlyCreated = true;
            }

            activity.Instruction = new Instruction()
            {
                Script = "_test\n"
            };
            activity.Parameters = new Parameters()
            {
                InputParameters = {
                        new Parameter() { Name = "HostDwg", LocalFileName = "$(HostDwg)" },
                        new Parameter() { Name = "ProductDb", LocalFileName="dummy.txt"}
                    },
                OutputParameters = { new Parameter() { Name = "Result", LocalFileName = "result.pdf" } }
            };
            activity.RequiredEngineVersion = "20.1";
            if (newlyCreated)
            {
                activity.AppPackages.Add(packageName); // reference the custom AppPackage
                container.AddToActivities(activity);
            }
            else
                container.UpdateObject(activity);
            container.SaveChanges();
            return activity;
        }
        
        static void SubmitWorkItem(Activity activity)
        {
            Console.WriteLine("Submitting workitem...");
            //create a workitem
            var wi = new WorkItem()
            {
                Id = "", //must be set to empty
                Arguments = new Arguments(),
                ActivityId = activity.Id 
            };

            wi.Arguments.InputArguments.Add(new Argument()
            {
                Name = "HostDwg",// Must match the input parameter in activity
                Resource = "https://albert-sandbox.s3-us-west-2.amazonaws.com/jars.dwg",
                StorageProvider = StorageProvider.Generic //Generic HTTP download (as opposed to A360)
            });
            wi.Arguments.InputArguments.Add(new Argument()
            {
                Name = "ProductDb",// Must match the input parameter in activity
                Resource = "http://services.odata.org/V4/Northwind/Northwind.svc/",
                StorageProvider = StorageProvider.Generic,
                ResourceKind=ResourceKind.Variadic,
                HttpVerb=HttpVerbType.GET
            });
            wi.Arguments.OutputArguments.Add(new Argument()
            {
                Name = "Result", //must match the output parameter in activity
                StorageProvider = StorageProvider.Generic, //Generic HTTP upload (as opposed to A360)
                HttpVerb = HttpVerbType.POST, //use HTTP POST when delivering result
                Resource = null, //use storage provided by AutoCAD.IO
            });

            container.AddToWorkItems(wi);
            container.SaveChanges();

            //polling loop
            do
            {
                Console.WriteLine("Sleeping for 2 sec...");
                System.Threading.Thread.Sleep(2000);
                container.LoadProperty(wi, "Status"); //http request is made here
                Console.WriteLine("WorkItem status: {0}", wi.Status);
            }
            while (wi.Status == ExecutionStatus.Pending || wi.Status == ExecutionStatus.InProgress);

            //re-query the service so that we can look at the details provided by the service
            container.MergeOption = Microsoft.OData.Client.MergeOption.OverwriteChanges;
            wi = container.WorkItems.ByKey(wi.Id).GetValue();

            //download the status report
            var url = wi.StatusDetails.Report;
            DownloadToDocs(url, @"AIO-report.txt");
            
            //Resource property of the output argument "Results" will have the output url
            url = wi.Arguments.OutputArguments.First(a => a.Name == "Result").Resource;
            DownloadToDocs(url, @"AIO.pdf");

        }

        static void DownloadToDocs(string url, string localFile)
        {
            var client = new HttpClient();
            var content = (StreamContent)client.GetAsync(url).Result.Content;
            var fname = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), localFile);
            Console.WriteLine("Downloading to {0}.", fname);
            using (var output = System.IO.File.Create(fname))
            {
                content.ReadAsStreamAsync().Result.CopyTo(output);
                output.Close();
            }
        }

    }
}
