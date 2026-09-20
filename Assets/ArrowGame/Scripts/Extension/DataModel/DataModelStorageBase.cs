
using System;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 数据模型, 可持久化保存
/// </summary>
public abstract class DataModelStorageBase : DataModelBase
{
    protected string StorageKey { get; private set; } = null;
    public DataModelStorageBase()
    {
        StorageKey = this.GetType().FullName;
    }
    protected override void OnCreate(RefParams userdata)
    {
        base.OnCreate(userdata);
        Load();
    }

    protected override void OnRelease()
    {
        Save();
    }

    private void Load()
    {
        if (Id != 0)
        {
            OnInitialDataModel();
            return;
        }
        string dataJson = GF.Setting.GetString(StorageKey, null);
        if (!string.IsNullOrEmpty(dataJson))
        {
            try
            {
                string decryptData = UtilityBuiltin.DES.Decrypt(dataJson);
                // Log.Info($"读取数据:{StorageKey}, {string.Join(",",decryptData)}");

                Newtonsoft.Json.JsonConvert.PopulateObject(decryptData, this);
            }
            catch (Exception e)
            {
                PlayerPrefs.DeleteKey(StorageKey);
                Log.Error("DataModelStorageBase Load Error: " + e.Message);
            }
        }
        else
        {
            OnInitialDataModel();
        }
    }
    /// <summary>
    /// 从没有本地储存数据时, 回调此方法, 用于初始化变量
    /// </summary>
    protected virtual void OnInitialDataModel() { }

    public void Save(bool isSave = true)
    {
        if (Id != 0) return;
        if (isSave)
        {
            string dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            if (!string.IsNullOrEmpty(dataJson))
            {
                Log.Info($"保存数据: {StorageKey},{string.Join(",",dataJson)}");
                string encryptData = UtilityBuiltin.DES.Encrypt(dataJson);
                GF.Setting.SetString(StorageKey, encryptData);
                GF.Setting.Save();
            }
        }
        else
        {
            // string dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            // if (!string.IsNullOrEmpty(dataJson))
            // {
            //     Log.Info($"保存数据: {string.Join(",",dataJson)}");
            // }
        }
    }
}
