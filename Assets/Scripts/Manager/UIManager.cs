using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UIManager : Singleton<UIManager>
{
    [HideInInspector]
    public Level level;
    [HideInInspector]
    public Player player;

    public MiniMap miniMap;

    public Slider bossHp;

    public HP hp;
    public Text num;
    public Text Coin;
    public Image weapon;


    // public PausePanel pausePanel;

    private Text skillIndexText;
    private GameObject dynamicCanvas;

    void Start()
    {
        level = GameManager.Instance.level;
        player = GameManager.Instance.player;
        hp.player = player;
        miniMap.level = level;
        num.text = GameManager.Instance.depth.ToString();
        CreateDynamicUI();
    }

    public void initialize()
    {
        level = GameManager.Instance.level;
        player = GameManager.Instance.player;
        hp.player = player;
        miniMap.level = level;
        num.text = GameManager.Instance.depth.ToString();
        CreateDynamicUI();
    }

    private void Update()
    {
        if (skillIndexText != null && AdaptiveDifficultyManager.Instance != null)
        {
            skillIndexText.text = "Skill Index: " + AdaptiveDifficultyManager.Instance.SkillIndex.ToString("F2");
        }
    }

    private Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            Text[] allTexts = FindObjectsOfType<Text>(true);
            foreach (var t in allTexts)
            {
                if (t.font != null)
                {
                    font = t.font;
                    break;
                }
            }
        }
        return font;
    }

    private void CreateDynamicUI()
    {
        if (dynamicCanvas != null) return; // Already exists

        Canvas existingCanvas = GetComponentInParent<Canvas>();
        if (existingCanvas == null)
        {
            existingCanvas = FindObjectOfType<Canvas>();
        }

        if (existingCanvas == null) return;

        Font uiFont = GetDefaultFont();

        // UI Container Panel
        GameObject uiContainer = new GameObject("DDA_UI_Container");
        uiContainer.transform.SetParent(existingCanvas.transform, false);

        RectTransform rect = uiContainer.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-10f, -80f); // Position below standard minimap/UI
        rect.sizeDelta = new Vector2(160f, 80f);

        // 1. Skill Index Text
        GameObject textObj = new GameObject("SkillIndexText");
        textObj.transform.SetParent(uiContainer.transform, false);
        skillIndexText = textObj.AddComponent<Text>();
        skillIndexText.font = uiFont;
        skillIndexText.fontSize = 15;
        skillIndexText.color = Color.yellow;
        skillIndexText.alignment = TextAnchor.MiddleRight;
        
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0f, 0.6f);
        txtRect.anchorMax = new Vector2(1f, 1f);
        txtRect.sizeDelta = Vector2.zero;

        // Outline effect for better visibility
        var outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1f, -1f);

        // 2. Save Button
        GameObject saveBtnObj = new GameObject("SaveButton");
        saveBtnObj.transform.SetParent(uiContainer.transform, false);
        Image saveImg = saveBtnObj.AddComponent<Image>();
        saveImg.color = new Color(0.15f, 0.4f, 0.15f, 0.85f); // Soft green
        Button saveBtn = saveBtnObj.AddComponent<Button>();
        saveBtn.onClick.AddListener(() => {
            if (SaveLoadSystem.Instance != null) SaveLoadSystem.Instance.SaveGame();
        });

        RectTransform saveRect = saveBtnObj.GetComponent<RectTransform>();
        saveRect.anchorMin = new Vector2(0f, 0f);
        saveRect.anchorMax = new Vector2(0.48f, 0.45f);
        saveRect.sizeDelta = Vector2.zero;

        GameObject saveTxtObj = new GameObject("SaveText");
        saveTxtObj.transform.SetParent(saveBtnObj.transform, false);
        Text saveTxt = saveTxtObj.AddComponent<Text>();
        saveTxt.text = "Зберегти";
        saveTxt.font = uiFont;
        saveTxt.fontSize = 12;
        saveTxt.color = Color.white;
        saveTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform saveTxtRect = saveTxtObj.GetComponent<RectTransform>();
        saveTxtRect.anchorMin = Vector2.zero;
        saveTxtRect.anchorMax = Vector2.one;
        saveTxtRect.sizeDelta = Vector2.zero;

        // 3. Load Button
        GameObject loadBtnObj = new GameObject("LoadButton");
        loadBtnObj.transform.SetParent(uiContainer.transform, false);
        Image loadImg = loadBtnObj.AddComponent<Image>();
        loadImg.color = new Color(0.15f, 0.2f, 0.4f, 0.85f); // Soft blue
        Button loadBtn = loadBtnObj.AddComponent<Button>();
        loadBtn.onClick.AddListener(() => {
            if (SaveLoadSystem.Instance != null) SaveLoadSystem.Instance.LoadGame();
        });

        RectTransform loadRect = loadBtnObj.GetComponent<RectTransform>();
        loadRect.anchorMin = new Vector2(0.52f, 0f);
        loadRect.anchorMax = new Vector2(1f, 0.45f);
        loadRect.sizeDelta = Vector2.zero;

        GameObject loadTxtObj = new GameObject("LoadText");
        loadTxtObj.transform.SetParent(loadBtnObj.transform, false);
        Text loadTxt = loadTxtObj.AddComponent<Text>();
        loadTxt.text = "Завантаж.";
        loadTxt.font = uiFont;
        loadTxt.fontSize = 12;
        loadTxt.color = Color.white;
        loadTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform loadTxtRect = loadTxtObj.GetComponent<RectTransform>();
        loadTxtRect.anchorMin = Vector2.zero;
        loadTxtRect.anchorMax = Vector2.one;
        loadTxtRect.sizeDelta = Vector2.zero;

        dynamicCanvas = uiContainer;
    }

    public void PlayerUIInitialize()
    {
        hp.UpdateHP();
        UpdateStatus();
    }

    public void UpdateStatus()
    {
        Coin.text = player.coins.ToString();
        weapon.sprite = player.GetGunNow().GetComponent<SpriteRenderer>().sprite;
        weapon.transform.Find("WeaponName").GetComponent<Text>().text = player.GetGunNow().name.ToString();
    }
}

