
using CefSharp;
using CefSharp.WinForms;
using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services.Description;
using System.Windows.Forms;

namespace RFIDForm
{
    public partial class Form1 : Form
    {
        //static DbUtility dbhelp = new DbUtility(System.Configuration.ConfigurationManager.ConnectionStrings["cloudConn"].ToString(), DbProviderType.MySql);
        private System.Reflection.Assembly assemblyobj = null;
        public static myDBHelp orahelp = new myDBHelp(System.Configuration.ConfigurationManager.ConnectionStrings["mesConn"].ToString(), DbProviderType.Oracle);
        private ChromiumWebBrowser myBrowser;
        private RFIDInterface objRFID = null;
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            if (CheckLicense() == false) { MessageBox.Show("请升级软件"); return; }
            assemblyobj = CreateWc("http://10.60.3.27/PMSService/DataService.asmx");
            //初始化读取描述
            //获取间隔时间
            List<string> TimeList = new List<string>();
            string[] aryTime = new Jsonhelp().readjson("ReadTime", BasicConfigInfo.path).ToString().Split('|');
            foreach (var a in aryTime)
            {
                TimeList.Add(a);
            }
            ddlinternalTime.DataSource = TimeList;

            //初始化cefsharp
            try
            {
                DbUtility dbhelp = new DbUtility(System.Configuration.ConfigurationManager.ConnectionStrings["cloudConn"].ToString(), DbProviderType.MySql);
                CefSettings seting = new CefSettings();
                //seting.CefCommandLineArgs.Add("disable-gpu", "1");
                //Cef.EnableHighDPISupport();
                Cef.Initialize(seting);
                String page = string.Format(@"{0}\IVCurve\IVCurve.html", Application.StartupPath);
                //ChromiumWebBrowser myBrowser = new ChromiumWebBrowser(page);
                myBrowser = new ChromiumWebBrowser(page);
                //this.Controls.Add(myBrowser);
                myBrowser.Dock = DockStyle.Fill;
                gbxCurve.Controls.Add(myBrowser);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            //panel1.Controls.Add(myBrowser);
            objRFID = new UHFISO18000();
            string res = "";
            objRFID.Open(ref res);
            Log(res,1);
        }
        private bool CheckLicense()
        {
            object ob = orahelp.ExecuteScalar("select sysdate as time from dual ", null);
            if (ob != null)
            {
                if (Convert.ToDateTime(ob.ToString()) > Convert.ToDateTime("2019-3-4"))
                {
                    return false;
                }
            }
            return true;
        }

        private void ClearText()
        {
            txtCellDate.Text = "";
            txtCertificationProvider.Text = "";
            txtIECDate.Text = "";
            txtISO9001.Text = "";
            txtLot.Text = "";
            txtModuleCellSupplier.Text = "";
            txtModuleCountry.Text = "";
            txtModuleDate.Text = "";

            txtPmax.Text = "";
            txtVmp.Text = "";
            txtVoc.Text = "";
            txtVoc.Text = "";
            txtFF.Text = "";
            txtIsc.Text = "";
            txtProductType.Text = "";
        }

        private void Log(string info,int color)
        {

            //string txtinfo = System.DateTime.Now.ToString() + " " + info;
            
            int nLen =txtLog.TextLength;

            if (nLen != 0)
            {
                txtLog.AppendText(Environment.NewLine + System.DateTime.Now.ToString() + " " + info);
            }
            else
            {
                txtLog.AppendText(System.DateTime.Now.ToString() + " " + info);
            }
            txtLog.Select(nLen, txtLog.TextLength - nLen);
            txtLog.SelectionColor = color==1?System.Drawing.Color.SeaGreen:Color.Tomato;

            //txtLog.Select(txtLog.TextLength, 0);
            //txtLog.ScrollToCaret();



            //txtLog.Select(len, txtLog.TextLength - len);

            //txtLog.Text =info+ "\r\n"+txtLog.Text;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cef.Shutdown();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            myBrowser.ExecuteScriptAsync("LoadChart(9.78,9.30,33.44,40.46,311.23)");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            myBrowser.ShowDevTools();
        }
        // add by xue lei on 2019-2-1
        private bool CheckModuleInfo(ModuleObj obj)
        {
            if (obj.ModuleDate == string.Empty 
                || obj.ProductType==string.Empty
                || obj.FF==string.Empty
                || obj.Imp==string.Empty
                || obj.Isc==string.Empty
                || obj.Pmax==string.Empty
                || obj.Vmp==string.Empty
                || obj.Voc==string.Empty
                )
            {
                return false;
            }
            return true;
        }

