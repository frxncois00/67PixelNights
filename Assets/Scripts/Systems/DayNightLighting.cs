using UnityEngine;
using UnityEngine.Rendering.Universal;
public class DayNightLighting : MonoBehaviour {
 public DayNightCycle cycle; public Light2D globalLight;
 public float dayIntensity=1,nightIntensity=.2f;
 public Color dayColor=Color.white,nightColor=new Color(.42f,.5f,.72f),sunsetColor=new Color(1,.55f,.25f),sunriseColor=new Color(1,.85f,.55f);
 void OnEnable(){if(!cycle||!globalLight){Debug.LogError("DayNightLighting requires cycle and Global Light 2D.",this);enabled=false;return;}Apply();}
 void Update()=>Apply();
 public void Apply(){if(!cycle||!globalLight)return;float t=cycle.TransitionDuration>0?Mathf.Clamp01(1-cycle.TimeRemaining/cycle.TransitionDuration):0;t=Mathf.SmoothStep(0,1,t);Color from=cycle.IsDaytime?dayColor:nightColor,to=cycle.IsDaytime?nightColor:dayColor,mid=cycle.IsDaytime?sunsetColor:sunriseColor;globalLight.color=t<.5f?Color.Lerp(from,mid,t*2):Color.Lerp(mid,to,(t-.5f)*2);globalLight.intensity=Mathf.Lerp(cycle.IsDaytime?dayIntensity:nightIntensity,cycle.IsDaytime?nightIntensity:dayIntensity,t);}
}
