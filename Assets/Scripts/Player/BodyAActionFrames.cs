using UnityEngine;
public class BodyAActionFrames : MonoBehaviour {
 public Sprite[] crushDown,crushUp,crushSide,sliceDown,sliceUp,sliceSide,carryDown,carryUp,carrySide;
 public Sprite[] pierceDown,pierceUp,pierceSide;
 public Sprite[] Choose(Vector2 direction,bool chop,bool sword,bool mining=false){bool side=Mathf.Abs(direction.x)>=Mathf.Abs(direction.y);return !chop?(side?carrySide:direction.y>0?carryUp:carryDown):sword?(side?pierceSide:direction.y>0?pierceUp:pierceDown):mining?(side?crushSide:direction.y>0?crushUp:crushDown):(side?sliceSide:direction.y>0?sliceUp:sliceDown);}
}
