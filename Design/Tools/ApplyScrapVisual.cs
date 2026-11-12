using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
public static class ApplyScrapVisual {
 public static object Main(){
 if(Application.isPlaying) throw new System.Exception("Edit mode required"); const string path="Assets/Art/Resources/RustyScrap.png";
 AssetDatabase.ImportAsset(path);
 var importer=(TextureImporter)AssetImporter.GetAtPath(path);
 importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
 importer.spritePixelsPerUnit=1280; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
 importer.alphaIsTransparency=true; importer.textureCompression=TextureImporterCompression.Uncompressed;
 importer.maxTextureSize=2048; importer.SaveAndReimport();
 var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
 var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Items/scrap.asset"); var old=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/Terrain/Resources/Tools/Tool_04.png");
 int count=0;
 foreach(var sr in Object.FindObjectsByType<SpriteRenderer>()) if(sr.sprite==old){Undo.RecordObject(sr,"Replace scrap visual");sr.sprite=sprite;EditorUtility.SetDirty(sr);count++;}
 foreach(var n in Object.FindObjectsByType<ResourceNode>()) if(n.type==ResourceType.Scrap){Undo.RecordObject(n,"Replace scrap drop");n.dropSprite=sprite;EditorUtility.SetDirty(n);}
 var so=new SerializedObject(item);so.FindProperty("icon").objectReferenceValue=sprite;so.ApplyModifiedProperties();
 const string prefabPath="Assets/AshenFrontier/Prefabs/ScrapPileNode.prefab";
 var root=PrefabUtility.LoadPrefabContents(prefabPath);
 try{foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>())if(sr.sprite==old)sr.sprite=sprite;foreach(var n in root.GetComponentsInChildren<ResourceNode>())if(n.type==ResourceType.Scrap)n.dropSprite=sprite;PrefabUtility.SaveAsPrefabAsset(root,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
 return new{updated=count,icon=item.Icon.name};
 }
}

