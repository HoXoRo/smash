using GameFramework.DataTable;

[System.Serializable]
public class MakeupData
{
    public int id;
    public int supplyNeed;
    public string payment;
    public MakeupStep makeupStep = MakeupStep.None;

    public int step1TaskNum;
    public int step2TaskNum;
    public float step2OriginVlue;
    /// <summary>
    /// Step2 累计里程碑上报标记（bit0~4 对应 500/750/900/950/1000）。
    /// </summary>
    public int step2CompleteReported;


    public long makeupTimestamp;
    public MakeupType makeupType;
    public MakeupPaymentInfo paymentInfo;
    public MakeupData()
    {

    }


    public MakeupData(ArrowMakeupBanknotes drMakeupBanknote)
    {
        makeupType = MakeupType.Mon;
        id = drMakeupBanknote.Id;
        supplyNeed = drMakeupBanknote.Step1_Lv;
        if(!string.IsNullOrEmpty(GF.DataModel.GetDataModel<PlayerDataModel>().Payment)&&paymentInfo==null)
        {
            payment = GF.DataModel.GetDataModel<PlayerDataModel>().Payment;
        }
        paymentInfo = new MakeupPaymentInfo();
        makeupTimestamp = CommonHelper.GetNowTime();
        step1TaskNum = 0;
        
        TurnNextStep();
    }

    public void TurnNextStep()
    {
        int step = (int)makeupStep;
        if (step < 6)
        {
            step++;
            makeupStep = (MakeupStep)step;
            if (makeupStep == MakeupStep.Step2)
            {
                CommonHelper.LogEvent(AdjustEventCodeEvent.withdraw_step2_enter);
                step2OriginVlue = GF.DataModel.GetDataModel<PlayerDataModel>().Dollars;
            }
        }

        GF.Event.Fire(this, MakeupDataChangedEventArgs.Create(MakeupDataChangeType.Updated));
        GF.DataModel.GetDataModel<PlayerDataModel>().Save();
    }
}
[System.Serializable]
public class MakeupPaymentInfo
{
    #region 必填信息
    // 支付方式
    public string payment;
    // 账号ID
    public string accountId;
    // 邮箱
    public string email;
    #endregion

    #region 可选信息
    // 手机号
    public string phone;
    // 姓名
    public string name;
    // 地址
    public string address;
    // 邮编
    public string zipCode;
    // 国家
    public string country;
    // 省份
    public string province;
    // 城市
    public string city;
    // 身份证
    public string cpf;
#endregion
}
public enum MakeupStep
{
    None = 0,
    Step1,
    Step2,
    Fail,
    Success
}