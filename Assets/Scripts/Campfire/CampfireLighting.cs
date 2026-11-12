using UnityEngine;
using UnityEngine.Rendering.Universal;
public class CampfireLighting : MonoBehaviour {
 public CampfireController fire;public Light2D fireLight;public SpriteRenderer originalFlame,flameVisual;public Sprite[] frames;
 [SerializeField] float maximumIntensity=1.5f,minimumIntensity=.4f,maximumRadius=5,minimumRadius=2,response=5,framesPerSecond=8;
 float animationTime;bool originalEnabled;
 public bool nativeLevelVisual;
 void OnEnable(){if(!fire||!fireLight||!originalFlame||!flameVisual){Debug.LogError("CampfireLighting missing reference",this);enabled=false;return;}originalEnabled=originalFlame.enabled;originalFlame.enabled=false;}
 void OnDisable(){if(originalFlame)originalFlame.enabled=originalEnabled;if(flameVisual)flameVisual.enabled=false;if(fireLight)fireLight.intensity=0;}
 void LateUpdate()=>Refresh(Time.deltaTime);
 public void Refresh(float delta){if(!fire||!fireLight||!flameVisual)return;float amount=fire.NormalizedFuel;float target=fire.IsBurning?Mathf.Lerp(minimumIntensity,maximumIntensity,amount):0;fireLight.intensity=fire.IsBurning?Mathf.Lerp(fireLight.intensity,target,1-Mathf.Exp(-Mathf.Max(.1f,response)*delta)):0;fireLight.pointLightOuterRadius=Mathf.Lerp(minimumRadius,maximumRadius,amount);if(nativeLevelVisual)return;flameVisual.enabled=fire.IsBurning;flameVisual.color=new Color(1,1,1,Mathf.Lerp(.4f,1,amount));if(frames!=null&&frames.Length>0){animationTime+=delta;flameVisual.sprite=frames[(int)(animationTime*framesPerSecond)%frames.Length];}}
}

