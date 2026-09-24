
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/HomePageUIForm")]

public partial class HomePageUIForm : UIFormBase
{
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }
    
}
