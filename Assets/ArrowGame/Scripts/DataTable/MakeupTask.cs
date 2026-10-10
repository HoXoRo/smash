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
/// MakeupTask
/// </summary>
public class MakeupTask : DataRowBase
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
        /// 关卡数
        /// </summary>
        public int Level
        {
            get;
            private set;
        }

        /// <summary>
        /// 美元值
        /// </summary>
        public float Mon
        {
            get;
            private set;
        }

        /// <summary>
        /// 奖励值
        /// </summary>
        public float Reward
        {
            get;
            private set;
        }

        /// <summary>
        /// 倍率
        /// </summary>
        public float Rate
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
            Level = int.Parse(columnStrings[index++]);
            Mon = float.Parse(columnStrings[index++]);
            Reward = float.Parse(columnStrings[index++]);
            Rate = float.Parse(columnStrings[index++]);
            index++;

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Level = binaryReader.Read7BitEncodedInt32();
                    Mon = binaryReader.ReadSingle();
                    Reward = binaryReader.ReadSingle();
                    Rate = binaryReader.ReadSingle();
                }
            }

            return true;
        }

//__DATA_TABLE_PROPERTY_ARRAY__
}
