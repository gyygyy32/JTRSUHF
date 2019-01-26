using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFIDForm
{
    public class Jsonhelp
    {
        public string readjson(string tagname, string filepath)
        {
            //AppDomain.CurrentDomain.BaseDirectory + "config.json"
            using (StreamReader r = new StreamReader(filepath))
            {
                var json = r.ReadToEnd();
                var jobj = JObject.Parse(json);
                //string value = jobj.Properties().Select(item=> item.Name="CSVFilePath")
                return jobj[tagname].ToString();
            }
        }

        public string writejson(string tagname, string value, string filepath)
        {
            string res = string.Empty;
            using (StreamReader r = new StreamReader(filepath))
            {
                var json = r.ReadToEnd();
                var jobj = JObject.Parse(json);
                //string value = jobj.Properties().Select(item=> item.Name="CSVFilePath")
                jobj[tagname] = value;
                res = jobj.ToString();
            }
            File.WriteAllText(filepath, res);
            return "success";
        }

    }
}
