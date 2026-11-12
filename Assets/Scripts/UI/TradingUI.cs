using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

// Created on first interaction; uses the same theme assets as the construction menu.
public class TradingUI : MonoBehaviour
{
    static TradingUI active;
    public static bool IsOpen => active && active.root && active.root.activeSelf;
    public static bool Owns(OutpostMerchant vendor, PlayerInteractionController actor) =>
        IsOpen && active.merchant == vendor && active.player == actor;
    OutpostMerchant merchant;
    PlayerInteractionController player;
    InventoryController inventory;
    GameObject root;
    Font font;
    Sprite wood, banner, leaves;
    Text page, stock, paymentName, rewardName, owned, received, status, tradeLabel;
    Image paymentIcon, rewardIcon;
    Button trade, previous, next;
    int index, openedFrame;
    float feedbackUntil, nextRefresh;
    string feedback;
    static readonly Color Cream = new Color(1f,.95f,.84f);
    static readonly Color Gold = new Color(1f,.85f,.42f);
    static readonly Color Muted = new Color(.85f,.79f,.64f);
    static readonly Color Ink = new Color(.20f,.13f,.07f);
    static readonly Color Bronze = new Color(.53f,.37f,.18f);
    static readonly Color Parchment = new Color(.84f,.73f,.53f);
    static readonly Color SagePaper = new Color(.74f,.77f,.55f);

