﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
namespace AutoTask
{
    public static class CommonHelper
    {
        public static string SqlDataBankToString(this object bankData)
        {
            return bankData == DBNull.Value ? "" : bankData.ToString();
        }

        public static string DataRowGetStringValue(this DataRow bankData, string key)
        {
          
            return bankData.Table.Columns.Contains(key.ToLower())? bankData[key].SqlDataBankToString() :  "" ;
        }

        public static double DataRowGetDoubleValue(this DataRow bankData, string key)
        {

            return bankData.Table.Columns.Contains(key.ToLower()) ? bankData[key].SqlDataBankToDouble() : 0;
        }
        
        public static int DataRowGetIntValue(this DataRow bankData, string key)
        {
            return bankData.Table.Columns.Contains(key.ToLower()) ? bankData[key].SqlDataBankToInt() : 0;
        }

        public static sbyte DataRowGetByteValue(this DataRow bankData, string key)
        {
            return bankData.Table.Columns.Contains(key.ToLower()) ? bankData[key].SqlDataBankToByte() : Convert.ToSByte(0);
        }
        public static int SqlDataBankToInt(this object bankData)
        {
            return bankData == DBNull.Value ? 0 : Convert.ToInt32(bankData);
        }

        public static double SqlDataBankToDouble(this object bankData)
        {
            return bankData == DBNull.Value ? 0 : Convert.ToDouble(bankData);
        }

        public static sbyte SqlDataBankToByte(this object bankData)
        {
            return bankData == DBNull.Value ? Convert.ToSByte(0) : Convert.ToSByte(bankData);
        }

        public static void Log(string content, string FileName)
        {
            try
            {
                string filename = FileName + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                string filePath = AppDomain.CurrentDomain.BaseDirectory + "Log/" + filename;
                FileInfo file = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "Log/" + filename);
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString());
                sb.Append(" ");
                sb.Append(content);
                FileMode fm = new FileMode();
                if (!file.Exists)
                {
                    fm = FileMode.Create;
                }
                else
                {
                    fm = FileMode.Append;
                }
                using (FileStream fs = new FileStream(filePath, fm, FileAccess.Write, FileShare.Write))
                {
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("gb2312")))
                    {
                        sw.WriteLine(sb.ToString());
                        sw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Log("生成Log失败" + ex.Message.ToString(), "Log异常");
            }
        }
    }
}
