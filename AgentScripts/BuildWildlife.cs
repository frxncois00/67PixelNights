using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;

// Run from the project: unity command script run --file AgentScripts/BuildWildlife.cs --entry BuildWildlife.Main
public static class BuildWildlife
{
    const string Root = "Assets/Wildlife";
    static void Set(SerializedObject so, string name, string value) => so.FindProperty(name).stringValue = value;
    static Sprite[] Frames(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
        .OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();
    static T Asset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (!asset) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
        return asset;
    }
    static Sprite Icon(string id)
    {
        // Small pixel inventory symbols; animal art itself uses the installed packs.
        var tex = new Texture2D(16,16,TextureFormat.RGBA32,false);
        tex.SetPixels(new Color[256]);
        Color ink = new Color32(62,40,31,255), cream = new Color32(239,228,195,255);
        void Dot(int x,int y,Color c) { if(x>=0&&x<16&&y>=0&&y<16)tex.SetPixel(x,y,c); }
        void Box(int x,int y,int w,int h,Color c) { for(int a=x;a<x+w;a++)for(int b=y;b<y+h;b++)Dot(a,b,c); }
        void Oval(int x,int y,int rx,int ry,Color c) { for(int a=-rx;a<=rx;a++)for(int b=-ry;b<=ry;b++)if(a*a/(float)(rx*rx)+b*b/(float)(ry*ry)<=1)Dot(x+a,y+b,c); }
        if(id=="egg") { Oval(8,7,5,7,ink);Oval(8,7,4,6,cream);Oval(7,9,2,3,Color.white); }
        if(id=="milk") { Box(4,2,8,9,ink);Box(6,10,4,4,ink);Box(5,3,6,7,cream);Box(7,10,2,3,cream);Box(5,6,6,2,new Color32(80,161,193,255));Box(6,13,4,2,new Color32(168,65,54,255)); }
        if(id=="wool") { for(int x=5;x<=11;x+=3)for(int y=5;y<=11;y+=3)Oval(x,y,3,3,ink);for(int x=5;x<=11;x+=3)for(int y=5;y<=11;y+=3)Oval(x,y,2,2,cream); }
        if(id=="leather") { Box(4,3,8,10,ink);Box(2,2,3,3,ink);Box(11,2,3,3,ink);Box(2,11,3,3,ink);Box(11,11,3,3,ink);Box(5,4,6,8,new Color32(170,100,55,255));Box(6,5,2,6,new Color32(206,139,79,255)); }
        if(id=="feathers") { for(int i=2;i<13;i++){Dot(i,i,ink);if(i>4){Box(i-2,i,4,2,ink);Box(i-1,i,2,1,cream);}} }
        if(id=="bone") { Box(3,6,10,4,ink);foreach(int x in new[]{3,12})foreach(int y in new[]{5,10})Oval(x,y,2,2,ink);Box(3,7,10,2,cream);foreach(int x in new[]{3,12})foreach(int y in new[]{5,10})Oval(x,y,1,1,cream); }
        tex.Apply(); string path=Root+"/Icons/"+id+".png";
        File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=24;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static ItemDefinition Item(string id,string label,float hunger)
    {
        var item=Asset<ItemDefinition>("Assets/Items/"+id+".asset");
        var so=new SerializedObject(item);
        Set(so,"itemId",id);Set(so,"displayName",label);
        Set(so,"description",hunger>0?label+". Restores "+hunger+" hunger.":"Animal material: "+label+". Keep for crafting or trade.");
        so.FindProperty("category").enumValueIndex=(int)(hunger>0?ItemCategory.Food:ItemCategory.Utility);
        so.FindProperty("icon").objectReferenceValue=Icon(id);
        so.FindProperty("consumable").boolValue=hunger>0;
        so.FindProperty("hungerRestored").floatValue=hunger;
        so.FindProperty("stackable").boolValue=true;so.FindProperty("droppable").boolValue=true;
        so.FindProperty("maximumStack").intValue=30;
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(item);return item;
    }
    static AnimalData.Drop Drop(ItemDefinition item,int min,int max,float chance=1) => new AnimalData.Drop {item=item,minimum=min,maximum=max,chance=chance};
    static PassiveAnimal Species(string id,string label,Sprite[] idle,Sprite[] walk,float health,float speed,float radius,bool facesLeft,AnimalData.Drop[] drops,Material material)
    {
        var data=Asset<AnimalData>(Root+"/Data/"+id+".asset");
        data.displayName=label;data.idleFrames=idle;data.walkFrames=walk;data.health=health;
        data.moveSpeed=speed;data.bodyRadius=radius;data.artworkFacesLeft=facesLeft;
        data.roamingRadius=facesLeft?3:4;data.pauseSeconds=facesLeft?new Vector2(2,4):new Vector2(1,3);
        data.fleeSeconds=facesLeft?4:3;data.fleeSpeedMultiplier=1.8f;data.drops=drops;
        EditorUtility.SetDirty(data);
        var go=new GameObject(label);go.SetActive(false);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=idle[0];renderer.sharedMaterial=material;
        renderer.sortingLayerName="Objects";renderer.spriteSortPoint=SpriteSortPoint.Pivot;
        var body=go.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;
        body.constraints=RigidbodyConstraints2D.FreezeRotation;body.interpolation=RigidbodyInterpolation2D.Interpolate;
        go.AddComponent<CircleCollider2D>().radius=radius;
        go.AddComponent<EnemyHealth>().maximumHealth=health;
        var animal=go.AddComponent<PassiveAnimal>();animal.data=data;
        animal.idleFrames=idle;animal.moveFrames=walk;
        go.AddComponent<WildlifeHarvest>();go.AddComponent<EnemyHitFeedback>();
        go.SetActive(true);
        var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+id+".prefab");
        UnityEngine.Object.DestroyImmediate(go);return prefab.GetComponent<PassiveAnimal>();
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before building wildlife assets.");
        foreach(string folder in new[]{Root,Root+"/Data",Root+"/Prefabs",Root+"/Icons"})Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var spawner=UnityEngine.Object.FindAnyObjectByType<WildlifeSpawner>();
        if(!spawner)throw new Exception("Open SampleScene with the existing WildlifeSpawner.");
        var meat=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Items/raw_meat.asset");
        var feather=Item("feathers","Feathers",0);var leather=Item("leather","Leather",0);
        var wool=Item("wool","Wool",0);var egg=Item("egg","Egg",10);var milk=Item("milk","Milk",18);var bone=Item("bone","Bone",0);
        var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("62d5864bc8dc3e244971dbcf683cf27c"));
        string farm="Assets/FarmRPG/Farm RPG FREE 16x16 - Tiny Asset Pack/Farm Animals/";
        string sheep="Assets/Tiny Swords/Terrain/Resources/Meat/Sheep/";
        var female=Frames(farm+"Female Cow Brown.png");var male=Frames(farm+"Male Cow Brown.png");
        spawner.animalPrefabs=new[]{
            Species("Sheep","Sheep",Frames(sheep+"Sheep_Idle.png"),Frames(sheep+"Sheep_Move.png"),30,1.3f,.28f,false,new[]{Drop(meat,1,2),Drop(wool,1,3)},material),
            Species("Cow","Cow",female.Take(1).ToArray(),female.Take(4).ToArray(),55,1,.4f,true,new[]{Drop(meat,2,4),Drop(leather,1,2),Drop(milk,1,1,.25f)},material),
            Species("Bull","Bull",male.Take(1).ToArray(),male.Take(4).ToArray(),70,1.1f,.42f,true,new[]{Drop(meat,3,4),Drop(leather,2,2),Drop(bone,1,1,.3f)},material)};
        spawner.cycle=UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();
        spawner.maximumAnimals=36;spawner.respawnSeconds=30;spawner.playerSpawnDistance=6;spawner.animalSpacing=3;
        var grass=new HashSet<TileBase>();
        foreach(var cell in spawner.ground.cellBounds.allPositionsWithin){var t=spawner.ground.GetTile(cell);if(t&&t.name.StartsWith("Grass",StringComparison.OrdinalIgnoreCase))grass.Add(t);}
        spawner.grassTiles=grass.ToArray();EditorUtility.SetDirty(spawner);
        foreach(var inventory in UnityEngine.Object.FindObjectsByType<InventoryController>(FindObjectsSortMode.None))
        {
            var so=new SerializedObject(inventory);var list=so.FindProperty("definitions");
            foreach(var item in new[]{feather,leather,wool,egg,milk,bone})
            {
                bool exists=false;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==item)exists=true;
                if(!exists){int i=list.arraySize;list.InsertArrayElementAtIndex(i);list.GetArrayElementAtIndex(i).objectReferenceValue=item;}
            }
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(inventory);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);EditorSceneManager.SaveScene(spawner.gameObject.scene);
        return "Configured sheep, cow and bull prefabs; six inventory items; "+grass.Count+" grass tiles; 36-animal cap. Chickens removed from the scene roster.";
    }
}