    public void Open(OutpostMerchant vendor, PlayerInteractionController actor)
    {
        if (!vendor || !actor || !actor.actions || actor.actions.Busy || !vendor.Reachable(actor.transform.position)) return;
        var bag = actor.GetComponent<InventoryController>();
        if (!bag || bag.ManagementLocked) return;
        if (active && active != this) active.Close();
        merchant = vendor; player = actor; inventory = bag;
        if (!root) Build();
        index = Mathf.Clamp(vendor.SelectedExchange,0,vendor.exchanges.Length-1);
        active = this; root.SetActive(true); openedFrame = Time.frameCount;
        feedbackUntil = 0;
        ClearSelection(); Refresh();
    }
    public void Close()
    {
        if (root) root.SetActive(false);
        if (active == this) active = null;
        ClearSelection();
    }
    void OnDisable() => Close();
    void OnDestroy() { if (root) Destroy(root); }
    static void ClearSelection() { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); }
    void Update()
    {
        if (!IsOpen || active != this) return;
        if (!merchant || !merchant.Reachable(player.transform.position) || !inventory || inventory.ManagementLocked ||
            PlayerSurvivalController.InputLocked || BuildingController.ModalOpen || InventoryUI.IsOpen || Time.timeScale == 0)
        { Close(); return; }
        var k = Keyboard.current;
        if (Time.frameCount > openedFrame && k != null)
        {
            if (k.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (k.tKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame) Browse(1);
            else if (k.leftArrowKey.wasPressedThisFrame) Browse(-1);
        }
        if (Time.unscaledTime >= nextRefresh)
        { nextRefresh = Time.unscaledTime + .15f; Refresh(); }
    }
    void Browse(int direction)
    {
        if (!merchant || !merchant.Available) { Close(); return; }
        index = (index + direction + merchant.exchanges.Length) % merchant.exchanges.Length;
        feedbackUntil = 0; ClearSelection(); Refresh();
    }
    void Confirm()
    {
        if (!Owns(merchant,player) || !trade.interactable) return;
        if (merchant.TryTrade(player,index))
        {
            var offer = merchant.exchanges[index];
            feedback = "Trade complete! +" + offer.rewardCount + " " + offer.reward.DisplayName;
        }
        else feedback = "Trade could not be completed. Check materials and bag space.";
        feedbackUntil = Time.unscaledTime + 3;
        ClearSelection(); Refresh();
    }
    void Refresh()
    {
        if (!merchant || !merchant.Available) { Close(); return; }
        index = Mathf.Clamp(index,0,merchant.exchanges.Length-1);
        var e = merchant.exchanges[index];
        page.text = "OFFER " + (index+1) + " / " + merchant.exchanges.Length;
        previous.interactable = next.interactable = merchant.exchanges.Length > 1;
        bool valid = e != null && e.payment && e.reward && e.paymentCount > 0 && e.rewardCount > 0;
        paymentIcon.enabled = rewardIcon.enabled = valid;
        if (!valid)
        {
            paymentName.text = rewardName.text = "Unavailable";
            owned.text = received.text = stock.text = "";
            trade.interactable = false; tradeLabel.text = "UNAVAILABLE";
            status.text = "Please choose another offer."; return;
        }
        paymentIcon.sprite = e.payment.Icon; paymentIcon.color = e.payment.IconTint;
        rewardIcon.sprite = e.reward.Icon; rewardIcon.color = e.reward.IconTint;
        paymentName.text = e.paymentCount + " × " + e.payment.DisplayName;
        rewardName.text = e.rewardCount + " × " + e.reward.DisplayName;
        int have = inventory.Count(e.payment);
        owned.text = "Have " + have + "  /  Need " + e.paymentCount;
        owned.color = have >= e.paymentCount ? new Color(.65f,.87f,.49f) : new Color(1f,.63f,.48f);
        received.text = "In your bag: " + inventory.Count(e.reward);
        stock.text = e.stock > 0 ? e.stock + " TRADES LEFT" : "SOLD OUT";
        stock.color = e.stock > 0 ? Gold : new Color(1f,.63f,.48f);
        bool can = e.stock > 0 && inventory.CraftBatch(new[] { new ItemAmount { item=e.payment, amount=e.paymentCount } }, e.reward,e.rewardCount,false);
        trade.interactable = can;
        tradeLabel.text = can ? "TRADE" : e.stock <= 0 ? "SOLD OUT" : have < e.paymentCount ? "NEED MATERIALS" : "BAG FULL";
        status.text = Time.unscaledTime < feedbackUntil ? feedback : e.stock <= 0 ? "This offer is sold out. Browse another trade." :
            have < e.paymentCount ? "Collect " + (e.paymentCount-have) + " more " + e.payment.DisplayName + " to trade." :
            !can ? "Make room in your bag for the reward." : "Ready to trade. Press Trade to confirm.";
    }
    Image Box(Transform parent,string name,float x,float y,float w,float h,Color color,Sprite sprite=null)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
        var r = go.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h);
        var img=go.GetComponent<Image>(); img.color=color; img.sprite=sprite;
        if(sprite)img.type=Image.Type.Sliced;
        return img;
    }
    Text Label(Transform parent,string text,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleCenter,bool shadowed=true)
    {
        var go=new GameObject(text,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
        var r=go.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        var t=go.GetComponent<Text>();t.font=font;t.fontSize=size;t.text=text;t.color=color;t.alignment=align;
        if (shadowed)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0,0,0,.65f); shadow.effectDistance = new Vector2(0,-1);
        }
        t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;
    }
    // Layered pixel borders keep the panels crisp without scaling up the modal.
    Image Frame(Transform parent,string name,float x,float y,float w,float h,Color fill,Color edge)
    {
        var outer=Box(parent,name,x,y,w,h,new Color(.035f,.04f,.03f));
        Box(outer.transform,"Outer trim",2,2,w-4,h-4,edge).raycastTarget=false;
        Box(outer.transform,"Inner shadow",4,4,w-8,h-8,new Color(.055f,.065f,.05f)).raycastTarget=false;
        var surface=Box(outer.transform,"Surface",6,6,w-12,h-12,fill);
        surface.raycastTarget=false;
        return outer;
    }
    Button Button(Transform parent,string text,float x,float y,float w,UnityEngine.Events.UnityAction action,bool primary=false)
    {
        var frame=Frame(parent,text,x,y,w,52,primary?new Color(.66f,.37f,.09f):new Color(.19f,.24f,.25f),
            primary?Gold:new Color(.43f,.47f,.43f));
        var surface=frame.transform.Find("Surface").GetComponent<Image>();
        Box(frame.transform,"Top light",8,7,w-16,2,primary?new Color(1f,.75f,.33f):new Color(.34f,.40f,.40f)).raycastTarget=false;
        var b=frame.gameObject.AddComponent<Button>();b.targetGraphic=surface;
        var colors=b.colors;colors.highlightedColor=new Color(1.35f,1.35f,1.2f);colors.selectedColor=colors.highlightedColor;
        colors.pressedColor=new Color(.65f,.65f,.55f);colors.disabledColor=new Color(.38f,.38f,.38f);colors.fadeDuration=.12f;b.colors=colors;
        Label(frame.transform,text,6,0,w-12,52,20,primary?new Color(1,.98f,.76f):Cream);
        b.onClick.AddListener(action);return b;
    }

    Image ItemCard(Transform parent,string name,float x,Color paper,Color ribbonColor,string heading)
    {
        var card=Frame(parent,name,x,140,304,202,paper,Bronze);
        Box(card.transform,"Paper highlight",8,8,288,2,new Color(1f,.94f,.73f,.6f)).raycastTarget=false;
        Box(card.transform,"Paper lower edge",8,192,288,2,new Color(.40f,.30f,.16f,.35f)).raycastTarget=false;
        // Small ink flecks along the edges give the paper a pixel-art finish.
        for (int i=0;i<7;i++)
        {
            Box(card.transform,"Paper fleck",12+(i%3)*3,48+i*19,3+(i%2)*3,2,new Color(.38f,.28f,.13f,.12f)).raycastTarget=false;
            Box(card.transform,"Paper fleck",278+(i%3)*3,54+i*18,4,2,new Color(.38f,.28f,.13f,.12f)).raycastTarget=false;
        }
        var ribbon=Frame(card.transform,"Item heading",40,-8,224,36,ribbonColor,Bronze);
        Label(ribbon.transform,heading,12,2,200,30,18,Cream);
        Diamond(ribbon.transform,12,18,5,Gold);
        Diamond(ribbon.transform,212,18,5,Gold);
        return card;
    }

    void Diamond(Transform parent,float x,float y,float size,Color color)
    {
        var mark=Box(parent,"Gold detail",x,y,size,size,color);
        mark.rectTransform.pivot=new Vector2(.5f,.5f);
        mark.rectTransform.localRotation=Quaternion.Euler(0,0,45);
        mark.raycastTarget=false;
    }

    void BalanceMark(Transform parent)
    {
        var seal=Frame(parent,"Merchant seal",28,20,42,48,new Color(.16f,.22f,.19f),Bronze);
        // A small balance-scale emblem drawn in the same square pixels as the HUD.
        var points=new[] {new Rect(20,10,2,25),new Rect(9,16,24,2),new Rect(15,35,12,2),
            new Rect(10,18,2,9),new Rect(30,18,2,9),new Rect(6,27,10,2),new Rect(26,27,10,2),
            new Rect(8,29,6,2),new Rect(28,29,6,2)};
        foreach (var point in points) Box(seal.transform,"Scales",point.x,point.y,point.width,point.height,Gold).raycastTarget=false;
    }

    void Build()
    {
        var theme=FindAnyObjectByType<ConstructionUI>();
        font=theme&&theme.customFont?theme.customFont:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(theme){wood=theme.panelWood;banner=theme.panelBanner;leaves=theme.iconLeaves;}
        root=new GameObject("Trading Menu",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var shade=Box(root.transform,"Backdrop",0,0,0,0,new Color(.025f,.035f,.02f,.62f));
        shade.rectTransform.anchorMin=Vector2.zero;shade.rectTransform.anchorMax=Vector2.one;shade.rectTransform.offsetMin=shade.rectTransform.offsetMax=Vector2.zero;
        var card=Box(root.transform,"Merchant",0,0,740,470,wood?Color.white:new Color(.13f,.12f,.08f),wood);
        card.rectTransform.anchorMin=card.rectTransform.anchorMax=card.rectTransform.pivot=new Vector2(.5f,.5f);
        Box(card.transform,"Wood inset",16,14,708,442,new Color(.17f,.12f,.08f)).raycastTarget=false;
        for (int i=0;i<6;i++)
        {
            Box(card.transform,"Plank seam",18,86+i*61,704,2,new Color(.055f,.035f,.02f,.6f)).raycastTarget=false;
            Box(card.transform,"Wood grain",34+(i%3)*120,89+i*61,126,1,new Color(.56f,.37f,.18f,.17f)).raycastTarget=false;
        }
        Box(card.transform,"Header",20,18,700,62,new Color(.13f,.10f,.07f)).raycastTarget=false;
        BalanceMark(card.transform);
        Label(card.transform,"OUTPOST TRADER",84,16,510,36,25,Gold,TextAnchor.MiddleLeft);
        Label(card.transform,"Supplies for the road. Make an exchange.",84,52,510,24,17,Muted,TextAnchor.MiddleLeft);
        Button(card.transform,"CLOSE",604,18,112,Close);
        Box(card.transform,"Divider",28,84,684,1,Bronze).raycastTarget=false;
        Diamond(card.transform,370,84,6,Gold);
        Box(card.transform,"Offer bar",24,94,692,30,new Color(.10f,.08f,.05f)).raycastTarget=false;
        page=Label(card.transform,"",38,97,300,28,16,Muted,TextAnchor.MiddleLeft);
        stock=Label(card.transform,"",402,97,300,28,16,Gold,TextAnchor.MiddleRight);
        var left=ItemCard(card.transform,"Payment",24,Parchment,new Color(.33f,.22f,.11f),"YOU GIVE");
        var right=ItemCard(card.transform,"Reward",412,SagePaper,new Color(.23f,.32f,.14f),"YOU RECEIVE");
        var lp=Frame(left.transform,"Item slot",108,40,88,88,new Color(.12f,.09f,.05f),new Color(.72f,.49f,.18f));
        var rp=Frame(right.transform,"Item slot",108,40,88,88,new Color(.10f,.14f,.07f),new Color(.57f,.62f,.28f));
        paymentIcon=Box(lp.transform,"Material",2,2,84,84,Color.white);paymentIcon.preserveAspect=true;paymentIcon.raycastTarget=false;
        rewardIcon=Box(rp.transform,"Item",2,2,84,84,Color.white);rewardIcon.preserveAspect=true;rewardIcon.raycastTarget=false;
        paymentName=Label(left.transform,"",12,130,280,34,21,Ink,shadowed:false);
        rewardName=Label(right.transform,"",12,130,280,34,21,Ink,shadowed:false);
        Box(left.transform,"Inventory strip",18,165,268,26,new Color(.26f,.20f,.11f)).raycastTarget=false;
        Box(right.transform,"Inventory strip",18,165,268,26,new Color(.20f,.25f,.12f)).raycastTarget=false;
        owned=Label(left.transform,"",20,165,264,26,16,Cream);
        received=Label(right.transform,"",20,165,264,26,16,Cream);
        var arrowSeal=Box(card.transform,"Exchange seal",370,238,42,42,new Color(.34f,.22f,.09f));
        arrowSeal.rectTransform.pivot=new Vector2(.5f,.5f);arrowSeal.rectTransform.localRotation=Quaternion.Euler(0,0,45);arrowSeal.raycastTarget=false;
        Label(card.transform,">",340,214,60,44,32,Gold);
        Box(card.transform,"Trade message",24,350,692,38,Color.white,banner).raycastTarget=false;
        status=Label(card.transform,"",38,350,664,38,17,Cream);
        previous=Button(card.transform,"< PREVIOUS",24,400,180,()=>Browse(-1));
        trade=Button(card.transform,"TRADE",220,400,300,Confirm,true);tradeLabel=trade.GetComponentInChildren<Text>();
        next=Button(card.transform,"NEXT [T] >",536,400,180,()=>Browse(1));
        if(leaves)
        {
            var ornament=Box(card.transform,"Leaf corner",-4,-4,32,32,Color.white,leaves);
            ornament.type=Image.Type.Simple;ornament.preserveAspect=true;ornament.raycastTarget=false;
            ornament=Box(card.transform,"Leaf corner",744,474,32,32,Color.white,leaves);
            ornament.type=Image.Type.Simple;ornament.preserveAspect=true;ornament.raycastTarget=false;
            ornament.rectTransform.localRotation=Quaternion.Euler(0,0,180);
        }
        root.SetActive(false);
    }
}