        private void txtLot_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {

                    //query module
                    ModuleObj objModule = null;
                    if (!cbxIsTest.Checked)
                    {
                        objModule = QueryModuleIndo(txtLot.Text);
                    }
                    else
                    {
                        objModule = new ModuleObj { ModuleID = txtLot.Text, Pmax = txtPmax.Text, Voc = txtVoc.Text, Isc = txtIsc.Text, Vmp = txtVmp.Text, Imp = txtImp.Text, FF = txtFF.Text, ModuleDate = txtModuleDate.Text, CellDate = txtCellDate.Text, ProductType = txtProductType.Text };
                    }
                    //显示组件信息
                    ShowModuleIndo(objModule);

                    //显示曲线
                    //LoadChart(Isc, Imp, Vmp, Voc, Pmax)
                    string script = "LoadChart({0},{1},{2},{3},{4})";
                    script = String.Format(script, objModule.Isc, objModule.Imp, objModule.Vmp, objModule.Voc, objModule.Pmax);
                    //Log(script);
                    myBrowser.ExecuteScriptAsync(script);

                    //写标签
                    if (cbxisWrite.Checked == true)
                    {
                        //检查组件信息是否完整
                        if (!CheckModuleInfo(objModule))
                        {
                            Log(objModule.ModuleID + "烧录失败:" + "组件信息不全", 0);
                            txtLot.Text = "";
                            return;
                        }

                        if (objRFID.WriteTagBuff(TagDataFormat.CreateByteArray(objModule)) == true && CheckWrite() == objModule.ModuleID)
                        {
                            Log(objModule.ModuleID + "烧录成功",1);
                            //读取卡关
                            if (cbxIsRead.Checked)
                            {
                                System.Threading.Thread.Sleep(Convert.ToInt32(ddlinternalTime.SelectedValue + "000"));
                                Log(objModule.ModuleID + "读取成功",1);
                            }

                            string wcInfo = "ModuleID:" + objModule.ModuleID + "|"
                                          + "ModuleDate:" + objModule.ModuleDate + "|"
                                          + "Imp:" + objModule.Imp + "|"
                                          + "Isc:" + objModule.Isc + "|"
                                          + "Voc" + objModule.Voc + "|"
                                          + "Vmp" + objModule.Vmp + "|"
                                          + "Pmax" + objModule.Pmax + "|"
                                          + "ProductType" + objModule.ProductType + "|"
                                          + "FF" + objModule.FF;
                            //var res =new CRUD().InvokeWebService("http://10.60.3.27/PMSService/DataService.asmx", "RecordRfidInfo", new object[] { objModule.ModuleID, wcInfo });
                            Type t = assemblyobj.GetType("client.Service", true, true);
                            object obj = Activator.CreateInstance(t);
                            System.Reflection.MethodInfo mi = t.GetMethod("RecordRfidInfo");
                            mi.Invoke(obj, new object[] { objModule.ModuleID, wcInfo });
                        }
                        else
                        {
                            Log(objModule.ModuleID + "烧录失败:" + objRFID.rfidConfig.Errormsg,0);
                        }
                    }
                    txtLot.Text = "";

                }
            }
            catch (Exception ex)
            {
                Log("烧录失败:" + ex.Message,0);
                txtLot.Text = "";
            }
        }
        #region 读取标签
        private string CheckWrite()
        {
            //throw new Exception();
            string res = "";
            if (!objRFID.ReadTagID())
            {
                objRFID.Beep(20);
                res = "fail";
            }
            else
            {
                System.Threading.Thread.Sleep(20);
                ErrorCode ec = objRFID.IsTagWrited();// ReadData();
                switch (ec)
                {
                    case ErrorCode.CanNotFindTag:
                        objRFID.Beep(20);
                        res = "无法找到标签，请重试！";
                        //WriteLog(lrtxtLog, str, 1);
                        break;
                    case ErrorCode.OtherException:
                        objRFID.Beep(20);
                        res = "其他异常，请重试";
                        break;
                    case ErrorCode.ReadFail:

                        res = "读取失败，请重试！";

                        break;
                    case ErrorCode.ReadSuccessful:
                        //paintBackgroundColor(statusType.PASS);
                        objRFID.Beep(10);

                        //MessageBox.Show("read success");
                        //break;
                        ModuleObj mo = new ModuleObj();
                        mo = TagDataFormat.ParserTag(objRFID.rfidConfig.readBuffer);
                        res = mo.ModuleID.ToString();
                        break;
                    case ErrorCode.TagHasNoData:

                        res = "空标签！";

                        break;
                    default:
                        break;
                }
            }
            return res;
        }
        #endregion

        public ModuleObj QueryModuleIndo(string lot)
        {
            return new CRUD().QueryOracle(lot);
        }
       public void ShowModuleIndo(ModuleObj obj)
        {
            var json = new Jsonhelp();
            var path = AppDomain.CurrentDomain.BaseDirectory + "config.json";
            txtCellDate.Text = obj.CellDate;
            txtModuleDate.Text = obj.ModuleDate;
            txtIECDate.Text = json.readjson("IECDate", path);
            txtISO9001.Text = json.readjson("ISO", path);
            txtCertificationProvider.Text = json.readjson("CertificationProvider", path);
            txtModuleCountry.Text = json.readjson("Country", path);
            txtModuleCellSupplier.Text = json.readjson("ModuleCellSupplier", path);
            txtIsc.Text = obj.Isc;
            txtImp.Text = obj.Imp;
            txtPmax.Text = obj.Pmax;
            txtVmp.Text = obj.Vmp;
            txtVoc.Text = obj.Voc;
            txtFF.Text = obj.FF;
            txtProductType.Text = obj.ProductType;
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            //read tag
            if (!objRFID.ReadTagID())
            {
                //WriteLog(lrtxtLog, "没有发现标签！", 1);
                //
                //
                Log("没有发现标签！",0);
                return;
            }
            else
            {

                System.Threading.Thread.Sleep(20);

                ErrorCode ec = objRFID.IsTagWrited();// ReadData();
                switch (ec)
                {
                    case ErrorCode.CanNotFindTag:
                        Log("无法找到标签，请重试！",0);                       
                        break;
                    case ErrorCode.OtherException:                                             
                        Log("其他异常，请重试",0);
                        break;
                    case ErrorCode.ReadFail:                                           
                        Log("读取失败，请重试！",0);
                        break;
                    case ErrorCode.TagHasNoData:
                        Log("空标签！",0);                      
                        break;
                    case ErrorCode.ReadSuccessful:
                    
                        ModuleObj objModule = null;
                        objModule = TagDataFormat.ParserTag(objRFID.rfidConfig.readBuffer);
                        txtLot.Text = objModule.ModuleID.ToString();

                        //显示组件信息
                        ShowModuleIndo(objModule);

                        //显示曲线
                        //LoadChart(Isc, Imp, Vmp, Voc, Pmax)
                        string script = "LoadChart({0},{1},{2},{3},{4})";
                        script = String.Format(script, objModule.Isc, objModule.Imp, objModule.Vmp, objModule.Voc, objModule.Pmax);
                        //Log(script);
                        myBrowser.ExecuteScriptAsync(script);

                        Log(objModule.ModuleID + "读取成功",1);

                        //ShowModuleInfo(true);
                        ////add by xue lei 计算ff
                        //tbx_ff.Text = oModuleInfo.FF + "%";
                        //ShowIVCurves(double.Parse(oModuleInfo.Isc), double.Parse(oModuleInfo.Ipm), double.Parse(oModuleInfo.Vpm), double.Parse(oModuleInfo.Voc), oModuleInfo.Module_ID);
                        //string storedDataString = Encoding.ASCII.GetString(_RFIDDevice.rConfig.readBuffer);
                        //WriteLog(lrtxtLog, storedDataString.Replace("@@", "").Replace("##", ""), 0);
                        //_RFIDDevice.Speech("读取成功");
                        break;
                    
                    default:
                        break;
                }
            }

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            objRFID.Close();
        }

        public System.Reflection.Assembly CreateWc(string url)
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
                //MessageBox.Show(classname);
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
                return assembly;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Log("ng", 0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Log("ok", 1);
        }

        private void txtLog_TextChanged(object sender, EventArgs e)
        {
            // set the current caret position to the end
            txtLog.SelectionStart = txtLog.Text.Length;
            // scroll it automatically
            //txtLog.ScrollToCaret();
            txtLog.ScrollToCaret();
        }
    }
}
