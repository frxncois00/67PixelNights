using System;
using System.Text;
using UnityEngine;
public class LootChest : InteractionTarget {
 [Serializable] public class Reward {public ItemDefinition item;[Min(0)] public int minimum=1,maximum=3;[HideInInspector] public int remaining;}
 public Reward[] rewards;public SpriteRenderer visual;public Transform lid;
 public ChestOpeningAnimation openingAnimation;
 [SerializeField] bool rolled,empty;
 bool transferring;
 ItemDefinition looseLog;
 public bool Opened=>rolled;public bool Empty=>empty;
 public int Remaining {get{int n=0;if(rewards!=null)foreach(var r in rewards)if(r!=null)n+=r.remaining;return n;}}
 public override bool Available=>!empty;
 public override string Prompt=>looseLog?"[E] Pick up 1 Wood Log":rolled?"[E] Collect remaining chest loot":"[E] Open Chest";
 protected override void OnEnable(){
  base.OnEnable();
  if(!openingAnimation)openingAnimation=GetComponent<ChestOpeningAnimation>();
  // Legacy map caches used the wood item sprite. Treat those as loose timber,
  // while preserving chest references used by encounter and cabin scripts.
  looseLog=null;
  var closedSprite=openingAnimation&&openingAnimation.closedBodySprite?openingAnimation.closedBodySprite:visual?visual.sprite:null;
  if(rewards!=null)foreach(var reward in rewards)
   if(reward!=null&&reward.item&&reward.item.Id=="wood_log"&&reward.item.Icon&&closedSprite==reward.item.Icon){looseLog=reward.item;break;}
  if(looseLog){
   if(openingAnimation){openingAnimation.SetState(false,false);openingAnimation.enabled=false;}
   if(visual){visual.sprite=looseLog.Icon;visual.color=Color.white;}
   if(lid)lid.gameObject.SetActive(false);
  }else if(openingAnimation)openingAnimation.SetState(rolled,empty);
 }
 public override void Use(PlayerInteractionController p){if(transferring||!p||PlayerInteractionController.InputBlocked||p.actions.Busy||!Reachable(p.transform.position))return;var inv=p.GetComponent<InventoryController>();if(!inv)return;
  if(looseLog){
   if(empty)return;
   int accepted=inv.Add(looseLog,1);
   if(accepted==0){p.Notice("INVENTORY FULL!");return;}
   rolled=true;empty=true;
   p.Notice("+1 LOG");
   gameObject.SetActive(false);
   Destroy(gameObject);
   return;
  }
  bool firstOpen=!rolled;
  transferring=true;try{if(firstOpen){rolled=true;if(rewards!=null)foreach(var r in rewards)if(r!=null&&r.item)r.remaining=UnityEngine.Random.Range(Mathf.Clamp(r.minimum,0,99),Mathf.Clamp(r.maximum,Mathf.Clamp(r.minimum,0,99),99)+1);if(lid&&!openingAnimation)lid.localRotation=Quaternion.Euler(0,0,25);}
   var text=new StringBuilder();if(rewards!=null)foreach(var r in rewards){if(r==null||!r.item||r.remaining<=0)continue;int accepted=inv.Add(r.item,r.remaining);r.remaining-=accepted;if(accepted>0)text.Append("+"+accepted+" "+r.item.DisplayName+"  ");}
   empty=Remaining==0;if(openingAnimation){if(firstOpen)openingAnimation.PlayOpen(empty);else openingAnimation.SetEmpty(empty);}else if(visual&&empty)visual.color=new Color(.55f,.55f,.55f);p.Notice((text.Length>0?text.ToString():empty?"CHEST EMPTY":"INVENTORY FULL")+(empty?"":" — LOOT REMAINS IN CHEST"));
  }finally{transferring=false;}
 }
}
