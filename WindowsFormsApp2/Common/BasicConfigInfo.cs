using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFIDForm
{
    public static class BasicConfigInfo
    {
        /// <summary>
        /// 组件生产厂商，如：SUNTECH
        /// </summary>
        public static string Manufacturer = "SUNTECH";
        /// <summary>
        /// 生产国,如：CHINA
        /// </summary>
        public static string MadeInCountry = "CHINA";
        /// <summary>
        /// 认证日期 如：2010-10-09
        /// </summary>
        public static string IecCertificateDate = "2017-05-24";
        /// <summary>
        /// 认证名称 如： IEC TUV
        /// </summary>
        public static string IecCertificate = "";
        /// <summary>
        /// 认证机构 如：TUV Germany
        /// </summary>
        public static string IecCertificateLib = "VDE";


        /// <summary>
        /// Other relevant information on traceability of solar cells and module as per ISO 9001
        /// </summary>
        public static string ISO9001 = "";
        /// <summary>
        /// 基准日期
        /// </summary>
        public static DateTime MinDate = DateTime.Parse("2016-01-01");

        /// <summary>
        /// 数据开始标识
        /// </summary>
        public static string PacketStart = "@@";
        /// <summary>
        /// 数据结束标识
        /// </summary>
        public static string PacketEnd = "##";

        /// <summary>
        /// 程序版本
        /// </summary>
        public static int Version = 200;


        public static string PassWord = "123";

        public static string path= AppDomain.CurrentDomain.BaseDirectory + "config.json";
    }
}

