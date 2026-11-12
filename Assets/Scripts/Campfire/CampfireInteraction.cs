using UnityEngine;
using UnityEngine.InputSystem;
public class CampfireInteraction : InteractionTarget {
 public CampfireController fire;public CampfireUpgrade upgrade;public TemporaryResourceWallet wallet;public Transform player;
 [SerializeField,Min(.1f)] float interactionDistance=2;
 public bool InRange=>isActiveAndEnabled&&player&&fire&&Vector2.Distance(player.position,fire.transform.position)<=Mathf.Max(.1f,interactionDistance);
 void Start(){if(!fire||!upgrade||!wallet||!player){Debug.LogError("CampfireInteraction missing required reference",this);enabled=false;}}
 public bool Refuel()=>!PlayerInteractionController.InputBlocked&&InRange&&fire.TryAddFuel(wallet);
 public bool Upgrade()=>!PlayerInteractionController.InputBlocked&&InRange&&upgrade.TryUpgrade(wallet);
 public PlayerGatherAnimation actions;
 public override float Range => interactionDistance;
 public override bool Available => InRange && wallet && fire.CurrentFuel < fire.MaxFuel;
 public override string Prompt => fire.IsExtinguished ? "[E] Relight campfire   [U] Upgrade" : "[E] Add wood   [U] Upgrade";
 public override void Use(PlayerInteractionController actor) {
     if (PlayerInteractionController.InputBlocked || !Reachable(actor.transform.position) || (actions && actions.Busy)) return;
     if (actions && wallet.Wood>0) actions.Play(false,fire.transform.position,()=>Refuel());
     else Refuel();
 }
 void Update() {
     var keyboard=Keyboard.current;
     if (keyboard!=null && keyboard.uKey.wasPressedThisFrame && InRange &&
         (!actions || !actions.Busy) && !PlayerInteractionController.InputBlocked) Upgrade();
 }
}
