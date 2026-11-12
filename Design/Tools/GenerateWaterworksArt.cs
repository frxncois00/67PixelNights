using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

return GenerateWaterworksArt.Main();

public static class GenerateWaterworksArt
{
    public static string Main()
    {
        string dir = "Assets/DrownedGrove/Art";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        GenerateCreatureSheets(dir);
        GeneratePropsSheet(dir);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "Generated and sliced all Sunken Waterworks creature and prop sprite sheets in Assets/DrownedGrove/Art.";
    }

    static void GenerateCreatureSheets(string dir)
    {
        // 1. Reed Stalker (128x32 -> 4 frames of 32x32)
        CreateCreatureSheet(dir + "/ReedStalker_Idle.png", 32, 32, 4, DrawReedStalkerFrame, false);
        CreateCreatureSheet(dir + "/ReedStalker_Walk.png", 32, 32, 4, DrawReedStalkerFrame, true);

        // 2. Sluice Crab (128x32 -> 4 frames of 32x32)
        CreateCreatureSheet(dir + "/SluiceCrab_Idle.png", 32, 32, 4, DrawSluiceCrabFrame, false);
        CreateCreatureSheet(dir + "/SluiceCrab_Walk.png", 32, 32, 4, DrawSluiceCrabFrame, true);

        // 3. Lantern Eel (128x32 -> 4 frames of 32x32)
        CreateCreatureSheet(dir + "/LanternEel_Idle.png", 32, 32, 4, DrawLanternEelFrame, false);
        CreateCreatureSheet(dir + "/LanternEel_Walk.png", 32, 32, 4, DrawLanternEelFrame, true);

        // 4. The Floodgate Keeper Boss (192x48 -> 4 frames of 48x48)
        CreateCreatureSheet(dir + "/FloodgateKeeper_Idle.png", 48, 48, 4, DrawFloodgateKeeperFrame, false);
        CreateCreatureSheet(dir + "/FloodgateKeeper_Walk.png", 48, 48, 4, DrawFloodgateKeeperFrame, true);
    }

    delegate void FrameDrawer(Texture2D tex, int ox, int oy, int frame, bool isWalk);

