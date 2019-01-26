using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace RFIDForm
{

    public class UHFISO18000 : RFIDInterface
    {
        public UHFISO18000()
        {
            this.st = 0;
            this.rfidConfig = new RFIDDeviceConfig();
            this.rfidConfig.reader = new UHFReader.ReaderMethod();
        }
        public short st { set; get; }
        public RFIDDeviceConfig rfidConfig { get; set; }

        public void Beep(int msec)
        {
            //throw new NotImplementedException();
        }

        public void Close()
        {
            //处理串口断开连接读写器
            rfidConfig.reader.CloseCom();
            //throw new NotImplementedException();
        }

        public ErrorCode IsTagWrited()
        {
            string[] reslut = CCommondMethod.StringToStringArray("00 00 00 00", 2);


            byte[] btAryPwd = CCommondMethod.StringArrayToByteArray(reslut, 4);

            byte[] rtnData = new byte[4];    //read first block, get the data length
            byte rtnLen = 0;
            // st = ISO15693Commands.rf_readblock(ReaderInfo.icdev, 0x22, 0x00, (byte)1, rfidConfig.m_btTagUID, out rtnLen, rtnData);

            st = (short)rfidConfig.reader.ReadTag(rfidConfig.m_curSetting.btReadId, rfidConfig.MemBank, (byte)0, (byte)2);
            int ReadCount = 0;
            rfidConfig.readTempBuffer = null;
            while ((rfidConfig.readTempBuffer == null || rfidConfig.readTempBuffer.Length != 4) && ReadCount < 30)
            {
                ReadCount++;
                st = (short)rfidConfig.reader.ReadTag(rfidConfig.m_curSetting.btReadId, rfidConfig.MemBank, (byte)0, (byte)2);// H3 的标签要读两遍 ，暂时没找到原因
                System.Threading.Thread.Sleep(100);
            }

            if (rfidConfig.readTempBuffer == null)
            {
                return ErrorCode.ReadFail;
            }
            rfidConfig.readTempBuffer.CopyTo(rtnData, 0);

            if (st != 0)
            {
                //MessageBox.Show("error");
                return ErrorCode.ReadFail;
            }
            else
            {
                //bool b_readLengthData = true;

                byte[] lenthData = new byte[2];
                byte[] test = new byte[2];
                //the first two bytes stored data length
                Array.Copy(rtnData, 2, lenthData, 0, 2);
                Array.Copy(rtnData, 0, test, 0, 2);
                Int32 i_totalBytes = BitConverter.ToInt16(lenthData, 0) + 4;

                if (i_totalBytes == 4)
                {
                    return ErrorCode.TagHasNoData;
                }

                //超高频 1个 Block = 2 个 byte

                rfidConfig.readBuffer = new byte[i_totalBytes - 4];



                st = 0;
                byte blockIndex = 2;
                int byteIndex = 0;

                while (i_totalBytes > 4 && st == 0)
                {
                    byte blockLen = 0;
                    //if (i_totalBytes % 4 == 0)
                    //{
                    //    blockLen = (byte)(i_totalBytes / 4);
                    //}
                    //else
                    //{
                    //    blockLen = (byte)(i_totalBytes / 4 + 1);
                    //}

                    blockLen = (byte)((i_totalBytes + 1) / 2);// 超高频 1块2个字节

                    //calculate block number required, max number is 10
                    byte blockNumber = blockLen > (byte)10 ? (byte)10 : blockLen;

                    //byte byteNumber = 0;

                    byte[] readData = new byte[blockNumber * 2];// 超高频 1块2个字节

                    // st = ISO15693Commands.rf_readblock(ReaderInfo.icdev, 0x22, blockIndex, blockNumber, rfidConfig.m_btTagUID, out rtnLen, readData);


                    ReadCount = 0;
                    rfidConfig.readTempBuffer = null;
                    while ((rfidConfig.readTempBuffer == null || rfidConfig.readTempBuffer.Length != blockNumber * 2) && ReadCount < 30)
                    {
                        ReadCount++;
                        st = (short)rfidConfig.reader.ReadTag(rfidConfig.m_curSetting.btReadId, rfidConfig.MemBank, blockIndex, blockNumber);// H3 的标签要读两遍 ，暂时没找到原因
                        System.Threading.Thread.Sleep(100);
                    }
                    readData = rfidConfig.readTempBuffer;
                    if (readData != null)
                    {
                        int leftDataLength = rfidConfig.readBuffer.Length - byteIndex;
                        int copyDataLength = leftDataLength > readData.Length ? readData.Length : leftDataLength;
                        rtnLen = (byte)copyDataLength;
                        //if (b_readLengthData)
                        //{
                        //    Array.Copy(readData, 2, readBuffer, byteIndex, copyDataLength == readData.Length ? copyDataLength - 2 : copyDataLength);

                        //    //b_readLengthData = true;
                        //}
                        //else
                        //{
                        Array.Copy(readData, 0, rfidConfig.readBuffer, byteIndex, copyDataLength);
                        //}
                    }
                    else
                    {
                        return ErrorCode.ReadFail;
                    }

                    byteIndex += rtnLen;
                    //if (b_readLengthData)
                    //{
                    //    byteIndex -= 2;

                    //    b_readLengthData = false;
                    //}
                    blockIndex += blockNumber;
                    i_totalBytes -= rtnLen;

                    System.Threading.Thread.Sleep(20);
                }

                return ErrorCode.ReadSuccessful;

            }

            //throw new NotImplementedException();
        }

        public void Open(ref string strlog)
        {
            //处理串口连接读写器
            string strException = string.Empty;
            //string strComPort = cmbComPort.Text;
            //int nBaudrate = Convert.ToInt32(cmbBaudrate.Text);
            rfidConfig.reader = new UHFReader.ReaderMethod(); //new UHFReader.ReaderMethod();
                                                              //回调函数
            rfidConfig.reader.AnalyCallback = AnalyData;
            //rfidConfig.reader.ReceiveCallback = ReceiveData;
            //rfidConfig.reader.SendCallback = SendData;
            int nRet=1;// = rfidConfig.reader.OpenCom(rfidConfig.ComPort, rfidConfig.Baudrate, out strException);
                     //if (nRet != 0)


            for (int i = 1; i < 20; i++)
            {
                string strComPort = "COM" + i.ToString();
                nRet = rfidConfig.reader.OpenCom(strComPort, rfidConfig.Baudrate, out strException);
                if (nRet == 0)
                {
                    rfidConfig.ComPort = strComPort;
                    break;
                }
            }
            if ((rfidConfig.m_curSetting.btRegion < 1) || (rfidConfig.m_curSetting.btRegion > 4)) //如果是自定义的频谱则需要先提取自定义频率信息
            {
                rfidConfig.reader.GetFrequencyRegion(rfidConfig.m_curSetting.btReadId);
                System.Threading.Thread.Sleep(5);

            }
            if (nRet != 0)
            {
                strlog = "连接读写器失败，失败原因： " + strException; ;
                return;
            }
            else
            {
                strlog = "连接读写器 " + rfidConfig.ComPort + "@" + rfidConfig.Baudrate;

            }
            
            //string strLog = "连接读写器失败，失败原因： " + strException;
            //WriteLog(lrtxtLog, strLog, 1);

            //return;

            //else
            //{
            //    string strLog = "连接读写器 " + rfidConfig.ComPort + "@" + rfidConfig.Baudrate;
            //    //WriteLog(lrtxtLog, strLog, 0);
            //}
            //throw new NotImplementedException();
        }

        public byte[] ReadTagBuff()
        {
            throw new NotImplementedException();
        }

        public void Speech(string sWorld)
        {
            try
            {
                SpeechSynthesizer synth = new SpeechSynthesizer();
                synth.SetOutputToDefaultAudioDevice();
                synth.SpeakAsync(sWorld);
            }
            catch (Exception)
            {

                //throw;
            }
        }

        public bool ReadTagID()
        {
            string[] reslut = CCommondMethod.StringToStringArray("00 00 00 00", 2);


            byte[] btAryPwd = CCommondMethod.StringArrayToByteArray(reslut, 4);
            byte[] rtnData = new byte[4];    //read first block, get the data length
            byte rtnLen = 0;
            int ReadCount = 0;
            rfidConfig.readTempBuffer = null;
            while (((rfidConfig.readTempBuffer == null) || (rfidConfig.readTempBuffer.Length != 24)) && ReadCount < 30)
            {
                ReadCount++;
                st = (short)rfidConfig.reader.ReadTag(rfidConfig.m_curSetting.btReadId, 0x02, (byte)0, (byte)12);
                System.Threading.Thread.Sleep(100);
            }
            rfidConfig.TagUID = rfidConfig.readTempBuffer;
            return true;
        }

        public bool WriteTagBuff(byte[] dataBytes)
        {
            string[] reslut = CCommondMethod.StringToStringArray("00 00 00 00", 2);
              

            byte[] btAryPwd = CCommondMethod.StringArrayToByteArray(reslut, 4);
            try
            {
                byte[] dataLen = BitConverter.GetBytes(dataBytes.Length);

                short version = (short)(200);
                string blockBuff = CCommondMethod.ByteArrayToHex(BitConverter.GetBytes(version)) + CCommondMethod.ByteArrayToHex(BitConverter.GetBytes((short)dataBytes.Length));
                byte[] oData = new byte[4];
                CCommondMethod.getByteData(blockBuff, out oData);
                int blockNum = ((dataBytes.Length * 2 + 3) / 4);//超高频 1个 Block = 2 个 byte
                byte[] writenDataAll = new byte[blockNum * 2 + 4];// +4 是因为前4个Byte 存储 version 和 长度
                for (int i = dataBytes.Length + 4; i < blockNum * 2 + 4; i++) // 超高频*2
                {
                    writenDataAll[i] = Convert.ToByte("00", 16);//补0
                }

                oData.CopyTo(writenDataAll, 0);               //data length stored in the third and forth byte
                dataBytes.CopyTo(writenDataAll, 4);             //useful data starts from next block--the fifth byte

                blockNum = Convert.ToByte(writenDataAll.Length / 2 + writenDataAll.Length % 2);

                System.Threading.Thread.Sleep(200);//50
                // rfidConfig.reader.Reset(rfidConfig.m_curSetting.btReadId);//复位读写器
                ///H3这款标签对普通的写指令支持的不好
                /////写数据时设专用功率值10dBm
                int ReadCount = 0;
                //rfidConfig.writeSucceed = false;!rfidConfig.writeSucceed &&
                rfidConfig.Errormsg = "start writing";
                while (rfidConfig.Errormsg!=""&&  ReadCount < 30)
                {

                    ReadCount++;
                    st = (short)rfidConfig.reader.WriteTag(rfidConfig.m_curSetting.btReadId, btAryPwd, rfidConfig.MemBank, 0, (byte)blockNum, writenDataAll);
                    System.Threading.Thread.Sleep(100);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
        }

        /*=========================================================================================================================
 *========================================================================================================================== 
 *========================================================================================================================== 
 *========================================================================================================================== 
 *========================================================================================================================== 
 *========================================================================================================================== 
 */
        #region reader 的 回调函数

        private void AnalyData(UHFReader.MessageTran msgTran)
        {
            // m_nReceiveFlag = 0;
            if (msgTran.PacketType != 0xA0)
            {
                return;
            }
            switch (msgTran.Cmd)
            {
                case 0x69:
                    ProcessSetProfile(msgTran);
                    break;
                case 0x6A:
                    ProcessGetProfile(msgTran);
                    break;
                case 0x71:
                    ProcessSetUartBaudrate(msgTran);
                    break;
                case 0x72:
                    ProcessGetFirmwareVersion(msgTran);
                    break;
                case 0x73:
                    ProcessSetReadAddress(msgTran);
                    break;
                case 0x74:
                    ProcessSetWorkAntenna(msgTran);
                    break;
                case 0x75:
                    ProcessGetWorkAntenna(msgTran);
                    break;
                case 0x76:
                    ProcessSetOutputPower(msgTran);
                    break;
                case 0x77:
                    ProcessGetOutputPower(msgTran);
                    break;
                case 0x78:
                    ProcessSetFrequencyRegion(msgTran);
                    break;
                case 0x79:
                    ProcessGetFrequencyRegion(msgTran);
                    break;
                case 0x7A:
                    ProcessSetBeeperMode(msgTran);
                    break;
                case 0x7B:
                    ProcessGetReaderTemperature(msgTran);
                    break;
                case 0x7C:
                    ProcessSetDrmMode(msgTran);
                    break;
                case 0x7D:
                    ProcessGetDrmMode(msgTran);
                    break;
                case 0x7E:
                    ProcessGetImpedanceMatch(msgTran);
                    break;
                case 0x60:
                    ProcessReadGpioValue(msgTran);
                    break;
                case 0x61:
                    ProcessWriteGpioValue(msgTran);
                    break;
                case 0x62:
                    ProcessSetAntDetector(msgTran);
                    break;
                case 0x63:
                    ProcessGetAntDetector(msgTran);
                    break;
                case 0x67:
                    ProcessSetReaderIdentifier(msgTran);
                    break;
                case 0x68:
                    ProcessGetReaderIdentifier(msgTran);
                    break;

                case 0x80:
                    ProcessInventory(msgTran);
                    break;
                case 0x81:
                    ProcessReadTag(msgTran);//读标签
                    break;
                case 0x82:
                case 0x94:
                    ProcessWriteTag(msgTran);//写标签
                    break;
                case 0x83:
                    ProcessLockTag(msgTran);//锁定标签
                    break;
                case 0x84:
                    ProcessKillTag(msgTran);//销毁标签
                    break;
                case 0x85:
                    ProcessSetAccessEpcMatch(msgTran);
                    break;
                case 0x86:
                    ProcessGetAccessEpcMatch(msgTran);//取得选定标签
                    break;

                case 0x89:
                case 0x8B:
                    ProcessInventoryReal(msgTran);
                    break;
                case 0x8A:
                    //ProcessFastSwitch(msgTran);
                    break;
                case 0x8D:
                    ProcessSetMonzaStatus(msgTran);
                    break;
                case 0x8E:
                    ProcessGetMonzaStatus(msgTran);
                    break;
                case 0x90:
                    ProcessGetInventoryBuffer(msgTran);
                    break;
                case 0x91:
                    ProcessGetAndResetInventoryBuffer(msgTran);
                    break;
                case 0x92:
                    ProcessGetInventoryBufferTagCount(msgTran);
                    break;
                case 0x93:
                    ProcessResetInventoryBuffer(msgTran);
                    break;
                case 0xb0:
                    ProcessInventoryISO18000(msgTran);
                    break;
                case 0xb1:
                    ProcessReadTagISO18000(msgTran);//读取标签
                    break;
                case 0xb2:
                    ProcessWriteTagISO18000(msgTran);//写入标签
                    break;
                case 0xb3:
                    ProcessLockTagISO18000(msgTran);
                    break;
                case 0xb4:
                    ProcessQueryISO18000(msgTran);
                    break;
                default:
                    break;
            }
        }
        #endregion


        #region Process

        /// <summary>
        /// 读标签
        /// </summary>
        /// <param name="msgTran"></param>
        private void ProcessReadTag(UHFReader.MessageTran msgTran)
        {
            string strCmd = "读标签";
            string strErrorCode = string.Empty;
            // rfidConfig.readTempBuffer = null;//add by genhong.hu On 2018-01-01
            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                //WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                int nLen = msgTran.AryData.Length;
                int nDataLen = Convert.ToInt32(msgTran.AryData[nLen - 3]);
                int nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - nDataLen - 4;

                string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                string strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                string strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                string strData = CCommondMethod.ByteArrayToString(msgTran.AryData, 7 + nEpcLen, nDataLen);

                rfidConfig.TagUID = new byte[nEpcLen];
                Array.Copy(msgTran.AryData, 5, rfidConfig.TagUID, 0, nEpcLen);
                rfidConfig.TagUIDString = CCommondMethod.ByteArrayToString(rfidConfig.TagUID, 0, rfidConfig.TagUID.Length);
                rfidConfig.readTempBuffer = new byte[nDataLen];
                Array.Copy(msgTran.AryData, 7 + nEpcLen, rfidConfig.readTempBuffer, 0, nDataLen);


                byte byTemp = msgTran.AryData[nLen - 2];
                byte byAntId = (byte)((byTemp & 0x03) + 1);
                string strAntId = byAntId.ToString();

                string strReadCount = msgTran.AryData[nLen - 1].ToString();

                DataRow row = rfidConfig.m_curOperateTagBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEPC;
                row[3] = strData;
                row[4] = nDataLen.ToString();
                row[5] = strAntId;
                row[6] = strReadCount;

                rfidConfig.m_curOperateTagBuffer.dtTagTable.Rows.Add(row);
                rfidConfig.m_curOperateTagBuffer.dtTagTable.AcceptChanges();

                //RefreshOpTag(0x81);
                //WriteLog(lrtxtLog, strCmd, 0);
            }
        }

        /// <summary>
        /// 写标签
        /// </summary>
        /// <param name="msgTran"></param>
        private void ProcessWriteTag(UHFReader.MessageTran msgTran)
        {
            string strCmd = "写标签";
            string strErrorCode = string.Empty;
            rfidConfig.Errormsg = string.Empty; // add by xue lei on 2019-1-24
            if (msgTran.AryData.Length == 1)
            {
                //rfidConfig.writeSucceed = false;//add by genhong.hu On 2018-01-05
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;
                rfidConfig.Errormsg = strLog;

                //WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                //rfidConfig.writeSucceed = true;//add by genhong.hu On 2018-01-05

                int nLen = msgTran.AryData.Length;
                int nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - 4;

                if (msgTran.AryData[nLen - 3] != 0x10)
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[nLen - 3]);
                    string strLog = strCmd + "失败，失败原因： " + strErrorCode;
                    rfidConfig.Errormsg = strLog;
                    //WriteLog(lrtxtLog, strLog, 1);
                    return;
                }

                string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                string strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                string strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                string strData = string.Empty;

                rfidConfig.readTempBuffer = new byte[nEpcLen];
                Array.Copy(msgTran.AryData, 5, rfidConfig.readTempBuffer, 0, nEpcLen);

                byte byTemp = msgTran.AryData[nLen - 2];
                byte byAntId = (byte)((byTemp & 0x03) + 1);
                string strAntId = byAntId.ToString();

                string strReadCount = msgTran.AryData[nLen - 1].ToString();

                DataRow row = rfidConfig.m_curOperateTagBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEPC;
                row[3] = strData;
                row[4] = string.Empty;
                row[5] = strAntId;
                row[6] = strReadCount;

                rfidConfig.m_curOperateTagBuffer.dtTagTable.Rows.Add(row);
                rfidConfig.m_curOperateTagBuffer.dtTagTable.AcceptChanges();

                //RefreshOpTag(0x82);
                //WriteLog(lrtxtLog, strCmd, 0);
            }
        }


        private void ProcessSetDrmMode(UHFReader.MessageTran msgTran)
        {
            string strCmd = "设置DRM模式";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessSetReadAddress(UHFReader.MessageTran msgTran)
        {
            string strCmd = "设置读写器地址";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessGetFirmwareVersion(UHFReader.MessageTran msgTran)
        {
            string strCmd = "取得读写器版本号";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 2)
            {
                rfidConfig.m_curSetting.btMajor = msgTran.AryData[0];
                rfidConfig.m_curSetting.btMinor = msgTran.AryData[1];
                rfidConfig.m_curSetting.btReadId = msgTran.ReadId;

                RefreshReadSetting(msgTran.Cmd);
                //WriteLog(lrtxtLog, strCmd, 0);
                return;
            }
            else if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessSetUartBaudrate(UHFReader.MessageTran msgTran)
        {
            string strCmd = "设置波特率";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessGetReaderTemperature(UHFReader.MessageTran msgTran)
        {
            string strCmd = "取得读写器温度";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 2)
            {
                rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                rfidConfig.m_curSetting.btPlusMinus = msgTran.AryData[0];
                rfidConfig.m_curSetting.btTemperature = msgTran.AryData[1];

                RefreshReadSetting(msgTran.Cmd);
                //WriteLog(lrtxtLog, strCmd, 0);
                return;
            }
            else if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessGetOutputPower(UHFReader.MessageTran msgTran)
        {
            string strCmd = "取得输出功率";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                rfidConfig.m_curSetting.btOutputPower = msgTran.AryData[0];

                RefreshReadSetting(0x77);
                //WriteLog(lrtxtLog, strCmd, 0);
                return;
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessSetOutputPower(UHFReader.MessageTran msgTran)
        {
            string strCmd = "设置输出功率";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessGetWorkAntenna(UHFReader.MessageTran msgTran)
        {
            string strCmd = "取得工作天线";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x00 || msgTran.AryData[0] == 0x01 || msgTran.AryData[0] == 0x02 || msgTran.AryData[0] == 0x03)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    rfidConfig.m_curSetting.btWorkAntenna = msgTran.AryData[0];

                    RefreshReadSetting(0x75);
                    //WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessSetWorkAntenna(UHFReader.MessageTran msgTran)
        {
            int intCurrentAnt = 0;
            intCurrentAnt = rfidConfig.m_curSetting.btWorkAntenna + 1;
            string strCmd = "设置工作天线成功,当前工作天线: 天线" + intCurrentAnt.ToString();

            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);

                    //校验是否盘存操作
                    //if (m_bInventory)
                    //{
                    //    RunLoopInventroy();
                    //}
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);

            //if (m_bInventory)
            //{
            //    m_curInventoryBuffer.nCommond = 1;
            //    m_curInventoryBuffer.dtEndInventory = DateTime.Now;
            //    RunLoopInventroy();
            //}
        }

        private void ProcessGetDrmMode(UHFReader.MessageTran msgTran)
        {
            string strCmd = "取得DRM模式";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x00 || msgTran.AryData[0] == 0x01)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    rfidConfig.m_curSetting.btDrmMode = msgTran.AryData[0];

                    RefreshReadSetting(0x7D);
                    //WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessGetFrequencyRegion(UHFReader.MessageTran msgTran)
        {
            string strCmd = "取得射频规范";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 3)
            {
                rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                rfidConfig.m_curSetting.btRegion = msgTran.AryData[0];
                rfidConfig.m_curSetting.btFrequencyStart = msgTran.AryData[1];
                rfidConfig.m_curSetting.btFrequencyEnd = msgTran.AryData[2];

                RefreshReadSetting(0x79);
                //WriteLog(lrtxtLog, strCmd, 0);
                return;
            }
            else if (msgTran.AryData.Length == 6)
            {
                rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                rfidConfig.m_curSetting.btRegion = msgTran.AryData[0];
                rfidConfig.m_curSetting.btUserDefineFrequencyInterval = msgTran.AryData[1];
                rfidConfig.m_curSetting.btUserDefineChannelQuantity = msgTran.AryData[2];
                rfidConfig.m_curSetting.nUserDefineStartFrequency = msgTran.AryData[3] * 256 * 256 + msgTran.AryData[4] * 256 + msgTran.AryData[5];
                RefreshReadSetting(0x79);
                //WriteLog(lrtxtLog, strCmd, 0);
                return;


            }
            else if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessSetFrequencyRegion(UHFReader.MessageTran msgTran)
        {
            string strCmd = "设置射频规范";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }


        private void ProcessSetBeeperMode(UHFReader.MessageTran msgTran)
        {
            string strCmd = "设置蜂鸣器模式";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessReadGpioValue(UHFReader.MessageTran msgTran)
        {
            string strCmd = "读取GPIO状态";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 2)
            {
                rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                rfidConfig.m_curSetting.btGpio1Value = msgTran.AryData[0];
                rfidConfig.m_curSetting.btGpio2Value = msgTran.AryData[1];

                RefreshReadSetting(0x60);
                //WriteLog(lrtxtLog, strCmd, 0);
                return;
            }
            else if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessWriteGpioValue(UHFReader.MessageTran msgTran)
        {
            string strCmd = "设置GPIO状态";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessGetAntDetector(UHFReader.MessageTran msgTran)
        {
            string strCmd = "读取天线连接检测阈值";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                rfidConfig.m_curSetting.btAntDetector = msgTran.AryData[0];

                RefreshReadSetting(0x63);
                //WriteLog(lrtxtLog, strCmd, 0);
                return;
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessGetMonzaStatus(UHFReader.MessageTran msgTran)
        {
            string strCmd = "读取Impinj Monza快速读TID功能";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x00 || msgTran.AryData[0] == 0x8D)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    rfidConfig.m_curSetting.btAntDetector = msgTran.AryData[0];

                    RefreshReadSetting(0x8E);
                    //WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessSetMonzaStatus(UHFReader.MessageTran msgTran)
        {
            string strCmd = "设置Impinj Monza快速读TID功能";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    rfidConfig.m_curSetting.btAntDetector = msgTran.AryData[0];

                    //WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessSetProfile(UHFReader.MessageTran msgTran)
        {
            string strCmd = "设置射频通讯链路配置";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    rfidConfig.m_curSetting.btLinkProfile = msgTran.AryData[0];

                    //WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessGetProfile(UHFReader.MessageTran msgTran)
        {
            string strCmd = "读取射频通讯链路配置";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if ((msgTran.AryData[0] >= 0xd0) && (msgTran.AryData[0] <= 0xd3))
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    rfidConfig.m_curSetting.btLinkProfile = msgTran.AryData[0];

                    RefreshReadSetting(0x6A);
                    //WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessGetReaderIdentifier(UHFReader.MessageTran msgTran)
        {
            string strCmd = "读取读写器识别标记";
            string strErrorCode = string.Empty;
            short i;
            string readerIdentifier = "";

            if (msgTran.AryData.Length == 12)
            {
                rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                for (i = 0; i < 12; i++)
                {
                    readerIdentifier = readerIdentifier + string.Format("{0:X2}", msgTran.AryData[i]) + " ";


                }
                rfidConfig.m_curSetting.btReaderIdentifier = readerIdentifier;
                RefreshReadSetting(0x68);

                //WriteLog(lrtxtLog, strCmd, 0);
                return;
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }

        private void ProcessGetImpedanceMatch(UHFReader.MessageTran msgTran)
        {
            string strCmd = "测量天线端口阻抗匹配";
            string strErrorCode = string.Empty;


            if (msgTran.AryData.Length == 1)
            {
                rfidConfig.m_curSetting.btReadId = msgTran.ReadId;

                rfidConfig.m_curSetting.btAntImpedance = msgTran.AryData[0];
                RefreshReadSetting(0x7E);

                //WriteLog(lrtxtLog, strCmd, 0);
                return;
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessSetReaderIdentifier(UHFReader.MessageTran msgTran)
        {
            string strCmd = "设置读写器识别标记";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessSetAntDetector(UHFReader.MessageTran msgTran)
        {
            string strCmd = "设置天线连接检测阈值";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    rfidConfig.m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);
        }



        private void ProcessInventoryReal(UHFReader.MessageTran msgTran)
        {
            //string strCmd = "";
            //if (msgTran.Cmd == 0x89)
            //{
            //    strCmd = "实时盘存";
            //}
            //if (msgTran.Cmd == 0x8B)
            //{
            //    strCmd = "自定义Session和Inventoried Flag盘存";
            //}
            //string strErrorCode = string.Empty;

            //if (msgTran.AryData.Length == 1)
            //{
            //    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            //    string strLog = strCmd + "失败，失败原因： " + strErrorCode;

            //    //WriteLog(lrtxtLog, strLog, 1);
            //    RefreshInventoryReal(0x00);
            //    RunLoopInventroy();
            //}
            //else if (msgTran.AryData.Length == 7)
            //{
            //    m_curInventoryBuffer.nReadRate = Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
            //    m_curInventoryBuffer.nDataCount = Convert.ToInt32(msgTran.AryData[3]) * 256 * 256 * 256 + Convert.ToInt32(msgTran.AryData[4]) * 256 * 256 + Convert.ToInt32(msgTran.AryData[5]) * 256 + Convert.ToInt32(msgTran.AryData[6]);

            //    //WriteLog(lrtxtLog, strCmd, 0);
            //    RefreshInventoryReal(0x01);
            //    RunLoopInventroy();
            //}
            //else
            //{
            //    m_nTotal++;
            //    int nLength = msgTran.AryData.Length;
            //    int nEpcLength = nLength - 4;

            //    //增加盘存明细表
            //    //if (msgTran.AryData[3] == 0x00)
            //    //{
            //    //    MessageBox.Show("");
            //    //}
            //    string strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, nEpcLength);
            //    string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 2);
            //    string strRSSI = msgTran.AryData[nLength - 1].ToString();
            //    SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nLength - 1]));
            //    byte btTemp = msgTran.AryData[0];
            //    byte btAntId = (byte)((btTemp & 0x03) + 1);
            //    m_curInventoryBuffer.nCurrentAnt = btAntId;
            //    string strAntId = btAntId.ToString();

            //    byte btFreq = (byte)(btTemp >> 2);
            //    string strFreq = GetFreqString(btFreq);

            //    //DataRow row = m_curInventoryBuffer.dtTagDetailTable.NewRow();
            //    //row[0] = strEPC;
            //    //row[1] = strRSSI;
            //    //row[2] = strAntId;
            //    //row[3] = strFreq;

            //    //m_curInventoryBuffer.dtTagDetailTable.Rows.Add(row);
            //    //m_curInventoryBuffer.dtTagDetailTable.AcceptChanges();

            //    ////增加标签表
            //    //DataRow[] drsDetail = m_curInventoryBuffer.dtTagDetailTable.Select(string.Format("COLEPC = '{0}'", strEPC));
            //    //int nDetailCount = drsDetail.Length;
            //    DataRow[] drs = m_curInventoryBuffer.dtTagTable.Select(string.Format("COLEPC = '{0}'", strEPC));
            //    if (drs.Length == 0)
            //    {
            //        DataRow row1 = m_curInventoryBuffer.dtTagTable.NewRow();
            //        row1[0] = strPC;
            //        row1[2] = strEPC;
            //        row1[4] = strRSSI;
            //        row1[5] = "1";
            //        row1[6] = strFreq;

            //        m_curInventoryBuffer.dtTagTable.Rows.Add(row1);
            //        m_curInventoryBuffer.dtTagTable.AcceptChanges();
            //    }
            //    else
            //    {
            //        foreach (DataRow dr in drs)
            //        {
            //            dr.BeginEdit();

            //            dr[4] = strRSSI;
            //            dr[5] = (Convert.ToInt32(dr[5]) + 1).ToString();
            //            dr[6] = strFreq;

            //            dr.EndEdit();
            //        }
            //        m_curInventoryBuffer.dtTagTable.AcceptChanges();
            //    }

            //    m_curInventoryBuffer.dtEndInventory = DateTime.Now;
            //    RefreshInventoryReal(0x89);
            //}
        }



        private void ProcessInventory(UHFReader.MessageTran msgTran)
        {
            //string strCmd = "盘存标签";
            //string strErrorCode = string.Empty;

            //if (msgTran.AryData.Length == 9)
            //{
            //    m_curInventoryBuffer.nCurrentAnt = msgTran.AryData[0];
            //    m_curInventoryBuffer.nTagCount = Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
            //    m_curInventoryBuffer.nReadRate = Convert.ToInt32(msgTran.AryData[3]) * 256 + Convert.ToInt32(msgTran.AryData[4]);
            //    int nTotalRead = Convert.ToInt32(msgTran.AryData[5]) * 256 * 256 * 256
            //        + Convert.ToInt32(msgTran.AryData[6]) * 256 * 256
            //        + Convert.ToInt32(msgTran.AryData[7]) * 256
            //        + Convert.ToInt32(msgTran.AryData[8]);
            //    m_curInventoryBuffer.nDataCount = nTotalRead;
            //    m_curInventoryBuffer.lTotalRead.Add(nTotalRead);
            //    m_curInventoryBuffer.dtEndInventory = DateTime.Now;

            //    RefreshInventory(0x80);
            //    //WriteLog(lrtxtLog, strCmd, 0);

            //    RunLoopInventroy();

            //    return;
            //}
            //else if (msgTran.AryData.Length == 1)
            //{
            //    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            //}
            //else
            //{
            //    strErrorCode = "未知错误";
            //}

            //string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            ////WriteLog(lrtxtLog, strLog, 1);

            //RunLoopInventroy();
        }


        private void ProcessGetInventoryBuffer(UHFReader.MessageTran msgTran)
        {
            string strCmd = "读取缓存";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                //WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                int nDataLen = msgTran.AryData.Length;
                int nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - 4;

                string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                string strEpc = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                string strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                string strRSSI = msgTran.AryData[nDataLen - 3].ToString();
                //SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nDataLen - 3]));
                byte btTemp = msgTran.AryData[nDataLen - 2];
                byte btAntId = (byte)((btTemp & 0x03) + 1);
                string strAntId = btAntId.ToString();
                string strReadCnr = msgTran.AryData[nDataLen - 1].ToString();

                DataRow row = rfidConfig.m_curInventoryBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEpc;
                row[3] = strAntId;
                row[4] = strRSSI;
                row[5] = strReadCnr;

                rfidConfig.m_curInventoryBuffer.dtTagTable.Rows.Add(row);
                rfidConfig.m_curInventoryBuffer.dtTagTable.AcceptChanges();

                //RefreshInventory(0x90);
                //WriteLog(lrtxtLog, strCmd, 0);
            }
        }



        private void ProcessGetAndResetInventoryBuffer(UHFReader.MessageTran msgTran)
        {
            //string strCmd = "读取清空缓存";
            //string strErrorCode = string.Empty;

            //if (msgTran.AryData.Length == 1)
            //{
            //    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            //    string strLog = strCmd + "失败，失败原因： " + strErrorCode;

            //    //WriteLog(lrtxtLog, strLog, 1);
            //}
            //else
            //{
            //    int nDataLen = msgTran.AryData.Length;
            //    int nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - 4;

            //    string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
            //    string strEpc = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
            //    string strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
            //    string strRSSI = msgTran.AryData[nDataLen - 3].ToString();
            //    SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nDataLen - 3]));
            //    byte btTemp = msgTran.AryData[nDataLen - 2];
            //    byte btAntId = (byte)((btTemp & 0x03) + 1);
            //    string strAntId = btAntId.ToString();
            //    string strReadCnr = msgTran.AryData[nDataLen - 1].ToString();

            //    DataRow row = m_curInventoryBuffer.dtTagTable.NewRow();
            //    row[0] = strPC;
            //    row[1] = strCRC;
            //    row[2] = strEpc;
            //    row[3] = strAntId;
            //    row[4] = strRSSI;
            //    row[5] = strReadCnr;

            //    m_curInventoryBuffer.dtTagTable.Rows.Add(row);
            //    m_curInventoryBuffer.dtTagTable.AcceptChanges();

            //    RefreshInventory(0x91);
            //    //WriteLog(lrtxtLog, strCmd, 0);
            //}
        }



        private void ProcessGetInventoryBufferTagCount(UHFReader.MessageTran msgTran)
        {
            //string strCmd = "缓存标签数量";
            //string strErrorCode = string.Empty;

            //if (msgTran.AryData.Length == 2)
            //{
            //    m_curInventoryBuffer.nTagCount = Convert.ToInt32(msgTran.AryData[0]) * 256 + Convert.ToInt32(msgTran.AryData[1]);

            //    RefreshInventory(0x92);
            //    string strLog1 = strCmd + " " + m_curInventoryBuffer.nTagCount.ToString();
            //    //WriteLog(lrtxtLog, strLog1, 0);
            //    return;
            //}
            //else if (msgTran.AryData.Length == 1)
            //{
            //    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            //}
            //else
            //{
            //    strErrorCode = "未知错误";
            //}

            //string strLog = strCmd + "失败，失败原因： " + strErrorCode;

            //WriteLog(lrtxtLog, strLog, 1);
        }


        private void ProcessResetInventoryBuffer(UHFReader.MessageTran msgTran)
        {
            string strCmd = "清空缓存";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    //RefreshInventory(0x93);
                    //WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;

            //WriteLog(lrtxtLog, strLog, 1);
        }


        /// <summary>
        /// 取得选定标签
        /// </summary>
        /// <param name="msgTran"></param>
        private void ProcessGetAccessEpcMatch(UHFReader.MessageTran msgTran)
        {
            string strCmd = "取得选定标签";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x01)
                {
                    //WriteLog(lrtxtLog, "未选定标签", 0);
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                if (msgTran.AryData[0] == 0x00)
                {
                    rfidConfig.m_curOperateTagBuffer.strAccessEpcMatch = CCommondMethod.ByteArrayToString(msgTran.AryData, 2, Convert.ToInt32(msgTran.AryData[1]));

                    //RefreshOpTag(0x86);
                    //WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }
                else
                {
                    strErrorCode = "未知错误";
                }
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;

            //WriteLog(lrtxtLog, strLog, 1);
        }

        /// <summary>
        /// 选定/取消选定标签
        /// </summary>
        /// <param name="msgTran"></param>
        private void ProcessSetAccessEpcMatch(UHFReader.MessageTran msgTran)
        {
            string strCmd = "选定/取消选定标签";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    //WriteLog(lrtxtLog, strCmd, 0);
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;

            //WriteLog(lrtxtLog, strLog, 1);
        }


        /// <summary>
        /// 锁定标签
        /// </summary>
        /// <param name="msgTran"></param>
        private void ProcessLockTag(UHFReader.MessageTran msgTran)
        {
            string strCmd = "锁定标签";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                //WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                int nLen = msgTran.AryData.Length;
                int nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - 4;

                if (msgTran.AryData[nLen - 3] != 0x10)
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[nLen - 3]);
                    string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                    //WriteLog(lrtxtLog, strLog, 1);
                    return;
                }

                string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                string strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                string strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                string strData = string.Empty;

                byte byTemp = msgTran.AryData[nLen - 2];
                byte byAntId = (byte)((byTemp & 0x03) + 1);
                string strAntId = byAntId.ToString();

                string strReadCount = msgTran.AryData[nLen - 1].ToString();

                DataRow row = rfidConfig.m_curOperateTagBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEPC;
                row[3] = strData;
                row[4] = string.Empty;
                row[5] = strAntId;
                row[6] = strReadCount;

                rfidConfig.m_curOperateTagBuffer.dtTagTable.Rows.Add(row);
                rfidConfig.m_curOperateTagBuffer.dtTagTable.AcceptChanges();

                //RefreshOpTag(0x83);
                //WriteLog(lrtxtLog, strCmd, 0);
            }
        }



        private void ProcessKillTag(UHFReader.MessageTran msgTran)
        {
            string strCmd = "销毁标签";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                //WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                int nLen = msgTran.AryData.Length;
                int nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - 4;

                if (msgTran.AryData[nLen - 3] != 0x10)
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[nLen - 3]);
                    string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                    //WriteLog(lrtxtLog, strLog, 1);
                    return;
                }

                string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                string strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                string strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                string strData = string.Empty;

                byte byTemp = msgTran.AryData[nLen - 2];
                byte byAntId = (byte)((byTemp & 0x03) + 1);
                string strAntId = byAntId.ToString();

                string strReadCount = msgTran.AryData[nLen - 1].ToString();

                DataRow row = rfidConfig.m_curOperateTagBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEPC;
                row[3] = strData;
                row[4] = string.Empty;
                row[5] = strAntId;
                row[6] = strReadCount;

                rfidConfig.m_curOperateTagBuffer.dtTagTable.Rows.Add(row);
                rfidConfig.m_curOperateTagBuffer.dtTagTable.AcceptChanges();

                //RefreshOpTag(0x84);
                //WriteLog(lrtxtLog, strCmd, 0);
            }
        }

        private void ProcessInventoryISO18000(UHFReader.MessageTran msgTran)
        {
            string strCmd = "盘存标签";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] != 0xFF)
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                    string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                    //WriteLog(lrtxtLog, strLog, 1);
                }
            }
            else if (msgTran.AryData.Length == 9)
            {
                string strAntID = CCommondMethod.ByteArrayToString(msgTran.AryData, 0, 1);
                string strUID = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 8);

                //增加保存标签列表，原未盘存则增加记录，否则将标签盘存数量加1
                DataRow[] drs = rfidConfig.m_curOperateTagISO18000Buffer.dtTagTable.Select(string.Format("UID = '{0}'", strUID));
                if (drs.Length == 0)
                {
                    DataRow row = rfidConfig.m_curOperateTagISO18000Buffer.dtTagTable.NewRow();
                    row[0] = strAntID;
                    row[1] = strUID;
                    row[2] = "1";
                    rfidConfig.m_curOperateTagISO18000Buffer.dtTagTable.Rows.Add(row);
                    rfidConfig.m_curOperateTagISO18000Buffer.dtTagTable.AcceptChanges();
                }
                else
                {
                    DataRow row = drs[0];
                    row.BeginEdit();
                    row[2] = (Convert.ToInt32(row[2]) + 1).ToString();
                    rfidConfig.m_curOperateTagISO18000Buffer.dtTagTable.AcceptChanges();
                }

            }
            else if (msgTran.AryData.Length == 2)
            {
                rfidConfig.m_curOperateTagISO18000Buffer.nTagCnt = Convert.ToInt32(msgTran.AryData[1]);
                //RefreshISO18000(msgTran.Cmd);

                //WriteLog(lrtxtLog, strCmd, 0);
            }
            else
            {
                strErrorCode = "未知错误";
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                //WriteLog(lrtxtLog, strLog, 1);
            }
        }


        /// <summary>
        /// 读取标签
        /// </summary>
        /// <param name="msgTran"></param>
        private void ProcessReadTagISO18000(UHFReader.MessageTran msgTran)
        {
            string strCmd = "读取标签";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                //WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                string strAntID = CCommondMethod.ByteArrayToString(msgTran.AryData, 0, 1);
                string strData = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, msgTran.AryData.Length - 1);

                rfidConfig.m_curOperateTagISO18000Buffer.btAntId = Convert.ToByte(strAntID);
                rfidConfig.m_curOperateTagISO18000Buffer.strReadData = strData;

                //RefreshISO18000(msgTran.Cmd);

                //WriteLog(lrtxtLog, strCmd, 0);
            }
        }


        /// <summary>
        /// 写入标签
        /// </summary>
        /// <param name="msgTran"></param>
        private void ProcessWriteTagISO18000(UHFReader.MessageTran msgTran)
        {
            string strCmd = "写入标签";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                //WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                //string strAntID = CCommondMethod.ByteArrayToString(msgTran.AryData, 0, 1);
                //string strCnt = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 1);

                rfidConfig.m_curOperateTagISO18000Buffer.btAntId = msgTran.AryData[0];
                rfidConfig.m_curOperateTagISO18000Buffer.btWriteLength = msgTran.AryData[1];

                ////RefreshISO18000(msgTran.Cmd);
                string strLength = msgTran.AryData[1].ToString();
                string strLog = strCmd + ": " + "成功写入" + strLength + "字节";
                //WriteLog(lrtxtLog, strLog, 0);
                //RunLoopISO18000(Convert.ToInt32(msgTran.AryData[1]));
            }
        }



        private void ProcessLockTagISO18000(UHFReader.MessageTran msgTran)
        {
            //string strCmd = "永久写保护";
            //string strErrorCode = string.Empty;

            //if (msgTran.AryData.Length == 1)
            //{
            //    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
            //    string strLog = strCmd + "失败，失败原因： " + strErrorCode;

            //    //WriteLog(lrtxtLog, strLog, 1);
            //}
            //else
            //{
            //    //string strAntID = CCommondMethod.ByteArrayToString(msgTran.AryData, 0, 1);
            //    //string strStatus = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 1);

            //    rfidConfig.m_curOperateTagISO18000Buffer.btAntId = msgTran.AryData[0];
            //    rfidConfig.m_curOperateTagISO18000Buffer.btStatus = msgTran.AryData[1];

            //    ////RefreshISO18000(msgTran.Cmd);
            //    string strLog = string.Empty;
            //    switch (msgTran.AryData[1])
            //    {
            //        case 0x00:
            //            strLog = strCmd + ": " + "成功锁定";
            //            break;
            //        case 0xFE:
            //            strLog = strCmd + ": " + "已是锁定状态";
            //            break;
            //        case 0xFF:
            //            strLog = strCmd + ": " + "无法锁定";
            //            break;
            //        default:
            //            break;
            //    }

            //    //WriteLog(lrtxtLog, strLog, 0);

            //}
        }



        private void ProcessQueryISO18000(UHFReader.MessageTran msgTran)
        {
            string strCmd = "查询标签";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                //WriteLog(lrtxtLog, strLog, 1);
            }
            else
            {
                //string strAntID = CCommondMethod.ByteArrayToString(msgTran.AryData, 0, 1);
                //string strStatus = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 1);

                rfidConfig.m_curOperateTagISO18000Buffer.btAntId = msgTran.AryData[0];
                rfidConfig.m_curOperateTagISO18000Buffer.btStatus = msgTran.AryData[1];

                //RefreshISO18000(msgTran.Cmd);

                //WriteLog(lrtxtLog, strCmd, 0);
            }
        }

        #endregion
        private void RefreshReadSetting(byte btCmd)
        {


        }
    }
}
