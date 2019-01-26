using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services.Description;

namespace RFIDForm
{
    public class CRUD
    {
        
        public static myDBHelp dbhelp = new myDBHelp(System.Configuration.ConfigurationManager.ConnectionStrings["mesConn"].ToString(), DbProviderType.Oracle);



        public ModuleObj QueryOracle(string lot)
        {
            string sql = new Jsonhelp().readjson("OracleSql", AppDomain.CurrentDomain.BaseDirectory + "config.json");
            sql = string.Format(sql, lot);

            var dataReader = dbhelp.ExecuteReader(sql, null);
            ModuleObj obj = new ModuleObj();
            if (dataReader.Read())
            {
                string moduletime = DateTime.Parse(dataReader["DTCR"].ToString()).ToString("yyyy-MM-dd");
                obj.ModuleDate = moduletime;
                obj.CellDate = DateTime.Parse(dataReader["CellDate"].ToString()).ToString("yyyy-MM-dd"); ;
                obj.Pmax = dataReader["PMAX"].ToString();
                obj.Voc = dataReader["RSVOC"].ToString();
                obj.Vmp = dataReader["RSVPM"].ToString();
                obj.Imp = dataReader["RSIPM"].ToString();
                obj.Isc = dataReader["RSISC"].ToString();
                obj.FF = dataReader["MODULE_EFF"].ToString();
                obj.ModuleID = lot;
                obj.ProductType = dataReader["NAME_PLATE"].ToString();
            }
            return obj;
        }


        /// <summary>
        /// 实例化WebServices
        /// </summary>
        /// <param name="url">WebServices地址</param>
        /// <param name="methodname">调用的方法</param>
        /// <param name="args">把webservices里需要的参数按顺序放到这个object[]里</param>
        public object InvokeWebService(string url, string methodname, object[] args)
        {

            //这里的namespace是需引用的webservices的命名空间，我没有改过，也可以使用。也可以加一个参数从外面传进来。
            string @namespace = "client";
            try
            {
                //获取WSDL
                WebClient wc = new WebClient();
                Stream stream = wc.OpenRead(url + "?WSDL");
                ServiceDescription sd = ServiceDescription.Read(stream);
                string classname = sd.Services[0].Name;
                ServiceDescriptionImporter sdi = new ServiceDescriptionImporter();
                sdi.AddServiceDescription(sd, "", "");
                CodeNamespace cn = new CodeNamespace(@namespace);

                //生成客户端代理类代码
                CodeCompileUnit ccu = new CodeCompileUnit();
                ccu.Namespaces.Add(cn);
                sdi.Import(cn, ccu);
                CSharpCodeProvider csc = new CSharpCodeProvider();
                //ICodeCompiler icc = csc.CreateCompiler();

                //设定编译参数
                CompilerParameters cplist = new CompilerParameters();
                cplist.GenerateExecutable = false;
                cplist.GenerateInMemory = true;
                cplist.ReferencedAssemblies.Add("System.dll");
                cplist.ReferencedAssemblies.Add("System.XML.dll");
                cplist.ReferencedAssemblies.Add("System.Web.Services.dll");
                cplist.ReferencedAssemblies.Add("System.Data.dll");

                //编译代理类
                CompilerResults cr = csc.CompileAssemblyFromDom(cplist, ccu);
                if (true == cr.Errors.HasErrors)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (System.CodeDom.Compiler.CompilerError ce in cr.Errors)
                    {
                        sb.Append(ce.ToString());
                        sb.Append(System.Environment.NewLine);
                    }
                    throw new Exception(sb.ToString());
                }

                //生成代理实例，并调用方法
                System.Reflection.Assembly assembly = cr.CompiledAssembly;
                Type t = assembly.GetType(@namespace + "." + classname, true, true);
                object obj = Activator.CreateInstance(t);
                System.Reflection.MethodInfo mi = t.GetMethod(methodname);

                return mi.Invoke(obj, args);
            }
            catch (Exception ex)
            {
                //throw new Exception();
                return null;
                throw new Exception();
            }

        }
    }




}
