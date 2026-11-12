using UnityEngine;
using UnityEngine.UI;
public class DayNightUI : MonoBehaviour {
 public DayNightCycle cycle;public Text dayCounterText,phaseText,timerText,notificationText;public Image progress;public CanvasGroup notification;
 public Color dayBar=new Color(1,.75f,.25f),nightBar=new Color(.45f,.55f,1);
 [Header("Optional styling")] public Image phaseIcon;public Sprite dayIcon,nightIcon;public Color dayText=new Color(1,.85f,.42f),nightText=new Color(.62f,.72f,1);
 float age=3;int lastSecond=-1;
 void OnEnable(){if(!cycle||!dayCounterText||!phaseText||!timerText||!notificationText||!progress||!notification){Debug.LogError("DayNightUI has missing references",this);enabled=false;return;}cycle.OnPhaseChanged+=Changed;Changed();}
 void OnDisable(){if(cycle)cycle.OnPhaseChanged-=Changed;}
 void Changed(){dayCounterText.text="DAY "+cycle.CurrentDay;phaseText.text=cycle.IsDaytime?"DAYTIME":"NIGHTTIME";notificationText.text=(cycle.IsDaytime?"DAY ":"NIGHT ")+cycle.CurrentDay;if(phaseIcon&&dayIcon&&nightIcon)phaseIcon.sprite=cycle.IsDaytime?dayIcon:nightIcon;phaseText.color=cycle.IsDaytime?dayText:nightText;age=0;lastSecond=-1;}
 void Update(){int seconds=Mathf.CeilToInt(cycle.TimeRemaining);if(seconds!=lastSecond){timerText.text=(seconds/60).ToString("00")+":"+(seconds%60).ToString("00");lastSecond=seconds;}progress.fillAmount=1-cycle.NormalizedPhaseProgress;progress.color=cycle.IsDaytime?dayBar:nightBar;age+=Time.deltaTime;notification.alpha=age<.5f?age/.5f:age<2.5f?1:Mathf.Clamp01((3-age)/.5f);}
}
