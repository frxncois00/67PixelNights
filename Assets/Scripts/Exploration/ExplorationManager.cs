using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ExplorationManager : MonoBehaviour {
 [Serializable] public class Region {public string id,title;public Rect bounds;}
 public Transform player;public Text label;public Region[] regions;
 readonly HashSet<string> discovered=new HashSet<string>();
 public int DiscoveredCount=>discovered.Count;public string CurrentRegion {get;private set;}="Campsite";
 string notice;float until;
 void Awake(){discovered.Add("camp");}
 public bool Discover(string id){if(regions==null)return false;foreach(var r in regions)if(r!=null&&r.id==id&&!string.IsNullOrWhiteSpace(id)){CurrentRegion=r.title;if(!discovered.Add(id))return false;notice=r.title.ToUpperInvariant()+" DISCOVERED!";until=Time.time+3;return true;}return false;}
 void Update(){if(!player||regions==null)return;Region best=null;float bestArea=float.MaxValue;foreach(var r in regions){if(r!=null&&r.bounds.Contains(player.position)){float a=Mathf.Abs(r.bounds.width*r.bounds.height);if(a<bestArea){bestArea=a;best=r;}}}if(best!=null)Discover(best.id);if(label){label.enabled=!InventoryUI.IsOpen&&!PlayerSurvivalController.InputLocked;int totalRegions=Mathf.Max(1,regions.Length);label.text="<color=#FFD86B>"+(Time.time<until?notice:CurrentRegion)+"</color>\n<size="+Mathf.RoundToInt(label.fontSize*.8f)+">AREAS DISCOVERED: "+DiscoveredCount+" / "+totalRegions+"</size>";}}
}
