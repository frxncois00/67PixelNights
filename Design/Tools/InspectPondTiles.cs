using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

return InspectPondTiles.Main();

public static class InspectPondTiles
{
    public static string Main()
    {
        var sb = new StringBuilder();
        var ground = GameObject.Find("Grid/HiddenPondClearing/Terrain/Ground")?.GetComponent<Tilemap>();
        var water = GameObject.Find("Grid/HiddenPondClearing/Water/Water")?.GetComponent<Tilemap>();
        var cliffs = GameObject.Find("Grid/HiddenPondClearing/Terrain/Cliffs (upper tier)")?.GetComponent<Tilemap>();
        var ramps = GameObject.Find("Grid/HiddenPondClearing/Terrain/Grass Ramps")?.GetComponent<Tilemap>();

        sb.AppendLine("=== WEST BANK & SLUICE SHAFT AREA (x=-34..-18, y=22..12) ===");
        sb.Append("     ");
        for (int x = -34; x <= -18; x++) sb.Append($"{Mathf.Abs(x)%10}");
        sb.AppendLine();
        for (int y = 22; y >= 12; y--)
        {
            sb.Append($"{y,3}: ");
            for (int x = -34; x <= -18; x++)
            {
                Vector3 wc = new Vector3(x + 0.5f, y + 0.5f, 0f);
                Vector3Int wCell = water.WorldToCell(wc);
                TileBase wt = water.GetTile(wCell);
                Tile.ColliderType wCol = wt != null ? water.GetColliderType(wCell) : Tile.ColliderType.None;
                TileBase ct = cliffs.GetTile(cliffs.WorldToCell(wc));
                TileBase rt = ramps.GetTile(ramps.WorldToCell(wc));
                TileBase gt = ground.GetTile(ground.WorldToCell(wc));

                char ch = '.';
                if (rt != null) ch = 'R';
                else if (ct != null) ch = 'C';
                else if (wt != null && wCol != Tile.ColliderType.None) ch = '~';
                else if (wt != null && wCol == Tile.ColliderType.None) ch = '=';
                else if (gt != null) ch = '#';
                sb.Append(ch);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
