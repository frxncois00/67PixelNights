using UnityEngine;
public class PlayerDamageFlash : MonoBehaviour {
 public PlayerHealth health;public SpriteRenderer visual;
 Color original;float until;bool flashing;
 void OnEnable(){if(health)health.Damaged+=Flash;}
 void Flash(float amount){if(!visual)return;if(!flashing)original=visual.color;flashing=true;until=Time.time+.18f;visual.color=new Color(1,.35f,.35f);}
 void LateUpdate(){if(flashing&&Time.time>=until)Restore();}
 void Restore(){if(flashing&&visual)visual.color=original;flashing=false;}
 void OnDisable(){if(health)health.Damaged-=Flash;Restore();}
}
