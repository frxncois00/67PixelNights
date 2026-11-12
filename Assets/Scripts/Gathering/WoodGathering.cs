using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class WoodGathering : MonoBehaviour {
 public TemporaryResourceWallet wallet;public PlayerGatherAnimation actions;public CampfireInteraction campfire;public WoodSource[] sources;public Text inventoryText,prompt;
 [SerializeField] float reach=1.5f;float noticeTime;string notice;WoodSource nearest;
 void Update(){nearest=null;float distance=reach;foreach(var source in sources){if(!source||source.Depleted||!source.gameObject.activeInHierarchy)continue;float d=Vector2.Distance(transform.position,source.transform.position);if(d<distance){distance=d;nearest=source;}}
 noticeTime-=Time.deltaTime;inventoryText.text="WOOD INVENTORY   "+wallet.Wood+" logs";prompt.text=noticeTime>0?notice:actions.Busy?"Working...":nearest?(nearest.isTree?"[F] Chop tree ("+nearest.HitsRemaining+" hits left)":"[F] Collect wood"):"[F] Gather nearby wood / trees";
 if(Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame&&nearest&&!actions.Busy){var source=nearest;actions.Play(source.isTree,source.transform.position,()=>{if(!source||source.Depleted||Vector2.Distance(transform.position,source.transform.position)>reach)return;int amount=source.Gather();if(amount>0)wallet.AddWood(amount);notice=amount>0?"+"+amount+" WOOD added to inventory":"Chop! "+source.HitsRemaining+" hits remaining";noticeTime=2;},!source.isTree);}}
}
