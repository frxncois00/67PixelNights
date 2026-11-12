using UnityEngine;
using UnityEngine.UI;
public class CampfireUI : MonoBehaviour {
 public CampfireController fire;public TemporaryResourceWallet wallet;public CampfireUpgrade upgrade;public CampfireInteraction interaction;
 public Text fuelText,woodText,statusText,levelText,promptText,feedbackText;public Image fuelBar;
 public Color high=new Color(1,.75f,.2f),medium=new Color(1,.4f,.1f),low=Color.red,empty=Color.gray;
 [Header("Optional styling")] public Image statusIcon;
 float messageTime;
 void OnEnable(){if(!fire||!wallet||!upgrade||!interaction||!fuelText||!woodText||!statusText||!levelText||!promptText||!feedbackText||!fuelBar){Debug.LogError("CampfireUI missing reference",this);enabled=false;return;}fire.Changed+=Refresh;wallet.Changed+=Refresh;upgrade.Changed+=Refresh;fire.Feedback+=Message;Refresh();}
 void OnDisable(){if(fire){fire.Changed-=Refresh;fire.Feedback-=Message;}if(wallet)wallet.Changed-=Refresh;if(upgrade)upgrade.Changed-=Refresh;}
 public void Refresh(){fuelText.text="Fuel: "+Mathf.CeilToInt(fire.CurrentFuel)+" / "+fire.MaxFuel.ToString("0");woodText.text="WOOD: "+wallet.Wood;levelText.text="LEVEL "+upgrade.CurrentLevel+(upgrade.IsMaxLevel?"  MAX LEVEL":"  [U] Upgrade: "+upgrade.NextCost+" wood");statusText.text=fire.IsExtinguished?"EMPTY / EXTINGUISHED":fire.NormalizedFuel<=.2f?"LOW FUEL":"BURNING";fuelBar.fillAmount=fire.NormalizedFuel;fuelBar.color=fire.IsExtinguished?empty:fire.NormalizedFuel<=.2f?low:fire.NormalizedFuel<=.5f?medium:high;if(statusIcon){statusIcon.color=fuelBar.color;statusText.color=fuelBar.color;}}
 void Message(string text){feedbackText.text=text;messageTime=3;}
 void Update(){if(messageTime>0){messageTime-=Time.deltaTime;if(messageTime<=0)feedbackText.text="";}}
}
