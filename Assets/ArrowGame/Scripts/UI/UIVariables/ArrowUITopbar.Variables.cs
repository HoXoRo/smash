//---------------------------------
//此文件由工具自动生成,请勿手动修改
//更新自:BF-202608261653
//---------------------------------
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Coffee.UIExtensions;
public partial class ArrowUITopbar
{
	[Space(10)]
	[Header("UI Variables:")]
	[SerializeField] protected TextMeshProUGUI varTxtCoin = null;
	[SerializeField] protected TextMeshProUGUI varTxtGem = null;
	[SerializeField] protected Button varBtnCoin = null;
	[SerializeField] private Button varBtnMenu = null;
	[SerializeField] private Image varBg = null;
	[SerializeField] private RectTransform varIcon_Coin = null;
	[SerializeField] private RectTransform varIcon_Gem = null;
	[SerializeField] private Button varGembutton = null;
	[SerializeField] private GameObject varGem = null;
	[SerializeField] private GameObject varCoin = null;
	[SerializeField] private Image varTopdi = null;
	[SerializeField] private GameObject varLvobj = null;
	[SerializeField] private TextMeshProUGUI varLv = null;
	[SerializeField] private UIParticle varCpEff = null;
	[SerializeField] private Image varSkin_Button = null;
	[SerializeField] private Image varBgSkin_Button = null;
	[SerializeField] private Image[] varButtonBgArr = null;
	[SerializeField] private GameObject varGuideSpine = null;
	[SerializeField] private TextMeshProUGUI varTxtUpGem = null;
	[SerializeField] private GameObject varPcard = null;
	[SerializeField] private TextMeshProUGUI varTxtPcard = null;
	[SerializeField] private TextMeshProUGUI varTxtUpPcard = null;
	[SerializeField] private GameObject varGift = null;
	[SerializeField] private TextMeshProUGUI varTxtGiftCount = null;
	[SerializeField] private Button varBtnDebug = null;
	[SerializeField] private Button varBtnLife = null;
	[SerializeField] private TextMeshProUGUI varTxtLife = null;
	[SerializeField] private GameObject varLife = null;
	[SerializeField] private TextMeshProUGUI varTxtTime = null;
}
