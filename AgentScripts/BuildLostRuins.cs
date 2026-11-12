using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;

// One-time editor authoring code, run through Unity's command pipeline outside Assets.
public static class LostRuinsBuilder
{
    const string Folder = "Assets/LostRuins";
    const string Grove = "Assets/DrownedGrove/Art/SunkenLakeRuins.png";
    const string Dry = "Assets/AshenFrontier/Art/DryVegetation.png";
    const string Rocks = "Assets/AshenFrontier/Art/Rocks.png";
    const string Dungeon = "Assets/GoblinOutpost/Art/DungeonProps.png";
    const string Fire = "Assets/PixelCrawler/Bonfire/Fire_01-Sheet.png";
    const string BushFolder = "Assets/Tiny Swords/Terrain/Decorations/Bushes/";
    static readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
    static readonly List<Rect> occupied = new List<Rect>();
    static readonly List<GameObject> displaced = new List<GameObject>();
    static readonly List<string> removalLog = new List<string>();
    static Material mat;
    static ForestNavigation nav;
    static AnimatorController fireController;
    static Transform root, fortress, village, temple, graveyard, shrine, crypt, wild, ways;
    static readonly Color ruin = new Color(.79f,.82f,.76f);
    static readonly Color lichen = new Color(.70f,.78f,.59f);
    static readonly Color scrub = new Color(.67f,.73f,.59f);

