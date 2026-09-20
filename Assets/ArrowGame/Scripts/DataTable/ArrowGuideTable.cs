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
/// GuideTable
/// </summary>
public class ArrowGuideTable : DataRowBase
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
        /// 引导类型（1强制引导2弱引导）
        /// </summary>
        public int GuideType
        {
            get;
            private set;
        }

        /// <summary>
        /// 遮罩类型（1透明2黑色半透明）
        /// </summary>
        public int MaskType
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否有手指引导
        /// </summary>
        public bool FingerShow
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否有提示文本
        /// </summary>
        public bool TipsShow
        {
            get;
            private set;
        }

        /// <summary>
        /// 文本位置
        /// </summary>
        public Vector2 TipsPos
        {
            get;
            private set;
        }

        /// <summary>
        /// 文本内容（多语言Key）
        /// </summary>
        public string TipsKey
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
            GuideType = int.Parse(columnStrings[index++]);
            MaskType = int.Parse(columnStrings[index++]);
            FingerShow = bool.Parse(columnStrings[index++]);
            TipsShow = bool.Parse(columnStrings[index++]);
            TipsPos = DataTableExtension.ParseVector2(columnStrings[index++]);
            TipsKey = columnStrings[index++];
            index++;
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
                    GuideType = binaryReader.Read7BitEncodedInt32();
                    MaskType = binaryReader.Read7BitEncodedInt32();
                    FingerShow = binaryReader.ReadBoolean();
                    TipsShow = binaryReader.ReadBoolean();
                    TipsPos = binaryReader.ReadVector2();
                    TipsKey = binaryReader.ReadString();
                }
            }

            return true;
        }

//__DATA_TABLE_PROPERTY_ARRAY__
}
