using System;
using UnityEngine;
public class CampfireUpgrade : MonoBehaviour {
 [Serializable] public class Level {public float capacity,radius;public int cost;public Level(float c,float r,int w){capacity=c;radius=r;cost=w;}}
 public CampfireController fire;public CampfireSafeZone safeZone;
 [SerializeField] Level[] levels={new Level(100,5,0),new Level(150,7,5),new Level(200,9,10)};
 public int CurrentLevel {get;private set;}=1;
 public bool IsMaxLevel=>CurrentLevel>=levels.Length;
 public int NextCost=>IsMaxLevel?0:levels[CurrentLevel].cost;
 public event Action Changed;
 bool Valid(){if(levels==null||levels.Length==0)return false;for(int i=0;i<levels.Length;i++)if(levels[i]==null||levels[i].capacity<=0||levels[i].radius<=0||levels[i].cost<0||(i>0&&(levels[i].capacity<=levels[i-1].capacity||levels[i].radius<=levels[i-1].radius)))return false;return true;}
 void Start(){if(!fire||!safeZone||!Valid()){Debug.LogError("Invalid campfire upgrade configuration",this);enabled=false;return;}fire.ApplyCapacityUpgrade(levels[0].capacity);safeZone.SetRadius(levels[0].radius);}
 public bool TryUpgrade(IWoodWallet wallet){if(!fire||!safeZone||!Valid())return false;if(IsMaxLevel){fire.Notify("MAXIMUM CAMPFIRE LEVEL!");return false;}if(wallet==null||!wallet.TrySpend(NextCost)){fire.Notify("NOT ENOUGH WOOD TO UPGRADE!");return false;}var level=levels[CurrentLevel++];fire.ApplyCapacityUpgrade(level.capacity);safeZone.SetRadius(level.radius);Changed?.Invoke();AudioManager.Instance?.PlayCampfireUpgrade();fire.Notify("CAMPFIRE UPGRADED TO LEVEL "+CurrentLevel+"!");return true;}
}
