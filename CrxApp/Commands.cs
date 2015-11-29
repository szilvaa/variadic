using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

[assembly: CommandClass(typeof(CrxApp.Commands))]
[assembly: ExtensionApplication(null)]

namespace CrxApp
{
    class DbAccess
    {
        public class Product
        {
            public string ProductName { get; set; }
        }
        [DllImport("accore.dll", EntryPoint = "?acesHttpOperation@@YA?AW4ErrorStatus@Acad@@PEB_W0000@Z", CharSet =CharSet.Unicode)]
        extern static int HttpOperation(string name, string suffix, string headers, string requestContentOrFile, string responseFile);
        public string GetProductName(string id)
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //make a call to the URL in the ProductDb input argument, the result of the call will be stored in response.json
            var response = "response.json";
            if (0 == HttpOperation("ProductDb", string.Format("Products({0})?", id), "","", string.Format("file://{0}",response)))
            {
                //read response text and extract the ProductName
                var product = JsonConvert.DeserializeObject<Product>(File.ReadAllText(response));
                return product.ProductName;
            }
            return "Failed";
        }
        static void list(Autodesk.AutoCAD.EditorInput.Editor ed, string folder)
        {
            var en = Directory.EnumerateFileSystemEntries(folder);
            foreach (var f in en)
            {
                ed.WriteMessage(string.Format("{0}\n",f));
            }
        }
    }
    
    public class Commands
    {
        [CommandMethod("MyTestCommands", "test", CommandFlags.Modal)]
        static public void Test()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var dba = new DbAccess();
            try
            {
                //iterate over modelspace and replace change attributes
                //on block references from ProductID to ProductName
                dynamic db = doc.Database;
                var ms = db.BlockTableId[BlockTableRecord.ModelSpace];
                foreach (dynamic e in ms)
                {
                    if (e.IsKindOf(typeof(BlockReference)))
                    {
                        var bref = e;
                        var productIds = from a in (IEnumerable<dynamic>)bref.AttributeCollection
                                    where a.Tag == "PRODUCTID"
                                    select a;

                        foreach (var p in productIds)
                        {
                            p.Tag = "PRODUCTNAME";
                            p.TextString = dba.GetProductName(p.TextString);
                        }
                    }
                }
                //finally export to pdf
                ed.Command("-export", "_pdf", "_extents", "_no", "result.pdf");
            }
            catch (System.Exception e)
            {
                ed.WriteMessage("Error: {0}", e);
            }
        }
    }
}
