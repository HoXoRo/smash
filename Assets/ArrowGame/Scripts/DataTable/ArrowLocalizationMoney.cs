//------------------------------------------------------------
//------------------------------------------------------------
// 此文件由工具自动生成，请勿直接修改。
// 生成时间：__DATA_TABLE_CREATE_TIME__
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
#endif
/// <summary>
/// LocalizationMoney
/// </summary>
public class ArrowLocalizationMoney : DataRowBase
{
	private int m_Id = 0;
	/// <summary>
    /// 
    /// </summary>
    public override int Id
    {
        get { return m_Id; }
    }

        /// <summary>
        /// 语言名
        /// </summary>
        public string LanguageName
        {
            get;
            private set;
        }

        /// <summary>
        /// 货币名
        /// </summary>
        public string NameStr
        {
            get;
            private set;
        }

        /// <summary>
        /// 货币符号
        /// </summary>
        public string SymbolStr
        {
            get;
            private set;
        }

        /// <summary>
        /// 汇率换算（对美元）
        /// </summary>
        public float CurrencyConverter
        {
            get;
            private set;
        }

        /// <summary>
        /// 小数精度（保留n位小数）
        /// </summary>
        public int Precision
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string[] Currency
        {
            get;
            private set;
        }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);
            for (int i = 0; i < columnStrings.Length; i++)
            {
                columnStrings[i] = columnStrings[i].Trim(DataTableExtension.DataTrimSeparators);
            }

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            index++;
            LanguageName = columnStrings[index++];
            NameStr = columnStrings[index++];
            SymbolStr = columnStrings[index++];
            CurrencyConverter = float.Parse(columnStrings[index++]);
            Precision = int.Parse(columnStrings[index++]);
            Currency = DataTableExtension.ParseArray<string>(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    LanguageName = binaryReader.ReadString();
                    NameStr = binaryReader.ReadString();
                    SymbolStr = binaryReader.ReadString();
                    CurrencyConverter = binaryReader.ReadSingle();
                    Precision = binaryReader.Read7BitEncodedInt32();
                    Currency = binaryReader.ReadArray<string>();
                }
            }

            return true;
        }

//__DATA_TABLE_PROPERTY_ARRAY__
}
