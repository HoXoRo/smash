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
/// PhoneTask
/// </summary>
public class PhoneTask : DataRowBase
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
        /// 目标碎片数（任务一）
        /// </summary>
        public int Fragment
        {
            get;
            private set;
        }

        /// <summary>
        /// 看广告手机碎片数
        /// </summary>
        public int FragmentAd
        {
            get;
            private set;
        }

        /// <summary>
        /// 看广告手机碎片CD
        /// </summary>
        public int FragmentAdCD
        {
            get;
            private set;
        }

        /// <summary>
        /// 首日可获得的最大碎片数
        /// </summary>
        public int FragmentFirstDayMax
        {
            get;
            private set;
        }

        /// <summary>
        /// 累计广告次数（任务二）
        /// </summary>
        public int AdNum
        {
            get;
            private set;
        }

        /// <summary>
        /// 广告任务CD
        /// </summary>
        public int AdCD
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
            Fragment = int.Parse(columnStrings[index++]);
            FragmentAd = int.Parse(columnStrings[index++]);
            FragmentAdCD = int.Parse(columnStrings[index++]);
            FragmentFirstDayMax = int.Parse(columnStrings[index++]);
            AdNum = int.Parse(columnStrings[index++]);
            AdCD = int.Parse(columnStrings[index++]);

            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Fragment = binaryReader.Read7BitEncodedInt32();
                    FragmentAd = binaryReader.Read7BitEncodedInt32();
                    FragmentAdCD = binaryReader.Read7BitEncodedInt32();
                    FragmentFirstDayMax = binaryReader.Read7BitEncodedInt32();
                    AdNum = binaryReader.Read7BitEncodedInt32();
                    AdCD = binaryReader.Read7BitEncodedInt32();
                }
            }

            return true;
        }

//__DATA_TABLE_PROPERTY_ARRAY__
}