    static Transform Group(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }
    static void LoadSheet(string path)
    {
        foreach (var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            art[s.name] = s;
    }
    static void LoadArt()
    {
        foreach (var path in new[]{Grove,Dry,Rocks,Dungeon,Fire,
            BushFolder+"Bushe1.png",BushFolder+"Bushe2.png",BushFolder+"Bushe3.png"})
            LoadSheet(path);
        foreach (var name in new[]{"Gatehouse","Temple","House","Tower","Crypt","Crystal","WallH","WallV",
            "WardenStatue","BreachedTower","Forge","IvyArch","RubbleA","RubbleB","BlightedAsh","OldOliveAsh",
            "GateStair","ChippedColumn","CarvedPlinth"})
        {
            string path = Folder+"/Art/"+name+".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Missing landmark texture " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = name.EndsWith("Ash")?64:32;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(.5f,name=="WallV"?.32f:name=="WallH"?.16f:.06f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            LoadSheet(path);
            if (!art.ContainsKey(name)) throw new Exception("Sprite not imported: "+name);
        }
    }
    static SpriteRenderer Place(Transform parent, string name, string sprite, float x, float y,
        float width=0, float depth=0, float scale=1, Color? tint=null, bool flip=false)
    {
        if (!art.TryGetValue(sprite, out var source)) throw new Exception("Missing sprite: "+sprite);
        var t = Group(parent, name);
        t.position = new Vector3(Mathf.Round(x*32)/32,Mathf.Round(y*32)/32,0);
        t.localScale = new Vector3(scale,scale,1);
        var sr=t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite=source; sr.flipX=flip; sr.sharedMaterial=mat;
        sr.sortingLayerName="Objects"; sr.sortingOrder=0;
        sr.spriteSortPoint=SpriteSortPoint.Pivot; sr.color=tint??Color.white;
        if (width>0) Box(t,0,depth*.25f,width,depth);
        return sr;
    }
    static void Box(Transform t,float ox,float oy,float width,float depth)
    {
        var c=t.gameObject.AddComponent<BoxCollider2D>();
        c.offset=new Vector2(ox,oy); c.size=new Vector2(width,depth);
    }
    static void Reserve(float x,float y,float w,float h)
    {
        occupied.Add(new Rect(x-w/2,y-h*.18f,w,h));
    }
    static void Way(string name,float x,float y) { Group(ways,name).position=new Vector3(x,y,0); }
    static void Wall(Transform g,float x,float y,bool vertical=false,bool fragment=false)
    {
        string sprite = fragment?"CollapsedPillarDrum":vertical?"SubmergedStoneWallV":"SubmergedStoneWallH";
        float w = fragment?.78f:vertical?.50f:1.47f;
        float d = vertical&&!fragment?1.27f:.34f;
        Place(g,fragment?"Fractured wall footing":"Moss-capped curtain wall",sprite,x,y,w,d,1,ruin);
        Reserve(x,y,w+.15f,vertical?1.8f:.8f);
    }
    static void Pillar(Transform g,float x,float y,bool full=false)
    {
        Place(g,full?"Carved standing column":"Shattered column base",full?"TallCarvedPillar":"BrokenPillarStump",
            x,y,.44f,.34f,1,ruin);
        Reserve(x,y,.72f,full?1.8f:.85f);
    }
    static void Arch(Transform g,string name,float x,float y,bool vines=true)
    {
        var r=Place(g,name,vines?"VineRuinedArch":"TempleArchway",x,y,0,0,.86f,ruin);
        float pierOffset=vines?.96f:1.34f;
        Box(r.transform,-pierOffset,0,.43f,.43f);
        Box(r.transform,pierOffset,0,.43f,.43f);
        Reserve(x,y,2*pierOffset+.45f,1.9f);
    }
    static void Building(Transform g,string label,string sprite,float x,float y,float w,float d,float scale=1,bool flip=false)
    {
        var r=Place(g,label,sprite,x,y,w,d,scale,null,flip);
        Reserve(x,y,r.bounds.size.x+.30f,r.bounds.size.y*.78f);
    }
    static void Glow(Transform owner, float y, float radius, float intensity)
    {
        var t=Group(owner,"Crimson light");
        t.localPosition=new Vector3(0,y,0);
        var lamp=t.gameObject.AddComponent<Light2D>();
        lamp.lightType=Light2D.LightType.Point;
        lamp.color=new Color(1,.105f,.045f);
        lamp.intensity=intensity; lamp.pointLightOuterRadius=radius;
        lamp.pointLightInnerRadius=.10f; lamp.falloffIntensity=.62f;
        var so=new SerializedObject(lamp);
        var layers=so.FindProperty("m_ApplyToSortingLayers");
        if(layers!=null)
        {
            var ids=SortingLayer.layers.Where(l=>l.name=="Ground"||l.name=="Objects").Select(l=>l.id).ToArray();
            layers.arraySize=ids.Length;
            for(int i=0;i<ids.Length;i++) layers.GetArrayElementAtIndex(i).intValue=ids[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    static void Flames()
    {
        const string clipPath=Folder+"/Watchfire.anim";
        const string controllerPath=Folder+"/Watchfire.controller";
        fireController=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(fireController) return;
        var clip = new AnimationClip {name="Ember flicker",frameRate=6,wrapMode=WrapMode.Loop};
        var frames=new ObjectReferenceKeyframe[5];
        for(int i=0;i<frames.Length;i++) frames[i]=new ObjectReferenceKeyframe{time=i/6f,value=art["Fire_01-Sheet_"+i%4]};
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),frames);
        var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true;
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        AssetDatabase.CreateAsset(clip,clipPath);
        fireController=AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath,clip);
    }
    static void Brazier(Transform group,float x,float y,bool light=true)
    {
        var stand=Place(group,"Red watchfire plinth","BrokenPillarStump",x,y,.38f,.32f,.78f,ruin);
        var flame=Place(stand.transform,"Ember flame","Fire_01-Sheet_0",x,y+.55f,0,0,.70f,new Color(1,.26f,.21f));
        flame.sortingOrder=1;
        flame.gameObject.AddComponent<Animator>().runtimeAnimatorController=fireController;
        if(light) Glow(stand.transform,.65f,2.25f,.63f);
        Reserve(x,y,.60f,1.10f);
    }
    static void BuildFortress()
    {
        fortress=Group(root,"The Vermilion Keep");
        var gate=Place(fortress,"The twin-tower gatehouse","Gatehouse",24,-10);
        Box(gate.transform,-2.28f,.52f,2.35f,1.65f);
        Box(gate.transform,2.28f,.52f,2.35f,1.65f);
        Reserve(24,-10,7.35f,6.0f);
        var curtain=Group(fortress,"Broken outer ward");
        foreach(float x in new[]{20.25f,22f,23.75f,25.5f,27.25f,29f}) Wall(curtain,x,-4.5f);
        foreach(float y in new[]{-5.45f,-7.0f,-8.55f,-10.1f,-12.85f}) Wall(curtain,19.45f,y,true);
        foreach(float y in new[]{-5.45f,-7.0f,-8.55f,-10.1f,-12.7f}) Wall(curtain,29.75f,y,true);
        foreach(float x in new[]{20.4f,22.15f,25.95f,27.72f,29.4f}) Wall(curtain,x,-14);
        Wall(curtain,19.45f,-11.65f,true,true);
        Wall(curtain,29.75f,-11.5f,true,true);
        Building(fortress,"Western sentinel tower","Tower",19.65f,-8.0f,1.46f,.9f,1);
        Building(fortress,"Eastern signal tower","Tower",29.15f,-7.2f,1.46f,.9f,1.15f,true);
        Pillar(curtain,19.65f,-14.10f); Pillar(curtain,29.5f,-14.0f);
        var stairs=Place(fortress,"Gatehouse approach steps","TempleStoneStairs",24,-10.66f,0,0,.81f,ruin);
        stairs.sortingLayerName="Ground"; stairs.sortingOrder=12;
        Brazier(fortress,21.5f,-11.35f); Brazier(fortress,26.48f,-11.35f);
        Place(fortress,"Eastward hanging standard","RedBanner",25.75f,-14.03f,0,0,.82f);
        Place(fortress,"Western hanging standard","RedBanner",22.09f,-14.03f,0,0,.82f);
        Place(fortress,"The last castellan","MossyRuinStatue",28.15f,-12.1f,.70f,.48f,1,ruin);
        Place(fortress,"Weathered king in the rear court","UnderwaterStatue",25.9f,-5.6f,.55f,.4f,.87f,ruin);
        Way("Keep forecourt",24,-12.25f); Way("Gate passage",24,-9.5f);
        Way("Hidden inner ward",24,-6f);
    }
    static void BuildDistricts()
    {
        temple=Group(root,"Cloister of the Hollow Bell");
        Building(temple,"The roofless basilica","Temple",15.5f,-18,2.7f,1.05f);
        Arch(temple,"Ivy gate of the processional way",18.45f,-15.55f);
        Pillar(temple,13.4f,-18.5f,true); Pillar(temple,17.65f,-18.65f);
        Pillar(temple,13.45f,-16.55f); Pillar(temple,17.68f,-16.85f,true);
        Wall(temple,14.75f,-14.3f); Wall(temple,16.95f,-14.3f,false,true);
        Place(temple,"Fallen saint","MossyRuinStatue",13.45f,-20.7f,.48f,.34f,.78f,ruin);
        Way("Temple stair",15.5f,-19.25f);

        village=Group(root,"Hearthfall – abandoned stone quarter");
        Building(village,"Burned bellmaker's house","House",21,-18.25f,1.89f,.80f);
        Building(village,"Eastern weaver's house","House",27.05f,-18.45f,1.83f,.76f,.91f,true);
        Building(village,"Roofless potter's dwelling","SubmergedHouseRuins",25.5f,-21.15f,1.56f,.73f,.82f);
        Building(village,"The last inn","House",20.1f,-23.05f,1.82f,.76f,.94f,true);
        Building(village,"Coastward foundations","SubmergedHouseRuins",23.1f,-23.40f,1.50f,.62f,.75f);
        Wall(village,28.35f,-19.35f,true); Wall(village,25.35f,-17.0f,false,true);
        Wall(village,19.25f,-21.25f,true); Wall(village,21.05f,-24.15f,false,true);
        Place(village,"Dry market fountain","MysteriousRuneSlab",24.05f,-19.7f,0,0,.92f,ruin);
        Pillar(village,25.12f,-19.8f); Pillar(village,18.8f,-22.8f);
        Brazier(village,21.65f,-20.5f,false);
        Way("Hearthfall market lane",23,-21.35f);

        graveyard=Group(root,"The nameless ossuary");
        Arch(graveyard,"Mourning arch",10.65f,-20.0f);
        Building(graveyard,"Broken mortuary chapel","SubmergedHouseRuins",8.4f,-16.45f,1.6f,.72f,.78f);
        var rows=new[]{new Vector2(6.9f,-18),new Vector2(8.0f,-17.85f),new Vector2(9.05f,-18.05f),
            new Vector2(10.08f,-17.8f),new Vector2(7.4f,-19.15f),new Vector2(8.58f,-19.12f),
            new Vector2(10.1f,-19.0f),new Vector2(11.0f,-17.75f)};
        for(int i=0;i<rows.Length;i++){
            var p=rows[i];
            Place(graveyard,i==2?"Broken grave marker":"Inscribed headstone",i==2?"BrokenPillarStump":"RuneStone",p.x,p.y,
                .32f,.22f,i%3==1?.83f:1,ruin,i%2==0);
            Reserve(p.x,p.y,.67f,.68f);
        }
        Wall(graveyard,6.3f,-18.45f,true); Wall(graveyard,8.3f,-16.2f);
        Place(graveyard,"The veiled mourner","UnderwaterStatue",11.45f,-16.85f,.55f,.35f,.82f,ruin);
        Way("Ossuary lantern walk",9,-20.4f);

        shrine=Group(root,"Redwake Sanctuary");
        Building(shrine,"The heartstone and its ritual dais","Crystal",11,-25,1.88f,.86f);
        foreach(var p in new[]{new Vector2(8.95f,-24),new Vector2(13.1f,-24.75f),new Vector2(9.1f,-26.65f),new Vector2(13,-26.5f)})
            Pillar(shrine,p.x,p.y,p.x>12 && p.y>-26);
        Wall(shrine,10.1f,-22.65f); Wall(shrine,12,-22.6f,false,true);
        Wall(shrine,8.22f,-25.85f,true); Wall(shrine,13.93f,-26.4f,true,true);
        Glow(shrine.Find("The heartstone and its ritual dais"),.72f,3.15f,.83f);
        Brazier(shrine,9.48f,-25.64f,false); Brazier(shrine,12.6f,-25.6f,false);
        Way("Heartstone dais",11,-26.5f);
        var tail=Group(shrine,"Southern pilgrims' landing");
        Place(tail,"Collapsed seaward lintel","CollapsedPillarDrum",7.0f,-28.3f,.65f,.28f,1,ruin);
        Pillar(tail,5.8f,-28.3f,true); Pillar(tail,8.3f,-28.4f);
        Way("Southern pilgrims' landing",7.5f,-27.6f);

        crypt=Group(root,"The far-eastern barrow");
        Building(crypt,"Rune-sealed descent","Crypt",35,-8,1.98f,.88f);
        Arch(crypt,"Crumbling outer portal",33.6f,-10.38f,false);
        Brazier(crypt,33.3f,-8.15f); Pillar(crypt,36.45f,-8.1f);
        Place(crypt,"Guardian of the drowned kings","MossyRuinStatue",32.3f,-8.8f,.62f,.35f,.88f,ruin);
        Wall(crypt,36.15f,-6.85f,true,true);
        Way("Eastern crypt steps",35,-9.0f);
        Way("Eastern wall breach",29.7f,-14.97f);
    }
    static bool Land(Vector2 p) { return nav.Terrain(p); }
    static bool InPool(Vector2 p)
    {
        Vector2[] centers={new Vector2(17.6f,-10),new Vector2(31.25f,-6.8f),new Vector2(7,-24.5f)};
        Vector2[] sizes={new Vector2(2.4f,1.18f),new Vector2(1.36f,.96f),new Vector2(1.61f,1.03f)};
        for(int i=0;i<centers.Length;i++)
        {
            var q=p-centers[i]; if(q.x*q.x/(sizes[i].x*sizes[i].x)+q.y*q.y/(sizes[i].y*sizes[i].y)<1) return true;
        }
        return false;
    }
    static bool NearArt(Vector2 p,float radius)
    {
        var bound=new Rect(p.x-radius,p.y-radius,radius*2,radius*2);
        return occupied.Any(r=>r.Overlaps(bound));
    }
    static void ThinExistingNature()
    {
        var candidates=UnityEngine.Object.FindObjectsByType<SpriteRenderer>()
            .Where(r=>!r.transform.IsChildOf(root) && r.transform.childCount==0 ||
                   !r.transform.IsChildOf(root) && r.transform.GetComponentsInChildren<SpriteRenderer>().Length==1)
            .Where(r=>r.transform.parent && (r.name.StartsWith("Expansion Tree ")||r.name.StartsWith("Expansion Rock ")||
                     r.name.StartsWith("Rocky Region_")||r.name.StartsWith("Abandoned Cabin_")))
            .OrderBy(r=>r.name).ToArray();
        foreach(var sr in candidates)
        {
            Vector2 p=sr.transform.position;
            bool construction=RuinsTerrain.Inside(p) && Land(p);
            bool approach=p.x>2.1f && p.x<11 && p.y< -16 && p.y> -21 && RuinsTerrain.RoadDistance(p)<.95f;
            if(!construction&&!approach) continue;
            bool insideWard = p.x>18.55f && p.x<30.6f && p.y> -15f && p.y< -4f;
            bool conflict=RuinsTerrain.RoadDistance(p)<1.34f || NearArt(p,.72f) || InPool(p) || insideWard || sr.name=="Expansion Tree 312";
            if(!conflict) { Reserve(p.x,p.y,.88f,.80f); continue; }
            displaced.Add(sr.gameObject);
            removalLog.Add(sr.transform.parent.name+"/"+sr.name+"\t"+p.ToString("F3"));
            sr.gameObject.SetActive(false);
        }
    }
    static void DetailLandscape()
    {
        wild=Group(root,"Woodland, fallen stone and undergrowth");
        var trees=Group(wild,"The deadwood belt");
        var growth=Group(wild,"Moss, thorn and fern");
        var debris=Group(wild,"Stones and scattered masonry");
        var rng=new System.Random(7027);
        float Rand(float lo,float hi)=>lo+(hi-lo)*(float)rng.NextDouble();
        var zoneTrees=new List<Vector2>();
        var used=new List<Vector2>();
        var pockets=new[]{new Vector2(12.2f,-11.4f),new Vector2(7.6f,-23.4f),new Vector2(15.6f,-25.1f),
            new Vector2(31.9f,-5),new Vector2(33.1f,-13.6f),new Vector2(28.9f,-16.1f),
            new Vector2(10.8f,-14.8f),new Vector2(16.9f,-4.6f),new Vector2(5.6f,-26.8f)};
        foreach(var c in pockets) for(int k=0;k<26;k++)
        {
            var p=c+new Vector2(Rand(-2.8f,2.8f),Rand(-2.0f,2.0f));
            if(!RuinsTerrain.Inside(p)||!Land(p)||InPool(p)||NearArt(p,.45f)||RuinsTerrain.RoadDistance(p)<1.08f) continue;
            if(zoneTrees.Any(q=>Vector2.Distance(p,q)<1.02f)) continue;
            float noise=Rand(0,1);
            Place(trees,noise>.3f?"Twisted ash":"Withered sapling",noise>.3f?"DeadTree_Large":"DeadTree_Small",
                  p.x,p.y,noise>.35f?.30f:0,.28f,noise>.78f?1.36f:1,noise>.45f?ruin:scrub,noise>.55f);
            zoneTrees.Add(p); Reserve(p.x,p.y,.48f,.75f);
        }
        string[] greens={"Bushe1_000","Bushe2_000","DryFern_Wide","DryScrub_Tuft","DryBroadleaf_Small","Bush_Ochre_Small"};
        string[] chips={"Rock_Slate_Med1","Rock_Slate_Small1","Rock_Charred_Cluster","DryBranch_1","DryTwig_1","CollapsedPillarDrum"};
        for(int i=0;i<1250 && used.Count<245;i++)
        {
            var p=new Vector2(Rand(3.7f,38.2f),Rand(-29.6f,-2.5f));
            if(!RuinsTerrain.Inside(p)||!Land(p)||InPool(p)||NearArt(p,.22f)||RuinsTerrain.RoadDistance(p)<.78f) continue;
            if(used.Any(q=>(p-q).sqrMagnitude<.37f)) continue;
            if(p.x>19 && p.x<30 && p.y> -13.8f && p.y< -4.6f && i%7!=0) continue;
            float pocket=Mathf.PerlinNoise(p.x*.39f+.7f,p.y*.39f+12.5f);
            if(pocket<.43f&&i%4!=0) continue;
            bool green=rng.NextDouble()<.61f;
            string s=green?greens[rng.Next(greens.Length)]:chips[rng.Next(chips.Length)];
            var r=Place(green?growth:debris,green?"Growth among the stones":"Weathered debris",s,p.x,p.y,
                         0,0,s=="CollapsedPillarDrum"?.65f:1,green?scrub:ruin,rng.NextDouble()>.5);
            if(s=="DryTwig_1"||s=="DryBranch_1"||s=="DryScrub_Tuft"){r.sortingLayerName="Ground";r.sortingOrder=12;}
            used.Add(p);
        }
    }
    static void FinishArt(Transform biome)
    {
        var existing=biome.GetComponentsInChildren<SpriteRenderer>();
        int chips=0,trees=0;
        foreach(var r in existing)
        {
            string id=r.sprite.name, into=null;
            if(id=="SubmergedStoneWallH") into="WallH";
            if(id=="SubmergedStoneWallV") into="WallV";
            if(id=="MossyRuinStatue" || id=="UnderwaterStatue") into="WardenStatue";
            if(id=="VineRuinedArch" || id=="TempleArchway") into="IvyArch";
            if(id=="CollapsedPillarDrum") into=(chips++)%2==0?"RubbleA":"RubbleB";
            if(r.name=="Burned bellmaker's house" || r.name=="Roofless potter's dwelling") into="Forge";
            if(r.name=="Eastern signal tower") into="BreachedTower";
            if(r.name=="Broken mortuary chapel") into="Crypt";
            if(id=="DeadTree_Large" || id=="DeadTree_Small")
            {
                trees++;
                if(trees%3!=0) into=trees%5==0?"OldOliveAsh":"BlightedAsh";
                if(into!=null) r.transform.localScale=new Vector3(trees%2==0?.87f:1,trees%2==0?.87f:1,1);
            }
            if(into==null) continue;
            r.sprite=art[into];
            r.color=into.EndsWith("Ash")?new Color(.80f,.83f,.77f):Color.white;
            if(into=="IvyArch")
            {
                foreach(var col in r.GetComponents<BoxCollider2D>())
                    col.offset=new Vector2(Mathf.Sign(col.offset.x)*1.13f,.1f);
            }
        }
        var villageRoot=biome.Find("Hearthfall – abandoned stone quarter");
        Building(villageRoot,"South-coast fallen belfry","BreachedTower",17.7f,-24.65f,1.45f,.65f,.78f);
        Place(villageRoot,"Blacksmith's broken bench","CollapsedPillarDrum",20.05f,-19.3f,0,0,.68f,ruin);
        var cloister=biome.Find("Cloister of the Hollow Bell");
        var chapel=Group(cloister,"Wayside chapel of the Ash Warden");
        var arch=Place(chapel,"Open chancel arch","IvyArch",12.4f,-13.1f,0,0,.78f);
        Box(arch.transform,-1.12f,.1f,.35f,.33f); Box(arch.transform,1.12f,.1f,.35f,.33f);
        Place(chapel,"Warden beneath the broken vault","WardenStatue",12.1f,-12.60f,.4f,.30f,.81f);
        Place(chapel,"Chancel wall stump","WallV",10.9f,-13.85f,.30f,.75f,.63f);
        Place(chapel,"Shattered eastern arcade","RubbleB",13.0f,-14.05f);
        var way=Group(biome.Find("Exploration Waypoints"),"Woodland wayside chapel");
        way.position=new Vector3(12.5f,-14.25f,0);

        // Leave the steps and dark mouth of the barrow accessible up to the back wall.
        var entrance=biome.Find("The far-eastern barrow/Rune-sealed descent");
        var collider=entrance.GetComponent<BoxCollider2D>();
        collider.offset=new Vector2(-.98f,.2f); collider.size=new Vector2(.68f,.77f);
        Box(entrance,.98f,.2f,.68f,.77f); Box(entrance,0,.80f,1.4f,.20f);

        var rubble=Group(biome,"Fall of the outer walls");
        var broken=new[]{
            new Vector2(19.12f,-11.62f),new Vector2(29.92f,-11.4f),new Vector2(20.2f,-13.64f),
            new Vector2(27.8f,-13.45f),new Vector2(28.9f,-7.48f),new Vector2(11.70f,-14.45f),
            new Vector2(18.08f,-24.98f),new Vector2(25.86f,-21.6f),new Vector2(21.0f,-18.75f),
            new Vector2(16.26f,-18.02f),new Vector2(32.8f,-9.72f),new Vector2(8.95f,-16.35f),
            new Vector2(10.10f,-22.62f),new Vector2(13.91f,-26.25f),new Vector2(28.5f,-18.8f)};
        var rng=new System.Random(472);
        foreach(var center in broken)
            for(int i=0;i<3;i++)
            {
                float x=center.x+(float)rng.NextDouble()*.98f-.49f;
                float y=center.y+(float)rng.NextDouble()*.58f-.29f;
                var sr=Place(rubble,"Loose masonry",i%2==0?"RubbleA":"RubbleB",x,y,0,0,i==0?1:.67f,Color.white,i%2==1);
                sr.sortingLayerName="Ground"; sr.sortingOrder=12;
            }
    }
    static void FinishCrossings(Transform biome)
    {
        var oldTrees=biome.Find("Woodland, fallen stone and undergrowth/The deadwood belt");
        var treesToMove=oldTrees.Cast<Transform>().Where(t=>Vector2.Distance(t.position,new Vector2(12.5f,-13.7f))<1.55f).OrderBy(t=>t.position.y).ToArray();
        var clearings=new[]{new Vector2(31,-8.8f),new Vector2(35.6f,-4.7f),new Vector2(14.2f,-26.9f),new Vector2(30.7f,-9.65f)};
        for(int i=0;i<Math.Min(treesToMove.Length,clearings.Length);i++) treesToMove[i].position=clearings[i];
        foreach(var r in biome.GetComponentsInChildren<SpriteRenderer>())
        {
            if(r.sprite.name=="TallCarvedPillar") {r.sprite=art["ChippedColumn"];r.color=Color.white;}
            if(r.sprite.name=="BrokenPillarStump") {r.sprite=art["CarvedPlinth"];r.color=Color.white;}
            if(r.name=="Gatehouse approach steps") {
                r.sprite=art["GateStair"];r.color=Color.white;
                r.transform.localScale=Vector3.one; r.transform.position=new Vector3(24,-10.71f,0);
            }
            if(r.name=="Coastward foundations") {
                r.sprite=art["WallH"];r.color=Color.white;
                Place(r.transform.parent,"Exposed inn foundation","WallV",22.52f,-22.78f,.31f,.70f,.58f);
                Place(r.transform.parent,"Fallen seaward wall","RubbleA",23.81f,-23.2f);
            }
        }
        var g=Group(biome,"Ruined bridges");
        foreach(var p in new[]{new Vector2(16.50f,-9.40f),new Vector2(16.55f,-10.50f),new Vector2(18.55f,-9.45f)})
            Place(g,"Broken causeway parapet","WallV",p.x,p.y,0,0,.55f);
        Place(g,"Slipped parapet stones","RubbleA",18.59f,-10.44f,0,0,.83f);
        Place(g,"Stones in the west pool","RubbleB",16.13f,-10.03f,0,0,.83f,new Color(.76f,.86f,.82f));
        // The causeway survives. The neighboring cistern retains only its two abutments.
        Place(g,"Eastern cistern bridge abutment","WallH",31.92f,-6.62f,0,0,.50f);
        Place(g,"Sunken end of the east bridge","RubbleB",31.02f,-6.69f,0,0,.69f,new Color(.63f,.76f,.72f));
    }
    public static string ApplyCrossings()
    {
        var instance=GameObject.Find("LostRuinsBiome");
        if(!instance||Application.isPlaying) throw new Exception("Expected newly authored biome in edit mode.");
        nav=UnityEngine.Object.FindAnyObjectByType<ForestNavigation>();
        mat=nav.ground.GetComponent<TilemapRenderer>().sharedMaterial;
        LoadArt();
        const string path=Folder+"/LostRuinsBiome.prefab";
        var contents=PrefabUtility.LoadPrefabContents(path);
        try {
            if(contents.transform.Find("Ruined bridges")) throw new Exception("Crossings already finished.");
            FinishCrossings(contents.transform);
            PrefabUtility.SaveAsPrefabAsset(contents,path);
        } finally {PrefabUtility.UnloadPrefabContents(contents);}
        var tree=GameObject.Find("Expansion Tree 312");
        if(tree && Vector2.Distance(tree.transform.position,new Vector2(11.69f,-13.69f))<.1f){
            File.AppendAllLines("Design/LostRuins/DisplacedNature.tsv",new[]{tree.transform.parent.name+"/"+tree.name+"\t"+((Vector2)tree.transform.position).ToString("F3")});
            tree.SetActive(false);
        }
        Physics2D.SyncTransforms();nav.Invalidate();
        AssetDatabase.SaveAssets();
        var scene=EditorSceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Saved both crossings and clarified the woodland chapel entrance.";
    }
    public static string ApplyArt()
    {
        var instance=GameObject.Find("LostRuinsBiome");
        if(!instance||Application.isPlaying) throw new Exception("Expected newly authored biome in edit mode.");
        nav=UnityEngine.Object.FindAnyObjectByType<ForestNavigation>();
        mat=nav.ground.GetComponent<TilemapRenderer>().sharedMaterial;
        LoadArt();
        const string path=Folder+"/LostRuinsBiome.prefab";
        var contents=PrefabUtility.LoadPrefabContents(path);
        try
        {
            if(contents.transform.Find("Fall of the outer walls")) throw new Exception("Finishing artwork already applied.");
            FinishArt(contents.transform);
            PrefabUtility.SaveAsPrefabAsset(contents,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        AssetDatabase.SaveAssets();
        Physics2D.SyncTransforms();nav.Invalidate();
        return "Updated architecture, trees, ruined chapel, fallen belfry and scattered masonry.";
    }
    public static string Main()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/SampleScene.unity" || Application.isPlaying || GameObject.Find("LostRuinsBiome"))
            throw new Exception("Expected SampleScene in edit mode with no LostRuinsBiome.");
        nav=UnityEngine.Object.FindAnyObjectByType<ForestNavigation>();
        if(!nav||!nav.ground) throw new Exception("Missing original navigation ground.");
        mat=nav.ground.GetComponent<TilemapRenderer>().sharedMaterial;
        LoadArt(); Flames();
        try {
            root=Group(null,"LostRuinsBiome");
            var ground=Group(root,"Terrain and roads");
            RuinsTerrain.Build(ground,nav.ground,mat);
            ways=Group(root,"Exploration Waypoints");
            BuildFortress(); BuildDistricts();
            ThinExistingNature(); DetailLandscape(); FinishArt(root); FinishCrossings(root);
            Way("East road turning",20,-2.5f);
            Way("Broken causeway",17.5f,-10);
            Way("Outer ward entrance",24,-14.75f);
            Way("South road turning",2,-18);
            Physics2D.SyncTransforms(); nav.Invalidate();
            File.WriteAllLines("Design/LostRuins/DisplacedNature.tsv",removalLog);
            var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject,Folder+"/LostRuinsBiome.prefab",InteractionMode.AutomatedAction);
            if(!prefab) throw new Exception("Could not save LostRuinsBiome prefab");
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save SampleScene");
            return "Saved biome with "+root.GetComponentsInChildren<Transform>().Length+" objects. Reversibly displaced "+displaced.Count+" nature objects.";
        } catch {
            foreach(var go in displaced) if(go) go.SetActive(true);
            if(root) UnityEngine.Object.DestroyImmediate(root.gameObject);
            throw;
        }
    }
}

// Editor authoring only. The saved scene uses normal Tilemaps and static 2D physics.
public static class RuinsTerrain
{
    const string Folder = "Assets/LostRuins/Tiles/";
    const int Ppu = 32, Stamp = 4, Width = 1280, Height = 1024;
    static Vector2 P(float x,float y) => new Vector2(x,y);
    static readonly Vector2[][] Routes = {
        new[] { P(20,-.62f),P(19.75f,-2.8f),P(18.4f,-4.6f),P(17,-5.6f),P(16.55f,-7.1f),P(16.8f,-8.1f),P(17.58f,-10),P(18.1f,-11.4f),P(18.45f,-13.35f),P(20,-14.8f),P(22,-15.4f),P(24,-14.8f),P(24,-11) },
        new[] { P(24,-14.8f),P(26.4f,-15.15f),P(29,-14.8f),P(30.8f,-13.1f),P(32,-12),P(33.8f,-11),P(35,-9),P(35,-8.25f) },
        new[] { P(24,-14.8f),P(24,-16.15f),P(23.3f,-17.5f),P(22,-19),P(22.25f,-20.3f),P(23.3f,-21.4f),P(25.6f,-20.9f),P(27.2f,-19.8f) },
        new[] { P(20,-14.8f),P(18.3f,-15.4f),P(17.35f,-17.3f),P(16,-19.25f),P(15.5f,-18.25f) },
        new[] { P(16,-19.25f),P(15,-21),P(13.6f,-21.65f),P(12,-23.25f),P(11.1f,-24),P(10,-24.8f),P(10,-25.8f),P(11,-26.55f) },
        new[] { P(.62f,-18),P(3.4f,-18.25f),P(5.9f,-20),P(7.4f,-20.4f),P(9,-20),P(11.75f,-20.3f),P(13.7f,-19.65f),P(16,-19.25f) },
        new[] { P(13.6f,-21.65f),P(15.2f,-22.65f),P(17,-23.6f),P(18.9f,-23.65f),P(20.6f,-22.75f),P(22.25f,-20.3f) },
        new[] { P(11,-26.55f),P(10,-26.75f),P(9.3f,-27.18f),P(8.3f,-27.3f),P(7.5f,-27.6f) }
    };
    static float Segment(Vector2 p,Vector2 a,Vector2 b) {
        Vector2 edge=b-a;
        return (p-a-Mathf.Clamp01(Vector2.Dot(p-a,edge)/edge.sqrMagnitude)*edge).sqrMagnitude;
    }
    public static float RoadDistance(Vector2 p) {
        float d=999f;
        foreach(var track in Routes)
            for(int i=1;i<track.Length;i++) d=Mathf.Min(d,Segment(p,track[i-1],track[i]));
        return Mathf.Sqrt(d);
    }
    static float Noise(float x,float y,float scale) =>
        Mathf.PerlinNoise(x*scale+254.17f,y*scale+143.83f);
    static float Border(Vector2 p) {
        float west=Mathf.Lerp(4.1f,12.0f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-17f,-7f,p.y)))
            +Mathf.Sin(p.y*0.82f)*0.65f+Mathf.Sin(p.y*.34f)*0.6f;
        float north=-2.45f-0.43f*Mathf.Sin(p.x*.7f)-0.28f*Mathf.Sin(p.x*1.7f);
        return Mathf.Min(p.x-west,north-p.y);
    }
    public static bool Inside(Vector2 p) => p.y < -1.65f && p.x > 1.7f && Border(p)>=0;
    static readonly Vector2[] Pools = { P(17.6f,-10),P(31.25f,-6.8f),P(7,-24.5f) };
    static readonly Vector2[] Radii = { P(2.25f,1.05f),P(1.2f,.8f),P(1.45f,.88f) };
    static float Pool(Vector2 p) {
        float best=-100f;
        for(int i=0;i<Pools.Length;i++) {
            var q=p-Pools[i]; var r=Radii[i];
            if(Mathf.Abs(q.x)>r.x+0.4f || Mathf.Abs(q.y)>r.y+0.4f) continue;
            float n=Noise(p.x,p.y,2f)-.5f;
            best=Mathf.Max(best,(1-Mathf.Sqrt(q.x*q.x/(r.x*r.x)+q.y*q.y/(r.y*r.y)))*r.y+n*.26f);
        }
        return best;
    }
    // Two navigable half-unit columns remain between the quarter-cell banks, including
    // the navigator's .27-unit clearance, all the way across the broken causeway.
    const float CausewayHalfWidth=1.08f;
    static float CrossingDistance(Vector2 p) => Mathf.Abs((p.x-17.55f)+(p.y+10)*.15f);
    public static bool WaterAt(Vector2 p) => Pool(p)>.025f &&
        (p.x<14 || p.x>22 || CrossingDistance(p)>CausewayHalfWidth);
    static float Oval(Vector2 p,float x,float y,float rx,float ry) {
        var d=p-P(x,y);
        return (1-Mathf.Sqrt(d.x*d.x/(rx*rx)+d.y*d.y/(ry*ry)))*Mathf.Min(rx,ry);
    }
    static float Yard(Vector2 p) {
        return Mathf.Max(
            Mathf.Max(Oval(p,24,-11.6f,4.3f,2.7f),Oval(p,15.5f,-18.8f,2.5f,1.35f)),
            Mathf.Max(Mathf.Max(Oval(p,23.9f,-20.4f,2.9f,1.3f),Oval(p,11,-25.2f,2.1f,1.55f)),
                      Mathf.Max(Oval(p,9,-18.9f,2.0f,1.1f),Oval(p,35,-8.8f,1.45f,1.35f))));
    }
    sealed class Sample {
        Color32[] pixels; int w,h;
        public Sample(string tilePath) {
            var tile=AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if(!tile || !tile.sprite) throw new Exception("Missing terrain source "+tilePath);
            var original=tile.sprite;
            var raw=new Texture2D(2,2,TextureFormat.RGBA32,false);
            raw.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(original.texture)));
            var r=original.rect;
            w=(int)r.width; h=(int)r.height; pixels=new Color32[w*h];
            var source=raw.GetPixels32();
            for(int y=0;y<h;y++)
                Array.Copy(source,((int)r.y+y)*raw.width+(int)r.x,pixels,y*w,w);
            UnityEngine.Object.DestroyImmediate(raw);
        }
        public Color32 At(int x,int y) => pixels[(x%w)+w*(y%h)];
    }
    static Color32 Mix(Color32 a,Color32 b,float t) => Color32.Lerp(a,b,Mathf.Clamp01(t));
    static Color32 Rgb(int r,int g,int b) => new Color32((byte)r,(byte)g,(byte)b,255);
    static Color32 Opaque(Color32 c) {c.a=255;return c;}
    static float Luma(Color32 c) => (c.r*.29f+c.g*.58f+c.b*.13f)/255;
    static Color32 Shade(Color32 c,float f) => Rgb(
        Mathf.RoundToInt(Mathf.Clamp(c.r*f,0,255)),Mathf.RoundToInt(Mathf.Clamp(c.g*f,0,255)),
        Mathf.RoundToInt(Mathf.Clamp(c.b*f,0,255)));
    static Color32 Alpha(Color32 c,float a) {c.a=(byte)Mathf.RoundToInt(Mathf.Clamp01(a)*255);return c;}
    static Color32 PoolPixel(Vector2 p,int i,int j,float pool,float coast,float grain,float mid,
        Sample slate,Sample shore,Sample water) {
        float side=CrossingDistance(p);
        bool bridge=p.x>14 && p.x<21 && side<CausewayHalfWidth+.035f && Mathf.Abs(p.y+10)<1.42f;
        Color32 fill;
        if(bridge) {
            fill=Mix(slate.At(i,j),Rgb(128,131,111),.26f);
            if(side>CausewayHalfWidth-.08f && (i/5+j/6)%7!=0)
                fill=Shade(fill,side>CausewayHalfWidth-.02f?.66f:1.09f);
        } else if(pool<0) {
            fill=Shade(Rgb(75,88,64),.94f+grain*.12f);
        } else {
            var sample=pool<.16f?shore.At(i,j):water.At(i,j);
            fill=Mix(Rgb(30,60,65),Opaque(sample),pool<.16f?.31f:.35f);
            if(pool<.045f) fill=Shade(Rgb(91,128,107),.94f+mid*.15f);
            if(pool>.35f) fill=Shade(fill,.83f);
        }
        return Alpha(fill,coast*Mathf.InverseLerp(-.14f,-.07f,pool));
    }
    public static string RebuildCrossing() {
        string prefix="LostRuinsBiome/Terrain and roads/";
        var wet=GameObject.Find(prefix+"Painted Terrain/Blackwater and Bridge").GetComponent<Tilemap>();
        var obstacle=GameObject.Find(prefix+"Pool Banks/Pool Depth Obstacles").GetComponent<Tilemap>();
        var deep=AssetDatabase.LoadAssetAtPath<Tile>(Folder+"DeepWater.asset");
        string png=Folder+"Pools.png";
        if(!deep || !File.Exists(png)) throw new Exception("The saved Lost Ruins pool assets must exist.");
        var basePath="Assets/DrownedGrove/Tiles/";
        var slate=new Sample(basePath+"AncientTempleFloorTile.asset");
        var shore=new Sample(basePath+"ShallowEmeraldWaterTile.asset");
        var water=new Sample(basePath+"DeepAbyssWaterTile.asset");
        var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);
        tex.LoadImage(File.ReadAllBytes(png));
        if(tex.width!=Width || tex.height!=Height)throw new Exception("Unexpected pool atlas dimensions");
        var colors=tex.GetPixels32();
        for(int j=20*Ppu;j<24*Ppu;j++)for(int i=15*Ppu;i<21*Ppu;i++) {
            var p=P((i+.5f)/Ppu,(j+.5f)/Ppu-32);
            float pool=Pool(p);
            if(pool<-.14f)continue;
            colors[j*Width+i]=PoolPixel(p,i,j,pool,1,Noise(p.x,p.y,6),Noise(p.x,p.y,1.55f),slate,shore,water);
        }
        tex.SetPixels32(colors);tex.Apply();File.WriteAllBytes(png,tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(png,ImportAssetOptions.ForceSynchronousImport);
        int opened=0;
        for(int ix=60;ix<84;ix++)for(int iy=-48;iy<-32;iy++) {
            var cell=new Vector3Int(ix,iy,0);
            bool blocked=WaterAt(P((ix+.5f)*.25f,(iy+.5f)*.25f));
            if(obstacle.HasTile(cell)==blocked) continue;
            obstacle.SetTile(cell,blocked?deep:null);
            if(!blocked)opened++;
        }
        wet.RefreshAllTiles();obstacle.CompressBounds();
        var curb=obstacle.GetComponent<TilemapCollider2D>();curb.ProcessTilemapChanges();
        obstacle.GetComponent<CompositeCollider2D>().GenerateGeometry();
        PrefabUtility.RecordPrefabInstancePropertyModifications(obstacle);
        EditorUtility.SetDirty(obstacle);
        Physics2D.SyncTransforms();
        UnityEngine.Object.FindAnyObjectByType<ForestNavigation>().Invalidate();
        return "Widened existing broken causeway and opened "+opened+" quarter cells.";
    }
    static Tilemap Layer(Transform parent,string title,int order,Material mat) {
        var go=new GameObject(title,typeof(Tilemap),typeof(TilemapRenderer));
        go.transform.SetParent(parent,false);
        var renderer=go.GetComponent<TilemapRenderer>(); renderer.sortingLayerName="Ground";
        renderer.sortingOrder=order; renderer.sharedMaterial=mat;
        return go.GetComponent<Tilemap>();
    }
    static Transform GridGroup(Transform parent,string name,float spacing) {
        var go=new GameObject(name,typeof(Grid));
        go.transform.SetParent(parent,false);
        go.GetComponent<Grid>().cellSize=new Vector3(spacing,spacing,1);
        return go.transform;
    }
    // Each authored 4x4-world patch stays separately paintable. PNGs store the original
    // project textures blended at pixel scale along the stone, moss, and wet banks.
    static void BakeLayer(Tilemap map,string name,Color32[] colors,bool[] used) {
        int size=Stamp*Ppu,nx=Width/size;
        var png=Folder+name+".png"; var tiles=Folder+name+".asset";
        var image=new Texture2D(Width,Height,TextureFormat.RGBA32,false);
        image.SetPixels32(colors); image.Apply();
        File.WriteAllBytes(png,image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
        AssetDatabase.ImportAsset(png,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(png);
        importer.textureType=TextureImporterType.Default; importer.mipmapEnabled=false;
        importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
        importer.filterMode=FilterMode.Point; importer.wrapMode=TextureWrapMode.Clamp;
        importer.alphaIsTransparency=true; importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(png);
        // Re-running the editor-only builder reuses existing persistent tile/sprite IDs.
        var palette=AssetDatabase.LoadAssetAtPath<Tile>(tiles);
        if(!palette){palette=ScriptableObject.CreateInstance<Tile>();palette.name=name;AssetDatabase.CreateAsset(palette,tiles);}
        var former=new Dictionary<string,Tile>();
        foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(tiles)) if(obj is Tile t) former[t.name]=t;
        for(int i=0;i<used.Length;i++) {
            if(!used[i]) continue;
            int cx=i%nx,cy=i/nx;
            string id=name+" "+cx+"_"+cy;
            Tile tile;
            if(!former.TryGetValue(id,out tile)) {
                Sprite sprite=Sprite.Create(tex,new Rect(cx*size,cy*size,size,size),P(.5f,.5f),Ppu,0,SpriteMeshType.FullRect);
                sprite.name=id+" art";
                AssetDatabase.AddObjectToAsset(sprite,palette);
                tile=ScriptableObject.CreateInstance<Tile>(); tile.name=id;tile.sprite=sprite;
                tile.colliderType=Tile.ColliderType.None;AssetDatabase.AddObjectToAsset(tile,palette);
                EditorUtility.SetDirty(tile);
            }
            var cell=new Vector3Int(cx,cy-Height/size,0);
            if(map.GetTile(cell)!=tile)map.SetTile(cell,tile);
        }
        map.CompressBounds();
        EditorUtility.SetDirty(palette);
    }
    public static string PolishExisting() {
        var parent=GameObject.Find("LostRuinsBiome/Terrain and roads").transform;
        var nav=UnityEngine.Object.FindAnyObjectByType<ForestNavigation>();
        BuildMaps(parent,nav.ground,nav.ground.GetComponent<TilemapRenderer>().sharedMaterial,true);
        return RebuildCrossing()+" Polished the saved paving and linked the pilgrims' landing.";
    }
    public static void Build(Transform parent,Tilemap ground,Material mat) => BuildMaps(parent,ground,mat,false);
    static void BuildMaps(Transform parent,Tilemap ground,Material mat,bool repaintOnly) {
        Directory.CreateDirectory(Folder);
        var terrain=repaintOnly?parent.Find("Painted Terrain"):GridGroup(parent,"Painted Terrain",Stamp);
        Tilemap GetLayer(Transform g,string name,int order) =>
            repaintOnly?g.Find(name).GetComponent<Tilemap>():Layer(g,name,order,mat);
        var mossMap=GetLayer(terrain,"Weathered Moss",4);
        var pathMap=GetLayer(terrain,"Broken Roads and Courts",6);
        var waterMap=GetLayer(terrain,"Blackwater and Bridge",7);
        var smallGrid=repaintOnly?parent.Find("Pool Banks"):GridGroup(parent,"Pool Banks",.25f);
        var obstacles=GetLayer(smallGrid,"Pool Depth Obstacles",5);
        obstacles.GetComponent<TilemapRenderer>().enabled=false;
        string baseTiles="Assets/DrownedGrove/Tiles/";
        var grass=new Sample("Assets/ForestTiles/Grass_37.asset");
        var road=new Sample(baseTiles+"MossyRuinRoadTile.asset");
        var slate=new Sample(baseTiles+"AncientTempleFloorTile.asset");
        var cobble=new Sample("Assets/AshenFrontier/Tiles/CrackedCobble.asset");
        var brick=new Sample("Assets/AshenFrontier/Tiles/RuinBrick_1.asset");
        var water=new Sample(baseTiles+"DeepAbyssWaterTile.asset");
        var shore=new Sample(baseTiles+"ShallowEmeraldWaterTile.asset");
        var mossPixels=new Color32[Width*Height];var roadPixels=new Color32[Width*Height];var poolPixels=new Color32[Width*Height];
        int patchCount=Width*Height/(Stamp*Ppu*Stamp*Ppu);
        var mUsed=new bool[patchCount]; var pUsed=new bool[patchCount]; var wUsed=new bool[patchCount];
        var land=new bool[42,34];
        for(int y=-33;y<=0;y++)for(int x=-1;x<=40;x++)
            land[x+1,y+33]=ground.HasTile(ground.WorldToCell(new Vector3(x+.5f,y+.5f,0)));
        // Cache route distances at one quarter tile, then interpolate a continuous path edge.
        var roadGrid=new float[Width/4+1,Height/4+1];
        for(int y=0;y<=Height/4;y++)for(int x=0;x<=Width/4;x++)
            roadGrid[x,y]=RoadDistance(P(x*4f/Ppu,y*4f/Ppu-32));
        for(int j=0;j<Height;j++)for(int i=0;i<Width;i++) {
            float x=(i+.5f)/Ppu,y=(j+.5f)/Ppu-32;
            int gx=i/Ppu+1,gy=j/Ppu+1;
            if(!land[gx,gy] || y>=-.45f || x<=.45f)continue;
            var p=P(x,y);float edge=Border(p);
            float u=((i%4)+.5f)*.25f,v=((j%4)+.5f)*.25f;
            int c=i/4,r=j/4;
            float d=Mathf.Lerp(Mathf.Lerp(roadGrid[c,r],roadGrid[c+1,r],u),
                               Mathf.Lerp(roadGrid[c,r+1],roadGrid[c+1,r+1],u),v);
            if(edge < -1.2f && d>1f) continue;
            float shoreClear=2f;
            for(int sy=-1;sy<=1;sy++)for(int sx=-1;sx<=1;sx++) {
                if(land[gx+sx,gy+sy]) continue;
                float dx=Mathf.Max(0,Mathf.Abs(x-(gx-1+.5f+sx))-.5f);
                float dy=Mathf.Max(0,Mathf.Abs(y-(gy-33+.5f+sy))-.5f);
                shoreClear=Mathf.Min(shoreClear,Mathf.Sqrt(dx*dx+dy*dy));
            }
            float coast=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.26f,1.18f,shoreClear));
            if(coast<=0) continue;
            float grain=Noise(x,y,6f),big=Noise(x,y,.36f),mid=Noise(x,y,1.55f);
            float border=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.8f,1.85f,edge+(mid-.5f)*.55f));
            int k=j*Width+i, patch=(i/(Stamp*Ppu))+(Width/(Stamp*Ppu))*(j/(Stamp*Ppu));
            Color32 baseGrass=grass.At(i*2,j*2);
            float tone=(baseGrass.g-160f)*.018f+(baseGrass.r-145f)*.015f;
            var moss=Mix(Rgb(91,117,76),Rgb(128,145,83),(big-.21f)*1.65f);
            moss=Shade(moss,.88f+tone*.095f+mid*.08f);
            if(border>0) {mossPixels[k]=Alpha(moss,border*coast*.94f);mUsed[patch]=true;}
            float grit=(mid-.5f)*.35f+(grain-.5f)*.29f;
            bool landing=y<-26.6f && x<10.8f;
            float width=(landing?.37f:.68f)+.13f*Mathf.Sin(x*.57f-y*.33f);
            float yard=Yard(p)+(mid-.5f)*.38f;
            float surface=Mathf.Max(width-d+grit,yard);
            if(surface>-.32f) {
                // A continuous bed of the pack's native masonry stays visible through
                // the wear. Paving changes in broken flag-sized groups rather than blobs.
                var block=brick.At(i,j);
                float relief=Mathf.InverseLerp(.30f,.49f,Luma(block));
                var stone=Mix(Rgb(87,91,79),Rgb(151,150,124),relief);
                float eroded=Noise(x,y,.94f);
                float nativeWear=Mathf.InverseLerp(.30f,.68f,eroded)*.24f;
                stone=Mix(stone,cobble.At(i,j),nativeWear);
                var older=yard>.12f?slate.At(i,j):road.At(i,j);
                // Subdue the strong four-corner stamp of the original temple tile.
                older=Mix(Rgb(117,121,106),older,.4f);
                float flag=Noise(Mathf.Floor(x*2)/2,Mathf.Floor(y*2)/2,1.65f);
                float pockets=Mathf.InverseLerp(.39f,.67f,flag)*.58f;
                stone=Mix(stone,older,pockets);
                float intact=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.2f,.17f,surface));
                float loose=Mathf.InverseLerp(.40f,.74f,grain)*(1-intact);
                float entryFade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,2.65f,Mathf.Min(x,-y)+(grain-.5f)*.6f));
                float opacity=(Mathf.InverseLerp(-.32f,.08f,surface)*(.40f+.55f*intact)+loose*.25f)*coast*entryFade;
                var earth=Mix(Shade(Rgb(128,122,84),.88f+mid*.20f),moss,.25f);
                var paving=Mix(earth,stone,Mathf.Clamp01(intact*.95f+loose*.55f));
                if(landing)opacity*=.48f+grain*.3f;
                roadPixels[k]=Alpha(paving,opacity); if(roadPixels[k].a!=0)pUsed[patch]=true;
            }
            float pool=Pool(p);
            if(pool<-.14f)continue;
            poolPixels[k]=PoolPixel(p,i,j,pool,coast,grain,mid,slate,shore,water);
            if(poolPixels[k].a!=0)wUsed[patch]=true;
        }
        BakeLayer(pathMap,"Paving",roadPixels,pUsed);
        if(repaintOnly) {
            pathMap.RefreshAllTiles();
            PrefabUtility.RecordPrefabInstancePropertyModifications(pathMap);
            EditorUtility.SetDirty(pathMap);
            AssetDatabase.SaveAssets();
            return;
        }
        BakeLayer(mossMap,"Moss",mossPixels,mUsed);
        BakeLayer(waterMap,"Pools",poolPixels,wUsed);
        string colliderPath=Folder+"DeepWater.asset";
        var deep=AssetDatabase.LoadAssetAtPath<Tile>(colliderPath);
        if(!deep) {
            deep=ScriptableObject.CreateInstance<Tile>();deep.name="DeepWater";
            deep.sprite=AssetDatabase.LoadAssetAtPath<Tile>(baseTiles+"DeepAbyssWaterTile.asset").sprite;
            deep.colliderType=Tile.ColliderType.Grid;AssetDatabase.CreateAsset(deep,colliderPath);
        }
        for(int ix=0;ix<160;ix++)for(int iy=-128;iy<0;iy++) {
            Vector2 p=P((ix+.5f)*.25f,(iy+.5f)*.25f);
            if(WaterAt(p))obstacles.SetTile(new Vector3Int(ix,iy,0),deep);
        }
        obstacles.CompressBounds();
        var body=obstacles.gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Static;
        var composite=obstacles.gameObject.AddComponent<CompositeCollider2D>();
        composite.geometryType=CompositeCollider2D.GeometryType.Polygons;
        var boundary=obstacles.gameObject.AddComponent<TilemapCollider2D>();
        boundary.compositeOperation=Collider2D.CompositeOperation.Merge;boundary.ProcessTilemapChanges();
        composite.GenerateGeometry();
        AssetDatabase.SaveAssets();
    }
}
