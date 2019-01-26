using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFIDForm
{
    public class TagDataFormat
    {
        /// <summary>
        /// 数据开始标识
        /// </summary>
        private static readonly String PacketStart = BasicConfigInfo.PacketStart;
        /// <summary>
        /// 数据结束标识
        /// </summary>
        private static readonly String PacketEnd = BasicConfigInfo.PacketEnd;
        /// <summary>
        /// 基准日期 2016-01-01
        /// </summary>
        private static readonly DateTime MinDate = BasicConfigInfo.MinDate;


        public static byte[] CreateByteArray(ModuleObj mi)
        {
            int year = DateTime.Parse(mi.ModuleDate).Year;
            int month = DateTime.Parse(mi.ModuleDate).Month;
            int day = DateTime.Parse(mi.ModuleDate).Day;
            DateTime moduledate = new DateTime(year, month, day, 0, 0, 0);
            year = DateTime.Parse(mi.CellDate).Year;
            month = DateTime.Parse(mi.CellDate).Month;
            day = DateTime.Parse(mi.CellDate).Day;
            //string CellSource = mi.Cellsource;//add by genhong.hu On 2017-12-31，在此之前高频读卡器没有写入CellSource
            DateTime celldate = new DateTime(year, month, day, 0, 0, 0);
            decimal iPmax = Decimal.Parse(mi.Pmax) * 100M;
            decimal iVoc = Decimal.Parse(mi.Voc) * 100M;
            decimal iIsc = Decimal.Parse(mi.Isc) * 100M;
            decimal iVpm = Decimal.Parse(mi.Vmp) * 100M;
            decimal iIpm = Decimal.Parse(mi.Imp) * 100M;
            //添加ff add by xue lei on 2018-6-23
            decimal iFF = Decimal.Parse(mi.FF) * 100M;

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(PacketStart);//@@
                    writer.Write(mi.ProductType);
                    writer.Write(mi.ModuleID);
                    writer.Write(DateToInt16(moduledate));
                    writer.Write((int)iPmax);
                    writer.Write((short)iVoc);
                    writer.Write((short)iIsc);
                    writer.Write((short)iVpm);
                    writer.Write((short)iIpm);
                    // 添加ff add by xue lei on 2018-6-23
                    writer.Write((short)iFF);
                    // writer.Write(CellSource);//add by genhong.hu On 2017-12-31，在此之前高频读卡器没有写入CellSource
                    writer.Write(DateToInt16(celldate));
                    //writer.Write(PacketEnd);//##
                    writer.Close();
                }
                return stream.ToArray();

                //string res = PacketStart + "|" + mi.ProductType + "|" + mi.Module_ID + "|" + dateOfModulePacked.ToString() + "|";// +
                //iPmax.ToString() + "|" + iVoc.ToString() + "|" + iIsc.ToString() + "|" + iVpm.ToString() + "|" + iIpm.ToString()
                //+ "|" + iFF.ToString() + "|" + celldate.ToString();
                //return System.Text.Encoding.Default.GetBytes(res);
            }
        }





        #region============解析标签内容，读取时用到===============================================================================
        /// <summary>
        /// 解析标签内容，读取时用到
        /// </summary>
        /// <param name="tagBuff"></param>
        /// <returns></returns>
        public static ModuleObj ParserTag(byte[] tagBuff)
        {
            try
            {
                ModuleObj o = new ModuleObj();
                MemoryStream memStream = new MemoryStream(tagBuff);
                BinaryReader buffReader = new BinaryReader(memStream);
                string packetStart = buffReader.ReadString();
                if (packetStart != PacketStart)
                {
                    throw new Exception("数据包开始标志出错");
                }
                o.ProductType = buffReader.ReadString();
                o.ModuleID = buffReader.ReadString();
                DateTime moduledate = DateFormInt16(buffReader.ReadInt16());
                o.ModuleDate = moduledate.ToString("yyyy-MM-dd");
                //string pivf = buffReader.ReadString();
                double Pmax = buffReader.ReadInt32() * 1.0 / 100;
                double Voc = buffReader.ReadInt16() * 1.0 / 100;
                double Isc = buffReader.ReadInt16() * 1.0 / 100;
                double Vpm = buffReader.ReadInt16() * 1.0 / 100;
                double Ipm = buffReader.ReadInt16() * 1.0 / 100;
                double FF = buffReader.ReadInt16() * 1.0 / 100;
                o.Pmax = Pmax.ToString("0.00");
                o.Voc = Voc.ToString("0.00");
                o.Isc = Isc.ToString("0.00");
                o.Vmp = Vpm.ToString("0.00");
                o.Imp = Ipm.ToString("0.00");
                o.FF = FF.ToString("0.00");
                DateTime celldate = DateFormInt16(buffReader.ReadInt16());// Add by genhong.hu On 2014-08-11 ; 
                //电池时间是组件时间-7天
                o.CellDate = celldate.ToString("yyyy-MM-dd");
                return o;
            }
            catch (Exception ex)
            {
                throw new Exception("解析数据包出错：\r\n" + ex.Message);
            }
        }
        #endregion


        /// <summary>
        /// 当前日期减去基准日期相差的天数
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static short DateToInt16(DateTime date)
        {
            TimeSpan span = date - MinDate;// DateTime.Parse("2016-01-01");
            int days = span.Days;
            return (short)days;


        }


        /// <summary>
        /// 基准日期加上相差天数
        /// </summary>
        /// <param name="days"></param>
        /// <returns></returns>
        private static DateTime DateFormInt16(short days)
        {
            DateTime date = MinDate.AddDays(days);
            return date;
        }
    }
}
