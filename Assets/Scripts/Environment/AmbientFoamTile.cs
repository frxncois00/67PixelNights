using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Standard Tile data with a native, render-only animation.</summary>
public sealed class AmbientFoamTile : Tile
{
    public Sprite[] frames;
    public float framesPerSecond = 8;

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData data)
    {
        if (frames == null || frames.Length < 2) return false;
        data.animatedSprites = frames;
        data.animationSpeed = framesPerSecond;
        // Stable offsets avoid synchronized shoreline pulses without consuming gameplay RNG.
        data.animationStartTime = Mathf.Repeat(position.x * .173f + position.y * .317f, 1f)
            * frames.Length / Mathf.Max(1, framesPerSecond);
        return true;
    }
}
