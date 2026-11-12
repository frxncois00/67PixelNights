using System;
using UnityEngine;
public class CampfireController : MonoBehaviour {
 [SerializeField,Min(1)] float maxFuel=100;
 [SerializeField,Min(0)] float startingFuel=75,consumptionPerSecond=1;
 [SerializeField,Min(.1f)] float woodFuelValue=25;
 [SerializeField,Min(0)] int refuelCost=1,relightCost=1;
 public float CurrentFuel {get;private set;}
 public float MaxFuel=>Mathf.Max(1,maxFuel);
 public float NormalizedFuel=>CurrentFuel/MaxFuel;
 public bool IsBurning=>CurrentFuel>0;
 public bool IsExtinguished=>!IsBurning;
 public event Action Changed;
 public event Action FuelAdded;
 public event Action<string> Feedback;
 void Awake(){CurrentFuel=Mathf.Clamp(startingFuel,0,MaxFuel);}
 void Update()=>Consume(Time.deltaTime);
 public void Consume(float seconds){if(!IsBurning||seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;SetFuel(CurrentFuel-Mathf.Max(0,consumptionPerSecond)*seconds);}
 void SetFuel(float value){float previous=NormalizedFuel;bool burning=IsBurning;CurrentFuel=Mathf.Clamp(value,0,MaxFuel);Changed?.Invoke();if(burning&&!IsBurning)Notify("CAMPFIRE EXTINGUISHED!");else if(previous>.2f&&NormalizedFuel<=.2f)Notify("CAMPFIRE FUEL LOW!");}
 public bool TryAddFuel(IWoodWallet wallet){if(CurrentFuel>=MaxFuel){Notify("CAMPFIRE FULL!");return false;}if(wallet==null||!wallet.TrySpend(IsBurning?Mathf.Max(0,refuelCost):Mathf.Max(0,relightCost))){Notify("NOT ENOUGH WOOD!");return false;}SetFuel(CurrentFuel+Mathf.Max(.1f,woodFuelValue));FuelAdded?.Invoke();AudioManager.Instance?.PlayCampfireRefuel();Notify("WOOD ADDED!");return true;}
 public bool TryRelight(IWoodWallet wallet)=>IsExtinguished&&TryAddFuel(wallet);
 public void Extinguish()=>SetFuel(0);
 public void ApplyCapacityUpgrade(float capacity){maxFuel=Mathf.Max(MaxFuel,capacity);Changed?.Invoke();}
 public void Notify(string message)=>Feedback?.Invoke(message);
 void OnValidate(){maxFuel=Mathf.Max(1,maxFuel);startingFuel=Mathf.Clamp(startingFuel,0,maxFuel);consumptionPerSecond=Mathf.Max(0,consumptionPerSecond);woodFuelValue=Mathf.Max(.1f,woodFuelValue);refuelCost=Mathf.Max(0,refuelCost);relightCost=Mathf.Max(0,relightCost);}
}

