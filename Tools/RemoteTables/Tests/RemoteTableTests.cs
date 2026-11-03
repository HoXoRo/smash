// Standalone harness: real sync/store/manager/converter, simulated Unity transport and runtime.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace Cysharp.Threading.Tasks
{
    public enum PlayerLoopTiming { Update }
    [AsyncMethodBuilder(typeof(TestTaskBuilder))]
    public struct UniTask
    {
        internal Task Task;
        public TaskAwaiter GetAwaiter() => Task.GetAwaiter();
        public static Task Yield(PlayerLoopTiming _) => System.Threading.Tasks.Task.Delay(1);
    }
    [AsyncMethodBuilder(typeof(TestTaskBuilder<>))]
    public struct UniTask<T> { internal Task<T> Task; public TaskAwaiter<T> GetAwaiter() => Task.GetAwaiter(); }
    public struct TestTaskBuilder
    {
        private AsyncTaskMethodBuilder inner;
        public static TestTaskBuilder Create() => new TestTaskBuilder { inner = AsyncTaskMethodBuilder.Create() };
        public UniTask Task => new UniTask { Task = inner.Task };
        public void SetResult() => inner.SetResult();
        public void SetException(Exception e) => inner.SetException(e);
        public void SetStateMachine(IAsyncStateMachine s) => inner.SetStateMachine(s);
        public void Start<T>(ref T s) where T:IAsyncStateMachine => inner.Start(ref s);
        public void AwaitOnCompleted<T,U>(ref T a, ref U s) where T:INotifyCompletion where U:IAsyncStateMachine => inner.AwaitOnCompleted(ref a, ref s);
        public void AwaitUnsafeOnCompleted<T,U>(ref T a, ref U s) where T:ICriticalNotifyCompletion where U:IAsyncStateMachine => inner.AwaitUnsafeOnCompleted(ref a, ref s);
    }
    public struct TestTaskBuilder<T>
    {
        private AsyncTaskMethodBuilder<T> inner;
        public static TestTaskBuilder<T> Create() => new TestTaskBuilder<T> { inner = AsyncTaskMethodBuilder<T>.Create() };
        public UniTask<T> Task => new UniTask<T> { Task = inner.Task };
        public void SetResult(T value) => inner.SetResult(value);
        public void SetException(Exception e) => inner.SetException(e);
        public void SetStateMachine(IAsyncStateMachine s) => inner.SetStateMachine(s);
        public void Start<U>(ref U s) where U:IAsyncStateMachine => inner.Start(ref s);
        public void AwaitOnCompleted<U,V>(ref U a, ref V s) where U:INotifyCompletion where V:IAsyncStateMachine => inner.AwaitOnCompleted(ref a, ref s);
        public void AwaitUnsafeOnCompleted<U,V>(ref U a, ref V s) where U:ICriticalNotifyCompletion where V:IAsyncStateMachine => inner.AwaitUnsafeOnCompleted(ref a, ref s);
    }
}
namespace UnityEngine
{
    public static class Application { public static string persistentDataPath, identifier = "com.test.game", version = "1.0"; }
    public static class Mathf { public static float Clamp01(float n) => Math.Max(0, Math.Min(1, n)); }
}
namespace UnityGameFramework.Runtime { public static class Log { public static void Info(string s, params object[] a) {} public static void Warning(string s, params object[] a) { Console.WriteLine("  " + string.Format(s,a)); } } }
namespace GameFramework
{
    public class GameFrameworkException:Exception { public GameFrameworkException(string s):base(s) {} }
    public static class Utility { public static class Text { public static string Format(string s,params object[] a)=>string.Format(s,a); } }
    public static class BinaryExtensions
    {
        public static int Read7BitEncodedInt32(this BinaryReader r)
        {
            uint value=0;
            for(int shift=0;shift<35;shift+=7) { byte b=r.ReadByte(); if(shift==28 && b>15) throw new FormatException(); value |= (uint)(b&127)<<shift; if(b<128) return (int)value; }
            throw new FormatException();
        }
    }
}
namespace GameFramework.DataTable
{
    public abstract class DataRowBase { public abstract int Id {get;} public abstract bool ParseDataRow(byte[] b,int start,int length,object data); }
}
public class TestRow:GameFramework.DataTable.DataRowBase
{
    private int id; public override int Id=>id;
    public override bool ParseDataRow(byte[] b,int start,int length,object data) { if(length!=1) return false; id=b[start]; return true; }
}
public class AppConfigs { public string[] Configs,DataTables; }
public class AppSettings
{
    public static AppSettings Instance=new AppSettings();
    public bool EnableRemoteTableSync=true;
    public string RemoteTableBaseUrl="https://test.invalid";
    public int RemoteTableSyncTimeoutSeconds=10;
}
public static class RemoteTableCrypto { public static string DecryptPayload(string text) => Encoding.UTF8.GetString(Convert.FromBase64String(text)); }
namespace UnityEngine.Networking
{
    public class UploadHandlerRaw { public byte[] data; public UploadHandlerRaw(byte[] b) {data=b;} }
    public class DownloadHandlerBuffer { public byte[] data; }
    public class UnityWebRequestAsyncOperation { public bool isDone=true; }
    public class UnityWebRequest:IDisposable
    {
        public enum Result { Success,ConnectionError }
        public static Queue<Action<UnityWebRequest>> Responses=new Queue<Action<UnityWebRequest>>();
        public static List<UnityWebRequest> Requests=new List<UnityWebRequest>();
        public string url,method,error,configId; public int timeout; public long responseCode=200; public float downloadProgress=1;
        public Result result=Result.Success; public bool stall,aborted;
        public UploadHandlerRaw uploadHandler; public DownloadHandlerBuffer downloadHandler=new DownloadHandlerBuffer();
        public UnityWebRequest(string u,string m){url=u;method=m;}
        public static UnityWebRequest Get(string u)=>new UnityWebRequest(u,"GET");
        public void SetRequestHeader(string k,string v) {if(k!="Content-Type" || v!="application/json") throw new Exception("header");}
        public string GetResponseHeader(string key)=>key=="X-Config-Id"?configId:null;
        public UnityWebRequestAsyncOperation SendWebRequest(){Requests.Add(this); if(Responses.Count==0) throw new Exception("Unexpected request: "+url); Responses.Dequeue()(this); return new UnityWebRequestAsyncOperation {isDone=!stall};}
        public void Abort(){aborted=true;}
        public void Dispose(){}
    }
}
class RemoteTableTests
{
    static int count;
    static void Check(bool ok,string label){if(!ok) throw new Exception(label); count++; Console.WriteLine("PASS "+label);}
    static void Reject(Action action,string label){try{action();}catch(Exception){Check(true,label);return;}throw new Exception("Expected rejection: "+label);}
    static byte[] Utf8(string s)=>Encoding.UTF8.GetBytes(s);
    static RemoteTableManifest Manifest(params (string path,byte[] bytes)[] files)
    {
        var m=new RemoteTableManifest {Version="1"}; int i=0;
        foreach(var f in files) m.Files.Add("arbitrary-server-key-"+(i++),new RemoteTableManifestEntry {Version="1",Path=f.path,Size=f.bytes.Length,Md5=RemoteTableManager.ComputeMd5(f.bytes)});
        return m;
    }
    static void Resolve(long id,RemoteTableManifest m){UnityWebRequest.Responses.Enqueue(r=>{r.configId=id.ToString();r.downloadHandler.data=Utf8(JsonConvert.SerializeObject(m));});}
    static void FileResponse(byte[] data){UnityWebRequest.Responses.Enqueue(r=>r.downloadHandler.data=data);}
    static void Sync()=>RemoteTableManager.SyncAsync().GetAwaiter().GetResult();
    static string Batch()=>RemoteTableLocalStore.LoadActive()?.Batch;
    static void Restart(){typeof(RemoteTableLocalStore).GetField("s_LoadedRoot",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,null);}
    public static void Main()
    {
        string testRoot=Path.Combine(Path.GetTempPath(),"remote-tables-test-"+Guid.NewGuid().ToString("N"));
        UnityEngine.Application.persistentDataPath=testRoot;
        RemoteTableManager.EnsureInitialized(new AppConfigs {Configs=new[]{"GameConfig","Extra"},DataTables=new[]{"Core/TestRow"}});
        try
        {
            byte[] first=Utf8("{\"price\":1}"), second=Utf8("{\"price\":2}"), extra=Utf8("{\"other\":3}");
            var one=Manifest(("config/GameConfig.json",first));
            Resolve(101,one);FileResponse(first);Sync();
            string original=Batch();
            Check(original!=null && RemoteTableLocalStore.LoadAssignedConfigId()==101,"first assignment and batch activation");
            Check(UnityWebRequest.Requests[0].method=="POST" && JObject.Parse(Encoding.UTF8.GetString(UnityWebRequest.Requests[0].uploadHandler.data))["config_id"].Type==JTokenType.Null,"first resolve has no prior assignment");
            Check(UnityWebRequest.Requests[1].url=="https://test.invalid/api/public/config-folders/101/config/GameConfig.json","file URL uses response config ID and path");
            Resolve(101,one);int before=UnityWebRequest.Requests.Count;Sync();
            Check(UnityWebRequest.Requests.Count==before+1 && Batch()==original,"unchanged MD5 does not download or switch batch");
            Check((long)JObject.Parse(Encoding.UTF8.GetString(UnityWebRequest.Requests.Last().uploadHandler.data))["config_id"]==101,"subsequent resolve keeps assignment");
            var two=Manifest(("config/GameConfig.json",second),("config/Extra.json",extra));
            Resolve(202,two);FileResponse(second);UnityWebRequest.Responses.Enqueue(r=>{r.result=UnityWebRequest.Result.ConnectionError;r.responseCode=503;});Sync();
            Check(Batch()==original && RemoteTableLocalStore.LoadAssignedConfigId()==202,"partial download failure preserves active batch but saves new assignment");
            Resolve(202,two);FileResponse(second);FileResponse(extra);Sync();
            Check(Batch()!=original && RemoteTableLocalStore.LoadActive().Manifest.Files.Count==2,"complete batch switches together with fixed version 1");
            string complete=Batch();
            Resolve(202,Manifest(("config/GameConfig.json",first)));FileResponse(second);Sync();
            Check(Batch()==complete,"hash mismatch never switches active batch");
            Resolve(202,Manifest(("config/GameConfig.json",second)));Sync();
            Check(!RemoteTableLocalStore.TryReadBytes(RemoteTableKind.Config,"Extra",out _),"removed table does not survive in new batch");
            AppSettings.Instance.EnableRemoteTableSync=false;before=UnityWebRequest.Requests.Count;Sync();
            Check(UnityWebRequest.Requests.Count==before && !RemoteTableLocalStore.TryReadBytes(RemoteTableKind.Config,"GameConfig",out _),"disabled remote feature bypasses transport and cache");
            AppSettings.Instance.EnableRemoteTableSync=true;
            string activeFile=RemoteTableLocalStore.GetBatchFile(Batch(),"GameConfig");System.IO.File.WriteAllBytes(activeFile,new byte[]{0});Restart();
            Check(RemoteTableLocalStore.LoadActive()==null,"corrupt member invalidates whole snapshot");
            Resolve(202,Manifest(("config/GameConfig.json",second)));FileResponse(second);Sync();
            Check(Batch()!=null,"missing/corrupt bytes redownload despite unchanged manifest hash");
            complete=Batch();UnityWebRequest.Responses.Enqueue(r=>{r.downloadHandler.data=Utf8(JsonConvert.SerializeObject(one));});Sync();
            Check(Batch()==complete && RemoteTableLocalStore.LoadAssignedConfigId()==202,"missing response ID leaves state unchanged");
            UnityWebRequest.Responses.Enqueue(r=>{r.configId="999";r.downloadHandler.data=Utf8("{}");});Sync();
            Check(Batch()==complete && RemoteTableLocalStore.LoadAssignedConfigId()==202,"malformed manifest cannot clear active tables or assignment");
            byte[] bom=new byte[]{239,187,191}.Concat(first).ToArray();Resolve(202,Manifest(("config/GameConfig.json",bom)));FileResponse(bom);Sync();
            Check(Batch()!=complete,"BOM is included in raw MD5 then stripped before parsing");
            complete=Batch();Resolve(202,Manifest(("config/GameConfig.json",second)));UnityWebRequest.Responses.Enqueue(r=>r.stall=true);AppSettings.Instance.RemoteTableSyncTimeoutSeconds=1;Sync();
            Check(Batch()==complete && UnityWebRequest.Requests.Last().aborted,"whole-sync timeout aborts download and retains batch");AppSettings.Instance.RemoteTableSyncTimeoutSeconds=10;
            foreach(int status in new[]{404,422,503}) {UnityWebRequest.Responses.Enqueue(r=>{r.result=UnityWebRequest.Result.ConnectionError;r.responseCode=status;});Sync();Check(Batch()==complete && RemoteTableLocalStore.LoadAssignedConfigId()==202,"HTTP "+status+" keeps assignment and active batch");}
            Reject(()=>RemoteTableSyncService.SelectTables(Manifest(("config/../GameConfig.json",first))),"reject path traversal");
            var dup=Manifest(("config/GameConfig.json",first),("config/GameConfig.json",first));Reject(()=>RemoteTableSyncService.SelectTables(dup),"reject duplicate mapped path");
            Check(RemoteTableSyncService.SelectTables(Manifest(("unrelated/file.json",first))).Files.Count==0,"skip unregistered files");
            Check(RemoteTableSyncService.SelectTables(Manifest(("datatable/Core/TestRow.json",first))).Files.ContainsKey("Core/TestRow"),"map nested Core paths independent of dictionary key");
            RemoteTableSyncService.ValidateTableBytes(RemoteTableKind.DataTable,"Core/TestRow",new byte[]{1,7,1,9});
            Reject(()=>RemoteTableSyncService.ValidateTableBytes(RemoteTableKind.DataTable,"Core/TestRow",new byte[]{1,7,1,7}),"reject duplicate row IDs before activation");
            Reject(()=>RemoteTableSyncService.ValidateTableBytes(RemoteTableKind.DataTable,"Core/TestRow",new byte[]{9,7}),"reject truncated row bytes before activation");
            string scope=RemoteTableLocalStore.RootPath;UnityEngine.Application.version="2.0";
            Check(RemoteTableLocalStore.RootPath!=scope && RemoteTableLocalStore.LoadAssignedConfigId()==null && RemoteTableLocalStore.LoadActive()==null,"app versions isolate IDs and caches");
            UnityEngine.Application.version="1.0";AppSettings.Instance.RemoteTableBaseUrl="https://staging.invalid";
            Check(RemoteTableLocalStore.RootPath!=scope,"API environments isolate cache scope");
            Check(UnityWebRequest.Responses.Count==0,"all expected HTTP responses consumed");
            Console.WriteLine("SUCCESS: "+count+" regression checks.");
        }
        finally {if(Directory.Exists(testRoot)) Directory.Delete(testRoot,true);}
    }
}
