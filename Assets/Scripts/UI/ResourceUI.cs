using UnityEngine;
using UnityEngine.UI;
public class ResourceUI : MonoBehaviour {
    public TemporaryResourceWallet wallet;
    public Text totals;
    [Header("Optional per-resource chips (leave empty for the single-line label)")]
    public Text woodText, stoneText, scrapText;
    void OnEnable() {
        if(!wallet||!totals){Debug.LogError("Resource HUD needs a wallet and label",this);enabled=false;return;}
        wallet.Changed+=Refresh;
        Refresh();
    }
    void Start()=>Refresh();
    void OnDisable(){if(wallet)wallet.Changed-=Refresh;}
    void Refresh(){
        int wood=wallet.Wood, stone=wallet.GetResourceCount(ResourceType.Stone), scrap=wallet.GetResourceCount(ResourceType.Scrap);
        if(woodText&&stoneText&&scrapText){totals.text="";woodText.text=wood.ToString();stoneText.text=stone.ToString();scrapText.text=scrap.ToString();return;}
        totals.text=$"WOOD {wood}     STONE {stone}     SCRAP {scrap}";
    }
}
