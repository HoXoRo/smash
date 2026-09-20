using GameFramework;
using GameFramework.Event;

/// <summary>
/// 支付信息变更事件
/// </summary>
public class PaymentInfoChangedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(PaymentInfoChangedEventArgs).GetHashCode();
    public override int Id => EventId;
    
    /// <summary>
    /// 变更类型
    /// </summary>
    public PaymentInfoChangeType ChangeType { get; private set; }
    
    /// <summary>
    /// 支付方式
    /// </summary>
    public string Payment { get; private set; }
    
    /// <summary>
    /// 账号ID
    /// </summary>
    public string AccountId { get; private set; }
    
    /// <summary>
    /// 邮箱
    /// </summary>
    public string Email { get; private set; }

    public static PaymentInfoChangedEventArgs Create(PaymentInfoChangeType changeType, string payment = "", string accountId = "", string email = "")
    {
        var instance = ReferencePool.Acquire<PaymentInfoChangedEventArgs>();
        instance.ChangeType = changeType;
        instance.Payment = payment;
        instance.AccountId = accountId;
        instance.Email = email;
        return instance;
    }

    public override void Clear()
    {
        ChangeType = PaymentInfoChangeType.None;
        Payment = string.Empty;
        AccountId = string.Empty;
        Email = string.Empty;
    }
}

/// <summary>
/// 支付信息变更类型
/// </summary>
public enum PaymentInfoChangeType
{
    None = 0,
    PaymentMethod,    // 支付方式变更
    AccountId,        // 账号变更
    Email,           // 邮箱变更
    All              // 全部信息变更
} 