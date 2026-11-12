using UnityEngine;
public class CampfireSafeZone : MonoBehaviour {
 public CampfireController fire;public Transform player;
 [SerializeField,Min(.1f)] float radius=5;
 public bool IsSafeZoneActive=>isActiveAndEnabled&&fire&&fire.IsBurning;
 public float CurrentSafeRadius=>Mathf.Max(.1f,radius);
 public bool IsPlayerSafe=>player&&IsPositionSafe(player.position);
 public bool IsPositionSafe(Vector2 position)=>IsSafeZoneActive&&((Vector2)transform.position-position).sqrMagnitude<=CurrentSafeRadius*CurrentSafeRadius;
 public void SetRadius(float value){radius=Mathf.Max(.1f,value);}
 void Start(){if(!fire||!player)Debug.LogError("CampfireSafeZone needs fire and Player references",this);}
 void OnDrawGizmosSelected(){Gizmos.color=IsSafeZoneActive?Color.green:Color.gray;Vector3 previous=transform.position+Vector3.right*CurrentSafeRadius;for(int i=1;i<=64;i++){float a=i*Mathf.PI*2/64;Vector3 next=transform.position+new Vector3(Mathf.Cos(a),Mathf.Sin(a))*CurrentSafeRadius;Gizmos.DrawLine(previous,next);previous=next;}}
}
