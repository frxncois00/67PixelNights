using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Survival/Animal Data")]
public class AnimalData : ScriptableObject
{
    public string displayName = "Animal";
    public Sprite[] idleFrames, walkFrames;
    [Min(1)] public float health = 30;
    [Min(.1f)] public float moveSpeed = 1.3f, roamingRadius = 4, walkingSeconds = 4;
    public Vector2 pauseSeconds = new Vector2(1, 3);
    public bool fleeWhenHit = true;
    [Min(.1f)] public float fleeSeconds = 3;
    [Min(1)] public float fleeSpeedMultiplier = 1.8f;
    [Min(.1f)] public float bodyRadius = .3f;
    [Min(1)] public float animationFPS = 8;
    // Farm RPG side-view cows face left in their source art; Tiny Swords sheep face right.
    public bool artworkFacesLeft;
    public Drop[] drops = Array.Empty<Drop>();

    [Serializable]
    public class Drop
    {
        public ItemDefinition item;
        [Min(1)] public int minimum = 1, maximum = 1;
        [Range(0, 1)] public float chance = 1;
    }
}
