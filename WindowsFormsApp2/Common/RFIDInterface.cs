using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFIDForm
{
    interface RFIDInterface
    {
        void Open(ref string strlog);
        void Close();
        ErrorCode IsTagWrited();
        byte[] ReadTagBuff();
        bool WriteTagBuff(byte[] buff);
        void Beep(int msec);
        void Speech(string Word);
        bool ReadTagID();

        Int16 st { get; set; }
        RFIDDeviceConfig rfidConfig { get; set; }
    }
    public enum ErrorCode
    {
        ReadSuccessful,
        ReadFail,
        TagHasNoData,
        CanNotFindTag,
        OtherException,
    }

    public class RFIDDeviceConfig
    {
        public RFIDDeviceConfig()
        {
            ComPort = "COM0";
            Baudrate = 115200;
            MemBank = 0x03;
            writeSuccees = false;
            m_curSetting = new ReaderSetting();
            m_curInventoryBuffer = new InventoryBuffer();
            m_curOperateTagBuffer = new OperateTagBuffer();
            m_curOperateTagISO18000Buffer = new OperateTagISO18000Buffer();
            TagUID = new byte[8];
            TagUIDString = string.Empty;
            Errormsg = string.Empty;

        }
        public string Errormsg { get; set; }
        public string ComPort { get; set; }
        public int Baudrate { get; set; }
        public byte MemBank { get; set; }
        public UHFReader.ReaderMethod reader { get; set; }
        public bool writeSuccees { get; set; }
        public ReaderSetting m_curSetting { get; set; }
        public InventoryBuffer m_curInventoryBuffer { get; set; }
        public OperateTagBuffer m_curOperateTagBuffer { get; set; }

        public OperateTagISO18000Buffer m_curOperateTagISO18000Buffer { get; set; }
        public byte[] TagUID { get; set; }
        public string  TagUIDString { get; set; }
        public byte[] readTempBuffer { get; set; }
        public byte[] readBuffer { get; set; }

    }
}
