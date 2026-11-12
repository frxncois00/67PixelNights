using System;
using UnityEngine;
public class WoodSource : MonoBehaviour {
 public bool isTree;public int woodYield=1;public int hitsRequired=3;public Sprite stumpSprite;int hits;public bool Depleted {get;private set;}
 public int HitsRemaining=>Mathf.Max(0,hitsRequired-hits);
 public int Gather(){if(Depleted)return 0;AudioManager.Instance?.PlayWoodHit();if(isTree&&++hits<hitsRequired)return 0;Depleted=true;if(isTree){var r=GetComponent<SpriteRenderer>();if(stumpSprite)r.sprite=stumpSprite;}else gameObject.SetActive(false);return Mathf.Max(1,woodYield);}
}
