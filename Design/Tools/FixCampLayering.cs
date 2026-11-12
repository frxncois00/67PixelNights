using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
public static class FixCampLayering {
 public static object Main(){
 if(Application.isPlaying) throw new System.Exception("Edit mode required");
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 var bushes=Object.FindObjectsByType<SpriteRenderer>().Where(s=>s.gameObject.scene==scene && s.name.StartsWith("Bush_") && !s.GetComponent<Collider2D>()).ToArray();
 foreach(var s in bushes){Undo.RecordObject(s,"Lower decorative foliage");s.sortingLayerName="Ground";s.sortingOrder=11;EditorUtility.SetDirty(s);}
 var meat=Object.FindObjectsByType<SpriteRenderer>().Where(p=>p.gameObject.scene==scene && p.name.StartsWith("Meat_") && Vector2.Distance(p.transform.position,Vector2.zero)<9).ToArray();
 var names=meat.Select(p=>p.name).ToArray();foreach(var p in meat) Undo.DestroyObjectImmediate(p.gameObject);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 return new{foliage=bushes.Length,removed=names};
 }
}

