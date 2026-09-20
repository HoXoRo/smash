using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameFramework;
using GameFramework.Event;
using GameFramework.DataTable;
using UnityGameFramework.Runtime;

public static class RemoteTableLoader
{
    public static bool TryLoadConfig(ConfigComponent configComponent, string configName, bool useBytes, object userData)
    {
        if (configComponent == null || !RemoteTableManager.IsRemoteSyncTable(configName))
        {
            return false;
        }

        if (!RemoteTableLocalStore.TryReadBytes(RemoteTableKind.Config, configName, out byte[] bytes))
        {
            return false;
        }

        string assetName = UtilityBuiltin.AssetsPath.GetConfigPath(configName, useBytes);
        return ParseConfigBytes(configComponent, assetName, bytes, userData);
    }

    public static bool TryLoadDataTable(DataTableComponent dataTableComponent, DataTableBase dataTable, string dataTableName, bool useBytes, object userData)
    {
        if (dataTableComponent == null || dataTable == null || !RemoteTableManager.IsRemoteSyncTable(dataTableName))
        {
            return false;
        }

        if (!RemoteTableLocalStore.TryReadBytes(RemoteTableKind.DataTable, dataTableName, out byte[] bytes))
        {
            return false;
        }

        string assetName = UtilityBuiltin.AssetsPath.GetDataTablePath(dataTableName, useBytes);
        try
        {
            if (!dataTable.ParseData(bytes, userData))
            {
                Log.Warning("Parse cached data table failed. Table='{0}'.", dataTableName);
                return false;
            }

            FireDataTableSuccess(assetName, userData);
            return true;
        }
        catch (Exception exception)
        {
            Log.Warning("Load cached data table failed. Table='{0}', Error='{1}'.", dataTableName, exception.Message);
            return false;
        }
    }

    private static bool ParseConfigBytes(ConfigComponent configComponent, string assetName, byte[] bytes, object userData)
    {
        try
        {
            if (!configComponent.ParseData(bytes, userData))
            {
                Log.Warning("Parse cached config failed. Asset='{0}'.", assetName);
                return false;
            }

            FireConfigSuccess(assetName, userData);
            return true;
        }
        catch (Exception exception)
        {
            Log.Warning("Load cached config failed. Asset='{0}', Error='{1}'.", assetName, exception.Message);
            return false;
        }
    }

    private static void FireConfigSuccess(string assetName, object userData)
    {
        EventComponent eventComponent = GameEntry.GetComponent<EventComponent>();
        if (eventComponent == null)
        {
            return;
        }

        ReadDataSuccessEventArgs readDataSuccessEventArgs = ReadDataSuccessEventArgs.Create(assetName, 0f, userData);
        eventComponent.Fire(null, LoadConfigSuccessEventArgs.Create(readDataSuccessEventArgs));
        ReferencePool.Release(readDataSuccessEventArgs);
    }

    private static void FireDataTableSuccess(string assetName, object userData)
    {
        EventComponent eventComponent = GameEntry.GetComponent<EventComponent>();
        if (eventComponent == null)
        {
            return;
        }

        ReadDataSuccessEventArgs readDataSuccessEventArgs = ReadDataSuccessEventArgs.Create(assetName, 0f, userData);
        eventComponent.Fire(null, LoadDataTableSuccessEventArgs.Create(readDataSuccessEventArgs));
        ReferencePool.Release(readDataSuccessEventArgs);
    }
}
