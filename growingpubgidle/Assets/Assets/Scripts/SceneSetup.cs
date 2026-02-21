#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class SceneSetup
{
    private const string PREFAB_PATH = "Assets/Prefabs";

    [MenuItem("Tools/Setup Game Scene")]
    public static void SetupScene()
    {
        EnsureFolder(PREFAB_PATH);
        EnsureLayer("Obstacle");

        Sprite spr = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // ══ Prefabs ══
        string playerPath = MakePrefab(spr, "Player", Color.white, 1.5f, true, false, true);
        string botPath    = MakePrefab(spr, "Bot", new Color(0.9f,0.2f,0.2f), 1.5f, true, true, false);
        string bulletPath = MakeBulletPrefab(spr);
        string lootPath   = MakeLootPrefab(spr);
        string portalPath = MakePortalPrefab(spr);
        string particlePath = MakeParticlePrefab(spr);
        string buildingPath = MakeBuildingPrefab(spr);
        string rockPath     = MakeRockPrefab(spr);
        string groundPath   = MakeGroundPrefab(spr);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ══ Scene Objects ══

        // GameManager
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        var gmS = new SerializedObject(gm);
        gmS.FindProperty("playerPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
        gmS.FindProperty("botPrefab").objectReferenceValue    = AssetDatabase.LoadAssetAtPath<GameObject>(botPath);
        gmS.FindProperty("bulletPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
        gmS.FindProperty("lootBoxPrefab").objectReferenceValue= AssetDatabase.LoadAssetAtPath<GameObject>(lootPath);
        gmS.FindProperty("portalPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(portalPath);
        gmS.ApplyModifiedProperties();

        // MapGenerator
        var mgGo = new GameObject("MapGenerator");
        var mg = mgGo.AddComponent<MapGenerator>();
        var mgS = new SerializedObject(mg);
        mgS.FindProperty("buildingPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(buildingPath);
        mgS.FindProperty("rockPrefab").objectReferenceValue     = AssetDatabase.LoadAssetAtPath<GameObject>(rockPath);
        mgS.FindProperty("groundPrefab").objectReferenceValue   = AssetDatabase.LoadAssetAtPath<GameObject>(groundPath);
        mgS.ApplyModifiedProperties();

        // ParticleSpawner
        var psGo = new GameObject("ParticleSpawner");
        var ps = psGo.AddComponent<ParticleSpawner>();
        var psS = new SerializedObject(ps);
        psS.FindProperty("particlePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(particlePath);
        psS.ApplyModifiedProperties();

        // BlueZone
        var bzGo = new GameObject("BlueZone");
        var bz = bzGo.AddComponent<BlueZone>();
        var lr = bzGo.AddComponent<LineRenderer>();
        lr.startWidth = 0.5f; lr.endWidth = 0.5f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = new Color(0.3f,0.5f,1f,0.8f);
        lr.startColor = lr.endColor = new Color(0.3f,0.5f,1f,0.8f);
        lr.sortingOrder = 5;
        var bzS = new SerializedObject(bz);
        bzS.FindProperty("circleRenderer").objectReferenceValue = lr;
        bzS.ApplyModifiedProperties();

        // Camera
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 15f;
            cam.backgroundColor = new Color(0.15f,0.15f,0.2f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (!cam.GetComponent<CameraController>()) cam.gameObject.AddComponent<CameraController>();
        }

        // ══ UI ══
        BuildUI();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("✅ Setup complete! Press Play to test. WASD=Move, Mouse=Aim, Click=Shoot");
    }

    // ═══════════════════════════════════
    static string MakePrefab(Sprite spr, string name, Color col, float scale, bool hasRb, bool isBot, bool isPlayer)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr; sr.color = col;
        go.transform.localScale = Vector3.one * scale;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        go.AddComponent<CircleCollider2D>().radius = 0.5f;
        go.AddComponent<EntityBase>();
        if (isPlayer) go.AddComponent<PlayerController>();
        if (isBot) go.AddComponent<BotController>();
        // Muzzle (direction indicator)
        var m = new GameObject("Muzzle");
        m.transform.SetParent(go.transform);
        m.transform.localPosition = new Vector3(0.8f,0,0);
        m.transform.localScale = new Vector3(0.3f,0.15f,1);
        var msr = m.AddComponent<SpriteRenderer>();
        msr.sprite = spr;
        msr.color = isBot ? new Color(0.5f,0.1f,0.1f) : new Color(0.3f,0.3f,0.3f);
        return Save(go);
    }

    static string MakeBulletPrefab(Sprite spr)
    {
        var go = new GameObject("Bullet");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr; sr.color = new Color(1f,0.9f,0.2f);
        go.transform.localScale = Vector3.one * 0.3f;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f; col.isTrigger = true;
        go.AddComponent<Bullet>();
        return Save(go);
    }

    static string MakeLootPrefab(Sprite spr)
    {
        var go = new GameObject("LootBox");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr; sr.color = new Color(1f,0.84f,0f);
        go.transform.localScale = Vector3.one;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f; col.isTrigger = true;
        go.AddComponent<LootBox>();
        return Save(go);
    }

    static string MakePortalPrefab(Sprite spr)
    {
        var go = new GameObject("Portal");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr; sr.color = new Color(0.2f,1f,0.4f,0.6f);
        go.transform.localScale = Vector3.one * 5f;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f; col.isTrigger = true;
        go.AddComponent<Portal>();
        return Save(go);
    }

    static string MakeParticlePrefab(Sprite spr)
    {
        var go = new GameObject("Particle");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr; sr.color = Color.white;
        go.transform.localScale = Vector3.one * 0.2f;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        return Save(go);
    }

    static string MakeBuildingPrefab(Sprite spr)
    {
        var go = new GameObject("Building");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr; sr.color = new Color(0.5f,0.5f,0.55f);
        go.AddComponent<BoxCollider2D>();
        go.layer = LayerMask.NameToLayer("Obstacle");
        return Save(go);
    }

    static string MakeRockPrefab(Sprite spr)
    {
        var go = new GameObject("Rock");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr; sr.color = new Color(0.55f,0.4f,0.3f);
        go.AddComponent<CircleCollider2D>();
        go.layer = LayerMask.NameToLayer("Obstacle");
        return Save(go);
    }

    static string MakeGroundPrefab(Sprite spr)
    {
        var go = new GameObject("Ground");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr; sr.color = new Color(0.35f,0.55f,0.25f);
        sr.sortingOrder = -10;
        return Save(go);
    }

    static string Save(GameObject go)
    {
        string p = $"{PREFAB_PATH}/{go.name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, p);
        Object.DestroyImmediate(go);
        return p;
    }

    // ═══════════════════════════════════
    static void BuildUI()
    {
        var canvasGo = new GameObject("GameCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        if (!Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>())
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var ui = canvasGo.AddComponent<UIManager>();
        var so = new SerializedObject(ui);

        so.FindProperty("phaseText").objectReferenceValue       = MkTMP(canvasGo.transform, "PhaseText",    "Menu",        24, .5f,1, .5f,1, 0,-20, 400,40);
        so.FindProperty("timerText").objectReferenceValue       = MkTMP(canvasGo.transform, "TimerText",    "60",          28, .5f,1, .5f,1, 0,-60, 200,40);
        so.FindProperty("aliveCounterText").objectReferenceValue= MkTMP(canvasGo.transform, "AliveText",    "Alive: 10",    18, 0,1, 0,1, 20,-20, 200,30);
        so.FindProperty("killCounterText").objectReferenceValue = MkTMP(canvasGo.transform, "KillText",     "Kills: 0",       18, 0,1, 0,1, 20,-50, 200,30);
        so.FindProperty("altitudeText").objectReferenceValue    = MkTMP(canvasGo.transform, "AltText",      "Alt: 100pt", 16, 1,1, 1,1, -20,-20, 200,30);
        so.FindProperty("weaponText").objectReferenceValue      = MkTMP(canvasGo.transform, "WeaponText",   "Weapon: Fist",  16, 1,1, 1,1, -20,-50, 250,30);
        so.FindProperty("lootCountText").objectReferenceValue   = MkTMP(canvasGo.transform, "LootText",     "Loot: 0/7",   16, 1,1, 1,1, -20,-80, 250,30);

        // HP bar
        var hpBg = MkBar(canvasGo.transform, "HPBar", 0,0, 160,30);
        so.FindProperty("hpBarFill").objectReferenceValue = MkFill(hpBg.transform, "HPF", Color.green);

        // Stamina bar
        var stBg = MkBar(canvasGo.transform, "StBar", 0,0, 160,55);
        so.FindProperty("staminaBarFill").objectReferenceValue = MkFill(stBg.transform, "StF", new Color(1,.84f,0));

        // Center Panel
        var cp = new GameObject("CenterPanel", typeof(RectTransform), typeof(Image));
        cp.transform.SetParent(canvasGo.transform, false);
        var cpr = cp.GetComponent<RectTransform>();
        cpr.anchorMin = cpr.anchorMax = new Vector2(.5f,.5f);
        cpr.sizeDelta = new Vector2(600,400);
        cp.GetComponent<Image>().color = new Color(.1f,.1f,.15f,.9f);
        so.FindProperty("centerPanel").objectReferenceValue = cp;

        so.FindProperty("panelTitleText").objectReferenceValue = MkTMP(cp.transform, "Title", "GROWING PUBG IDLE", 32, .5f,1,.5f,1, 0,-30, 550,60);
        so.FindProperty("panelDescText").objectReferenceValue  = MkTMP(cp.transform, "Desc",  "WASD=Move/Mouse=Aim/Click=Attack\nShift=Sprint\nLoot 7 boxes then portal escape!", 18, .5f,.5f,.5f,.5f, 0,20, 500,200);

        // Start button
        var btn = new GameObject("StartBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btn.transform.SetParent(cp.transform, false);
        var br = btn.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = new Vector2(.5f,0);
        br.anchoredPosition = new Vector2(0,60);
        br.sizeDelta = new Vector2(250,60);
        btn.GetComponent<Image>().color = new Color(.2f,.6f,.9f);
        so.FindProperty("startButton").objectReferenceValue = btn.GetComponent<Button>();

        var btxt = MkTMP(btn.transform, "BtnTxt", "START GAME", 24, 0,0,1,1, 0,0, 0,0);
        var btr = (btxt as Component).GetComponent<RectTransform>();
        btr.anchorMin = Vector2.zero; btr.anchorMax = Vector2.one;
        btr.offsetMin = btr.offsetMax = Vector2.zero;
        so.FindProperty("startButtonText").objectReferenceValue = btxt;

        // Damage overlay
        var ov = new GameObject("DmgOverlay", typeof(RectTransform), typeof(Image));
        ov.transform.SetParent(canvasGo.transform, false);
        var ovr = ov.GetComponent<RectTransform>();
        ovr.anchorMin = Vector2.zero; ovr.anchorMax = Vector2.one;
        ovr.offsetMin = ovr.offsetMax = Vector2.zero;
        var ovi = ov.GetComponent<Image>();
        ovi.color = new Color(1,0,0,0);
        ovi.raycastTarget = false;
        so.FindProperty("damageOverlay").objectReferenceValue = ovi;

        // Log
        so.FindProperty("logText").objectReferenceValue = MkTMP(canvasGo.transform, "LogText", "", 14, 0,0,0,0, 20,100, 500,120);

        so.ApplyModifiedProperties();
    }

    static TMP_Text MkTMP(Transform parent, string name, string text, int size,
        float ax, float ay, float bx, float by, float px, float py, float sw, float sh)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(ax,ay);
        r.anchorMax = new Vector2(bx,by);
        r.anchoredPosition = new Vector2(px,py);
        r.sizeDelta = new Vector2(sw,sh);
        tmp.text = text;
        tmp.fontSize = size;
        tmp.raycastTarget = false;
        return tmp;
    }

    static GameObject MkBar(Transform parent, string name, float ax, float ay, float px, float py)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(ax,ay);
        r.anchoredPosition = new Vector2(px,py);
        r.sizeDelta = new Vector2(300,20);
        go.GetComponent<Image>().color = new Color(.2f,.2f,.2f,.8f);
        return go;
    }

    static Image MkFill(Transform parent, string name, Color col)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = col;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillAmount = 1;
        return img;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string c = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string n = c + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(n)) AssetDatabase.CreateFolder(c, parts[i]);
            c = n;
        }
    }

    static void EnsureLayer(string name)
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("layers");
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == name) return;
        for (int i = 6; i < layers.arraySize; i++)
        {
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            {
                layers.GetArrayElementAtIndex(i).stringValue = name;
                tm.ApplyModifiedProperties();
                return;
            }
        }
    }
}
#endif