    static void CreateCreatureSheet(string path, int fw, int fh, int frames, FrameDrawer drawer, bool isWalk)
    {
        var tex = new Texture2D(fw * frames, fh, TextureFormat.RGBA32, false);
        var clear = new Color32[fw * frames * fh];
        tex.SetPixels32(clear);
        for (int f = 0; f < frames; f++)
            drawer(tex, f * fw, 0, f, isWalk);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        string baseName = Path.GetFileNameWithoutExtension(path);
        var rects = new List<SpriteRect>();
        for (int f = 0; f < frames; f++)
        {
            rects.Add(new SpriteRect
            {
                name = $"{baseName}_{f}",
                rect = new Rect(f * fw, 0, fw, fh),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0.18f),
                spriteID = GUID.Generate()
            });
        }
        ConfigureSpriteSheet(path, 32, rects);
    }

    static void DrawReedStalkerFrame(Texture2D t, int ox, int oy, int f, bool walk)
    {
        Color32 outline = new Color32(18, 34, 28, 255);
        Color32 darkScale = new Color32(36, 78, 58, 255);
        Color32 midScale = new Color32(58, 118, 82, 255);
        Color32 reedFrond = new Color32(96, 156, 78, 255);
        Color32 belly = new Color32(132, 168, 120, 255);
        Color32 eyeGlow = new Color32(210, 255, 95, 255);

        int bob = (f % 2 == 1) ? 1 : 0;
        int stride = walk ? ((f % 2 == 0) ? -2 : 2) : 0;

        // Shadow/water ring at base
        FillEllipse(t, ox + 16, oy + 5, 10, 3, new Color32(20, 55, 58, 130));

        // Crouched amphibious body
        FillEllipse(t, ox + 16, oy + 12 + bob, 9, 5, outline);
        FillEllipse(t, ox + 16, oy + 12 + bob, 8, 4, darkScale);
        FillEllipse(t, ox + 16, oy + 13 + bob, 6, 3, midScale);
        FillEllipse(t, ox + 16, oy + 11 + bob, 5, 2, belly);

        // Low-slung head & jaws
        FillEllipse(t, ox + 16, oy + 15 + bob, 6, 4, outline);
        FillEllipse(t, ox + 16, oy + 15 + bob, 5, 3, midScale);

        // Glowing chartreuse predator eyes
        SetPx(t, ox + 13, oy + 15 + bob, eyeGlow);
        SetPx(t, ox + 14, oy + 15 + bob, eyeGlow);
        SetPx(t, ox + 18, oy + 15 + bob, eyeGlow);
        SetPx(t, ox + 19, oy + 15 + bob, eyeGlow);

        // Reed-like dorsal crest fronds
        int sway = (f == 1) ? -1 : (f == 3 ? 1 : 0);
        for (int i = -2; i <= 2; i++)
        {
            int bx = ox + 16 + i * 3;
            DrawLine(t, bx, oy + 16 + bob, bx + sway + i, oy + 24 + bob, outline);
            DrawLine(t, bx, oy + 17 + bob, bx + sway + i, oy + 23 + bob, reedFrond);
        }

        // Webbed limbs
        FillRect(t, ox + 8, oy + 6 + Mathf.Max(0, stride), 3, 5, darkScale);
        FillRect(t, ox + 21, oy + 6 + Mathf.Max(0, -stride), 3, 5, darkScale);
        FillRect(t, ox + 11, oy + 5, 3, 4, midScale);
        FillRect(t, ox + 18, oy + 5, 3, 4, midScale);
    }

    static void DrawSluiceCrabFrame(Texture2D t, int ox, int oy, int f, bool walk)
    {
        Color32 outline = new Color32(22, 28, 34, 255);
        Color32 slate = new Color32(58, 78, 88, 255);
        Color32 copper = new Color32(158, 96, 56, 255);
        Color32 verdigris = new Color32(72, 168, 148, 255);
        Color32 highlight = new Color32(215, 152, 98, 255);
        Color32 eyeGlow = new Color32(95, 245, 255, 255);

        int bob = (f % 2 == 1) ? 1 : 0;
        int legShift = walk ? ((f % 2 == 0) ? 1 : -1) : 0;

        FillEllipse(t, ox + 16, oy + 5, 12, 3, new Color32(18, 42, 48, 140));

        // 6 jointed legs on sides
        for (int i = 0; i < 3; i++)
        {
            int ly = oy + 7 + i * 3 + ((i % 2 == 0) ? legShift : -legShift);
            DrawLine(t, ox + 8, oy + 11, ox + 3, ly, outline);
            DrawLine(t, ox + 8, oy + 12, ox + 4, ly + 1, slate);
            DrawLine(t, ox + 24, oy + 11, ox + 29, ly, outline);
            DrawLine(t, ox + 24, oy + 12, ox + 28, ly + 1, slate);
        }

        // Wide armored copper-and-slate carapace
        FillEllipse(t, ox + 16, oy + 14 + bob, 11, 7, outline);
        FillEllipse(t, ox + 16, oy + 14 + bob, 10, 6, slate);
        FillEllipse(t, ox + 16, oy + 15 + bob, 8, 5, copper);
        // Verdigris barnacle patches & rivets
        FillRect(t, ox + 11, oy + 16 + bob, 3, 2, verdigris);
        FillRect(t, ox + 19, oy + 15 + bob, 3, 2, verdigris);
        SetPx(t, ox + 14, oy + 18 + bob, highlight);
        SetPx(t, ox + 18, oy + 18 + bob, highlight);

        // Glowing cyan eyes under shell rim
        FillRect(t, ox + 12, oy + 11 + bob, 2, 2, eyeGlow);
        FillRect(t, ox + 18, oy + 11 + bob, 2, 2, eyeGlow);

        // Heavy frontal crusher claw (guards front)
        int clawY = oy + 8 + bob + (f == 2 ? 1 : 0);
        FillEllipse(t, ox + 11, clawY, 5, 4, outline);
        FillEllipse(t, ox + 11, clawY, 4, 3, copper);
        SetPx(t, ox + 10, clawY + 1, highlight);

        FillEllipse(t, ox + 21, clawY, 4, 3, outline);
        FillEllipse(t, ox + 21, clawY, 3, 2, verdigris);
    }

    static void DrawLanternEelFrame(Texture2D t, int ox, int oy, int f, bool walk)
    {
        Color32 outline = new Color32(14, 24, 36, 255);
        Color32 deepIndigo = new Color32(34, 58, 86, 255);
        Color32 tealFin = new Color32(56, 148, 158, 255);
        Color32 cyanStripe = new Color32(95, 235, 245, 255);
        Color32 lureGlow = new Color32(255, 235, 115, 255);
        Color32 ripple = new Color32(130, 225, 235, 190);

        int wave = (f == 0 || f == 2) ? 0 : (f == 1 ? 2 : -2);

        // Waterline ripple ring at base
        DrawEllipseOutline(t, ox + 16, oy + 6, 12, 4, ripple);
        DrawEllipseOutline(t, ox + 16, oy + 6, 8, 2, cyanStripe);

        // Sinuous S-curved eel body rising from the water
        for (int y = 6; y <= 21; y++)
        {
            float tNorm = (y - 6) / 15f;
            int cx = ox + 16 + Mathf.RoundToInt(Mathf.Sin(tNorm * Mathf.PI * 1.6f + f * 1.5f) * 3.5f);
            int rad = y < 18 ? 4 : 3;
            FillEllipse(t, cx, oy + y, rad + 1, 2, outline);
            FillEllipse(t, cx, oy + y, rad, 1, deepIndigo);
            SetPx(t, cx - 1, oy + y, cyanStripe);
            SetPx(t, cx + rad, oy + y, tealFin);
        }

        // Eel head & jaws at top
        int hx = ox + 16 + wave / 2;
        FillEllipse(t, hx, oy + 21, 5, 3, outline);
        FillEllipse(t, hx, oy + 21, 4, 2, deepIndigo);
        SetPx(t, hx - 2, oy + 21, lureGlow);
        SetPx(t, hx + 2, oy + 21, lureGlow);

        // Arching angler lantern stalk & glowing lure orb
        DrawLine(t, hx, oy + 23, hx - 4, oy + 28, tealFin);
        DrawLine(t, hx - 4, oy + 28, hx - 6, oy + 26, cyanStripe);
        FillEllipse(t, hx - 6, oy + 25, 3, 3, new Color32(120, 245, 255, 160));
        FillEllipse(t, hx - 6, oy + 25, 2, 2, lureGlow);
    }

    static void DrawFloodgateKeeperFrame(Texture2D t, int ox, int oy, int f, bool walk)
    {
        Color32 outline = new Color32(18, 22, 28, 255);
        Color32 ironDark = new Color32(48, 62, 74, 255);
        Color32 ironMid = new Color32(78, 98, 112, 255);
        Color32 bronze = new Color32(168, 114, 58, 255);
        Color32 bronzeBright = new Color32(224, 172, 92, 255);
        Color32 hydroCore = new Color32(85, 245, 255, 255);
        Color32 moss = new Color32(62, 128, 92, 255);

        int bob = (f % 2 == 1) ? 1 : 0;
        int step = walk ? ((f % 2 == 0) ? 2 : -2) : 0;

        // Boss base shadow
        FillEllipse(t, ox + 24, oy + 7, 15, 4, new Color32(15, 35, 42, 160));

        // Heavy armored greaves
        FillRect(t, ox + 15, oy + 7 + Mathf.Max(0, step), 6, 10, outline);
        FillRect(t, ox + 16, oy + 8 + Mathf.Max(0, step), 4, 8, ironDark);
        FillRect(t, ox + 27, oy + 7 + Mathf.Max(0, -step), 6, 10, outline);
        FillRect(t, ox + 28, oy + 8 + Mathf.Max(0, -step), 4, 8, ironDark);

        // Hulking floodgate torso & bronze pauldrons
        FillEllipse(t, ox + 24, oy + 23 + bob, 14, 11, outline);
        FillEllipse(t, ox + 24, oy + 23 + bob, 13, 10, ironDark);
        FillEllipse(t, ox + 24, oy + 24 + bob, 11, 8, ironMid);

        // Shoulder floodgate pauldrons
        FillEllipse(t, ox + 11, oy + 27 + bob, 6, 5, outline);
        FillEllipse(t, ox + 11, oy + 27 + bob, 5, 4, bronze);
        FillRect(t, ox + 9, oy + 28 + bob, 4, 2, moss);

        FillEllipse(t, ox + 37, oy + 27 + bob, 6, 5, outline);
        FillEllipse(t, ox + 37, oy + 27 + bob, 5, 4, bronze);
        FillRect(t, ox + 35, oy + 28 + bob, 4, 2, moss);

        // Glowing pressurized Hydro-Core weak point in chest
        FillEllipse(t, ox + 24, oy + 23 + bob, 5, 5, bronze);
        FillEllipse(t, ox + 24, oy + 23 + bob, 4, 4, hydroCore);
        SetPx(t, ox + 24, oy + 24 + bob, Color.white);

        // Diving-bell horned helm
        FillEllipse(t, ox + 24, oy + 35 + bob, 8, 6, outline);
        FillEllipse(t, ox + 24, oy + 35 + bob, 7, 5, bronze);
        FillRect(t, ox + 20, oy + 34 + bob, 9, 2, hydroCore); // Glowing visor slit
        SetPx(t, ox + 24, oy + 35 + bob, bronzeBright);

        // Heavy Sluice Paddle-Cleaver weapon in right hand
        int wx = ox + 39;
        int wy = oy + 14 + bob + (f == 2 ? 2 : 0);
        FillRect(t, wx - 1, wy, 3, 24, outline);
        FillRect(t, wx, wy + 1, 1, 22, bronzeBright);
        FillRect(t, wx - 4, wy + 14, 8, 10, outline);
        FillRect(t, wx - 3, wy + 15, 6, 8, ironMid);
        FillRect(t, wx - 2, wy + 17, 4, 4, hydroCore);
    }

    static void GeneratePropsSheet(string dir)
    {
        // Atlas 512x256 at 32 PPU
        int W = 512, H = 256;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var clear = new Color32[W * H];
        tex.SetPixels32(clear);

        Color32 outline = new Color32(22, 26, 30, 255);
        Color32 woodDark = new Color32(78, 52, 34, 255);
        Color32 woodMid = new Color32(116, 78, 48, 255);
        Color32 woodLight = new Color32(154, 108, 68, 255);
        Color32 rope = new Color32(188, 162, 108, 255);
        Color32 stoneDark = new Color32(58, 68, 74, 255);
        Color32 stoneMid = new Color32(92, 106, 114, 255);
        Color32 stoneLight = new Color32(132, 148, 154, 255);
        Color32 ironDark = new Color32(42, 52, 58, 255);
        Color32 ironLight = new Color32(98, 116, 126, 255);
        Color32 brass = new Color32(198, 152, 72, 255);
        Color32 moss = new Color32(64, 124, 78, 255);
        Color32 algae = new Color32(72, 158, 112, 210);
        Color32 waterDeep = new Color32(36, 98, 112, 235);
        Color32 waterFoam = new Color32(168, 238, 242, 235);
        Color32 glowCyan = new Color32(95, 245, 255, 255);
        Color32 glowAmber = new Color32(255, 205, 85, 255);

        var rects = new List<SpriteRect>();
        void AddSlice(string name, int x, int y, int w, int h, float py = 0.2f)
        {
            rects.Add(new SpriteRect
            {
                name = name,
                rect = new Rect(x, y, w, h),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, py),
                spriteID = GUID.Generate()
            });
        }

        // 1. MooringPost (0, 0, 16, 32)
        FillRect(tex, 4, 2, 8, 26, outline);
        FillRect(tex, 5, 3, 6, 24, woodMid);
        FillRect(tex, 5, 24, 6, 3, woodLight);
        FillRect(tex, 3, 14, 10, 5, rope);
        FillRect(tex, 5, 6, 6, 4, moss);
        AddSlice("MooringPost", 0, 0, 16, 32, 0.15f);

        // 2.DepthMarkerPost (16, 0, 16, 48)
        FillRect(tex, 20, 2, 8, 42, outline);
        FillRect(tex, 21, 3, 6, 40, stoneMid);
        FillRect(tex, 21, 3, 6, 14, new Color32(38, 56, 58, 255)); // Water stain
        FillRect(tex, 21, 16, 6, 2, glowCyan); // Flood level line
        for (int m = 10; m <= 36; m += 6)
            FillRect(tex, 22, m, 4, 1, brass);
        AddSlice("DepthMarkerPost", 16, 0, 16, 48, 0.12f);

        // 3. WaterLevelGaugeMarker (32, 0, 16, 32)
        FillRect(tex, 36, 2, 8, 28, outline);
        FillRect(tex, 37, 3, 6, 26, ironDark);
        FillRect(tex, 38, 6, 4, 20, glowCyan);
        FillRect(tex, 36, 16, 8, 3, brass);
        AddSlice("WaterLevelGaugeMarker", 32, 0, 16, 32, 0.15f);

        // 4. FishingRack (48, 0, 48, 40)
        DrawLine(tex, 52, 4, 60, 35, outline);
        DrawLine(tex, 53, 4, 61, 35, woodMid);
        DrawLine(tex, 92, 4, 84, 35, outline);
        DrawLine(tex, 91, 4, 83, 35, woodMid);
        FillRect(tex, 56, 30, 32, 4, outline);
        FillRect(tex, 57, 31, 30, 2, woodLight);
        // Hanging net & fish silhouettes
        for (int nx = 60; nx <= 84; nx += 4)
            DrawLine(tex, nx, 30, nx, 12, rope);
        for (int ny = 14; ny <= 28; ny += 4)
            DrawLine(tex, 60, ny, 84, ny, rope);
        FillEllipse(tex, 66, 22, 3, 6, new Color32(95, 148, 162, 255));
        FillEllipse(tex, 78, 20, 3, 6, new Color32(95, 148, 162, 255));
        AddSlice("FishingRack", 48, 0, 48, 40, 0.15f);

        // 5. BrokenBoatRamp (96, 0, 64, 48)
        for (int p = 0; p < 6; p++)
        {
            int py = 6 + p * 6;
            FillRect(tex, 100, py, 36, 5, outline);
            FillRect(tex, 101, py + 1, 34, 3, p % 2 == 0 ? woodMid : woodDark);
        }
        // Half-sunken broken skiff hull beside ramp
        FillEllipse(tex, 144, 22, 13, 16, outline);
        FillEllipse(tex, 144, 22, 11, 14, woodDark);
        FillEllipse(tex, 144, 22, 8, 11, waterDeep);
        FillRect(tex, 136, 22, 16, 3, woodLight); // Boat bench
        DrawEllipseOutline(tex, 144, 22, 14, 17, waterFoam);
        AddSlice("BrokenBoatRamp", 96, 0, 64, 48, 0.5f);

        // 6. SluiceShaftGrate (160, 0, 48, 48)
        FillRect(tex, 166, 4, 36, 38, new Color32(16, 24, 28, 220));
        // Iron bars
        for (int bx = 168; bx <= 198; bx += 6)
        {
            FillRect(tex, bx, 4, 3, 38, outline);
            FillRect(tex, bx + 1, 5, 1, 36, ironLight);
        }
        FillRect(tex, 166, 14, 36, 3, ironDark);
        FillRect(tex, 166, 28, 36, 3, ironDark);
        FillRect(tex, 166, 4, 36, 8, new Color32(42, 88, 76, 190)); // Damp moss/water stain at base
        AddSlice("SluiceShaftGrate", 160, 0, 48, 48, 0.15f);

        // 7. WalkwayStoneSlab (208, 0, 32, 32)
        FillRect(tex, 208, 0, 32, 32, outline);
        FillRect(tex, 209, 1, 30, 30, stoneDark);
        FillRect(tex, 211, 3, 12, 12, stoneMid);
        FillRect(tex, 225, 3, 12, 12, stoneMid);
        FillRect(tex, 211, 17, 12, 12, stoneMid);
        FillRect(tex, 225, 17, 12, 12, stoneMid);
        // Center iron grating strip
        for (int gx = 213; gx <= 235; gx += 4)
            FillRect(tex, gx, 13, 2, 6, ironLight);
        AddSlice("WalkwayStoneSlab", 208, 0, 32, 32, 0.5f);

        // 8. DrainageWheelPedestal (240, 0, 32, 32) & DrainageWheelRotor (272, 0, 32, 32)
        FillRect(tex, 248, 2, 16, 18, outline);
        FillRect(tex, 249, 3, 14, 16, ironDark);
        FillRect(tex, 251, 5, 10, 12, stoneMid);
        FillRect(tex, 246, 2, 20, 4, brass);
        AddSlice("DrainageWheelPedestal", 240, 0, 32, 32, 0.2f);

        DrawEllipseOutline(tex, 288, 16, 12, 12, outline);
        DrawEllipseOutline(tex, 288, 16, 11, 11, brass);
        DrawEllipseOutline(tex, 288, 16, 10, 10, outline);
        DrawLine(tex, 277, 16, 299, 16, brass);
        DrawLine(tex, 288, 5, 288, 27, brass);
        DrawLine(tex, 280, 8, 296, 24, brass);
        DrawLine(tex, 280, 24, 296, 8, brass);
        FillEllipse(tex, 288, 16, 4, 4, ironDark);
        FillEllipse(tex, 288, 16, 2, 2, glowCyan);
        AddSlice("DrainageWheelRotor", 272, 0, 32, 32, 0.5f);

        // 9. PumpHousing (304, 0, 64, 48)
        FillRect(tex, 308, 4, 56, 34, outline);
        FillRect(tex, 309, 5, 54, 32, stoneDark);
        FillRect(tex, 312, 8, 30, 26, stoneMid);
        // Brass boiler/pressure dome
        FillEllipse(tex, 327, 34, 12, 8, outline);
        FillEllipse(tex, 327, 34, 10, 6, brass);
        FillEllipse(tex, 327, 22, 5, 5, glowCyan); // Pressure gauge
        // Iron flywheel on right side of pump housing
        FillEllipse(tex, 350, 20, 11, 11, outline);
        DrawEllipseOutline(tex, 350, 20, 9, 9, ironLight);
        FillEllipse(tex, 350, 20, 3, 3, brass);
        AddSlice("PumpHousing", 304, 0, 64, 48, 0.2f);

        // 10. TimberFlumePipe (368, 0, 32, 24)
        FillRect(tex, 368, 4, 32, 16, outline);
        FillRect(tex, 368, 6, 32, 12, woodDark);
        FillRect(tex, 368, 8, 32, 8, waterDeep);
        FillRect(tex, 368, 10, 32, 3, waterFoam);
        FillRect(tex, 372, 4, 4, 16, ironLight);
        FillRect(tex, 392, 4, 4, 16, ironLight);
        AddSlice("TimberFlumePipe", 368, 0, 32, 24, 0.5f);

        // 11. WaterworksDiagramBoard (400, 0, 32, 32)
        FillRect(tex, 404, 2, 4, 14, woodDark);
        FillRect(tex, 424, 2, 4, 14, woodDark);
        FillRect(tex, 402, 12, 28, 18, outline);
        FillRect(tex, 403, 13, 26, 16, woodLight);
        FillRect(tex, 405, 15, 22, 12, new Color32(34, 68, 84, 255)); // Blueprint parchment
        DrawLine(tex, 407, 23, 425, 23, glowCyan);
        DrawLine(tex, 416, 23, 416, 17, glowAmber);
        FillEllipse(tex, 416, 17, 2, 2, glowAmber);
        AddSlice("WaterworksDiagramBoard", 400, 0, 32, 32, 0.18f);

        // 12. WalkableSteppingStone (432, 0, 32, 32)
        DrawEllipseOutline(tex, 448, 16, 14, 11, waterFoam);
        FillEllipse(tex, 448, 16, 12, 9, outline);
        FillEllipse(tex, 448, 16, 11, 8, stoneMid);
        FillEllipse(tex, 447, 17, 8, 5, stoneLight);
        FillEllipse(tex, 452, 13, 4, 2, moss);
        AddSlice("WalkableSteppingStone", 432, 0, 32, 32, 0.5f);

        // 13. AlgaePatch & WaterRipple (464, 0, 48, 32)
        FillEllipse(tex, 480, 16, 11, 7, algae);
        FillEllipse(tex, 486, 14, 7, 4, moss);
        AddSlice("AlgaePatch", 464, 0, 32, 32, 0.5f);

        // Row 2 (y = 64..192):
        // 14. BoundedSpillwayFlooded (0, 64, 64, 32)
        FillRect(tex, 0, 64, 64, 32, outline);
        FillRect(tex, 0, 65, 64, 4, stoneMid);
        FillRect(tex, 0, 91, 64, 4, stoneMid);
        FillRect(tex, 0, 69, 64, 22, waterDeep);
        for (int fx = 4; fx < 60; fx += 10)
            FillRect(tex, fx, 76 + (fx % 3) * 2, 7, 3, waterFoam);
        AddSlice("BoundedSpillwayFlooded", 0, 64, 64, 32, 0.5f);

        // 15. BoundedSpillwayDrained (64, 64, 64, 32)
        FillRect(tex, 64, 64, 64, 32, outline);
        FillRect(tex, 64, 65, 64, 4, stoneMid);
        FillRect(tex, 64, 91, 64, 4, stoneMid);
        FillRect(tex, 64, 69, 64, 22, stoneDark);
        for (int sx = 68; sx < 124; sx += 14)
        {
            FillRect(tex, sx, 72, 11, 16, stoneLight);
            FillRect(tex, sx + 1, 73, 9, 14, stoneMid);
        }
        AddSlice("BoundedSpillwayDrained", 64, 64, 64, 32, 0.5f);

        // 16. SpillwaySluiceGrate (128, 64, 32, 32)
        FillRect(tex, 130, 66, 28, 28, outline);
        FillRect(tex, 132, 68, 24, 4, brass);
        FillRect(tex, 132, 88, 24, 4, brass);
        for (int gx = 134; gx <= 154; gx += 5)
            FillRect(tex, gx, 68, 2, 24, ironLight);
        AddSlice("SpillwaySluiceGrate", 128, 64, 32, 32, 0.2f);

        // 17. BrokenWaterwheel (160, 64, 64, 64)
        DrawEllipseOutline(tex, 192, 96, 28, 28, outline);
        DrawEllipseOutline(tex, 192, 96, 27, 27, woodMid);
        DrawEllipseOutline(tex, 192, 96, 25, 25, woodDark);
        DrawEllipseOutline(tex, 192, 96, 18, 18, ironDark);
        for (int a = 0; a < 8; a++)
        {
            if (a == 2) continue; // Broken missing spoke!
            float rad = a * Mathf.PI * 0.25f;
            int ex = 192 + Mathf.RoundToInt(Mathf.Cos(rad) * 27f);
            int ey = 96 + Mathf.RoundToInt(Mathf.Sin(rad) * 27f);
            DrawLine(tex, 192, 96, ex, ey, woodLight);
            FillRect(tex, ex - 2, ey - 2, 5, 5, woodDark);
        }
        FillEllipse(tex, 192, 96, 6, 6, ironLight);
        FillRect(tex, 164, 66, 56, 12, waterDeep);
        FillRect(tex, 166, 76, 52, 3, waterFoam);
        AddSlice("BrokenWaterwheel", 160, 64, 64, 64, 0.25f);

        // 18. PumpStationSluiceGate (224, 64, 96, 48)
        FillRect(tex, 224, 64, 96, 44, outline);
        FillRect(tex, 226, 66, 92, 40, stoneDark);
        FillRect(tex, 228, 68, 20, 36, stoneMid);
        FillRect(tex, 296, 68, 20, 36, stoneMid);
        // Center iron sluice gate & dual hoist chains
        FillRect(tex, 250, 66, 44, 34, ironDark);
        for (int gx = 254; gx <= 290; gx += 6)
            FillRect(tex, gx, 68, 3, 30, ironLight);
        FillRect(tex, 248, 98, 48, 5, brass);
        FillRect(tex, 260, 70, 3, 36, brass);
        FillRect(tex, 282, 70, 3, 36, brass);
        FillRect(tex, 250, 66, 44, 6, waterFoam);
        AddSlice("PumpStationSluiceGate", 224, 64, 96, 48, 0.18f);

        // 19. GaugeShrineMonument (320, 64, 48, 64)
        FillRect(tex, 330, 66, 28, 10, outline);
        FillRect(tex, 332, 68, 24, 8, stoneMid);
        FillRect(tex, 336, 76, 16, 42, outline);
        FillRect(tex, 337, 77, 14, 40, stoneLight);
        // Brass water gauge scale on obelisk
        FillRect(tex, 342, 80, 4, 32, ironDark);
        for (int gy = 82; gy <= 108; gy += 4)
            FillRect(tex, 341, gy, 6, 1, brass);
        // Glowing shrine lantern at top
        FillEllipse(tex, 344, 118, 6, 6, brass);
        FillEllipse(tex, 344, 118, 4, 4, glowAmber);
        FillRect(tex, 333, 74, 8, 4, moss);
        AddSlice("GaugeShrineMonument", 320, 64, 48, 64, 0.15f);

        // 20. PumpPlatformSlab (368, 64, 32, 32)
        FillRect(tex, 368, 64, 32, 32, outline);
        FillRect(tex, 369, 65, 30, 30, stoneMid);
        FillRect(tex, 371, 67, 26, 26, stoneDark);
        for (int py = 69; py <= 89; py += 5)
            FillRect(tex, 372, py, 24, 3, woodMid);
        AddSlice("PumpPlatformSlab", 368, 64, 32, 32, 0.5f);

        // 21. WaterRippleRing (400, 64, 32, 32)
        DrawEllipseOutline(tex, 416, 80, 13, 8, waterFoam);
        DrawEllipseOutline(tex, 416, 80, 8, 5, glowCyan);
        AddSlice("WaterRippleRing", 400, 64, 32, 32, 0.5f);

        // 22. NightTellGlow (432, 64, 32, 32)
        FillEllipse(tex, 448, 80, 7, 4, new Color32(120, 255, 240, 140));
        FillEllipse(tex, 444, 80, 2, 2, glowAmber);
        FillEllipse(tex, 452, 80, 2, 2, glowAmber);
        AddSlice("NightTellGlow", 432, 64, 32, 32, 0.5f);

        string outPath = dir + "/WaterworksProps.png";
        tex.Apply();
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
        ConfigureSpriteSheet(outPath, 32, rects);
    }

    static void ConfigureSpriteSheet(string assetPath, int ppu, List<SpriteRect> rects)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(rects.ToArray());
        provider.Apply();
        importer.SaveAndReimport();
    }

    static void SetPx(Texture2D t, int x, int y, Color32 c)
    {
        if (x >= 0 && x < t.width && y >= 0 && y < t.height)
            t.SetPixel(x, y, c);
    }

    static void FillRect(Texture2D t, int x, int y, int w, int h, Color32 c)
    {
        for (int iy = y; iy < y + h; iy++)
            for (int ix = x; ix < x + w; ix++)
                SetPx(t, ix, iy, c);
    }

    static void FillEllipse(Texture2D t, int cx, int cy, int rx, int ry, Color32 c)
    {
        for (int dy = -ry; dy <= ry; dy++)
        {
            for (int dx = -rx; dx <= rx; dx++)
            {
                float nx = dx / (float)Mathf.Max(1, rx);
                float ny = dy / (float)Mathf.Max(1, ry);
                if (nx * nx + ny * ny <= 1f)
                    SetPx(t, cx + dx, cy + dy, c);
            }
        }
    }

    static void DrawEllipseOutline(Texture2D t, int cx, int cy, int rx, int ry, Color32 c)
    {
        for (int a = 0; a < 64; a++)
        {
            float rad = a * Mathf.PI * 2f / 64f;
            int x = cx + Mathf.RoundToInt(Mathf.Cos(rad) * rx);
            int y = cy + Mathf.RoundToInt(Mathf.Sin(rad) * ry);
            SetPx(t, x, y, c);
        }
    }

    static void DrawLine(Texture2D t, int x0, int y0, int x1, int y1, Color32 c)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
        for (int i = 0; i <= steps; i++)
        {
            int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, i / (float)steps));
            int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, i / (float)steps));
            SetPx(t, x, y, c);
        }
    }
}
