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
    private Text roomTaskText;
    private GameObject dynamicCanvas;
    private GameObject taskCanvasObj;
    private Image humanBtnImg;
    private Image proBtnImg;
    private Image noobBtnImg;
    private GameObject mainMenuOverlay;

    void Start()
    {
        level = GameManager.Instance.level;
        player = GameManager.Instance.player;
        hp.player = player;
        miniMap.level = level;
        num.text = (GameManager.Instance.depth + 1).ToString();
        CreateDynamicUI();

        // Play procedural start sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(600f, 0.15f, 0.2f);
            AudioManager.Instance.PlaySound(900f, 0.3f, 0.2f);
        }
    }

    public void initialize()
    {
        level = GameManager.Instance.level;
        player = GameManager.Instance.player;
        hp.player = player;
        miniMap.level = level;
        num.text = (GameManager.Instance.depth + 1).ToString();
        CreateDynamicUI();
    }

    private void Update()
    {
        if (player == null)
        {
            player = GameManager.Instance != null ? GameManager.Instance.player : null;
            if (player == null) player = FindObjectOfType<Player>();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }

        if (skillIndexText != null && AdaptiveDifficultyManager.Instance != null)
        {
            float skill = AdaptiveDifficultyManager.Instance.SkillIndex;
            string difficultyName = "НОРМАЛЬНО";
            string diffColorHex = "FFFF00"; // Yellow
            if (skill < 0.35f)
            {
                difficultyName = "ЛЕГКО";
                diffColorHex = "00FF00"; // Green
            }
            else if (skill > 0.7f)
            {
                difficultyName = "СКЛАДНО";
                diffColorHex = "FF0000"; // Red
            }

            int score = AdaptiveDifficultyManager.Instance.GetGameScore();
            int rooms = AdaptiveDifficultyManager.Instance.roomsCleared;
            int dmg = AdaptiveDifficultyManager.Instance.totalDamageTaken;
            int chalWin = AdaptiveDifficultyManager.Instance.challengesCleared;
            int chalLose = AdaptiveDifficultyManager.Instance.challengesFailed;
            float accuracy = AdaptiveDifficultyManager.Instance.GetAccuracy() * 100f;
            float avgTime = AdaptiveDifficultyManager.Instance.GetAverageRoomClearTime();
            string speedStr = avgTime > 0f ? string.Format("{0:F1}с", avgTime) : "немає даних";

            skillIndexText.text = string.Format(
                "DDA Skill Index: {0:F2}\n" +
                "Складність: <color=#{1}>{2}</color>\n" +
                "Рахунок гри: {3}\n" +
                "Пройдено кімнат: {4}\n" +
                "Отримано шкоди: {5} HP\n" +
                "Міткість (точність): {6:F1}%\n" +
                "Швидкість (очищення): {7}\n" +
                "Випробування: {8} / {9}",
                skill,
                diffColorHex,
                difficultyName,
                score,
                rooms,
                dmg,
                accuracy,
                speedStr,
                chalWin,
                chalLose
            );
        }
    }

    public void SetRoomTaskText(string task)
    {
        if (roomTaskText != null)
        {
            roomTaskText.text = task;
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

        if (taskCanvasObj == null)
        {
            taskCanvasObj = new GameObject("RoomTaskContainer");
            taskCanvasObj.transform.SetParent(existingCanvas.transform, false);

            RectTransform taskRect = taskCanvasObj.AddComponent<RectTransform>();
            taskRect.anchorMin = new Vector2(0f, 1f);
            taskRect.anchorMax = new Vector2(0f, 1f);
            taskRect.pivot = new Vector2(0f, 1f);
            taskRect.anchoredPosition = new Vector2(20f, -25f); 
            taskRect.sizeDelta = new Vector2(600f, 40f);

            GameObject taskTextObj = new GameObject("RoomTaskText");
            taskTextObj.transform.SetParent(taskCanvasObj.transform, false);
            roomTaskText = taskTextObj.AddComponent<Text>();
            roomTaskText.font = uiFont;
            roomTaskText.fontSize = 18;
            roomTaskText.color = new Color(0.9f, 0.9f, 1f, 1f); 
            roomTaskText.alignment = TextAnchor.MiddleLeft;

            RectTransform taskTxtRect = taskTextObj.GetComponent<RectTransform>();
            taskTxtRect.anchorMin = Vector2.zero;
            taskTxtRect.anchorMax = Vector2.one;
            taskTxtRect.sizeDelta = Vector2.zero;

            var taskOutline = taskTextObj.AddComponent<Outline>();
            taskOutline.effectColor = Color.black;
            taskOutline.effectDistance = new Vector2(1.5f, -1.5f);
            
            roomTaskText.text = "КІМНАТА: Знайдіть вихід!";
        }

        // UI Container Panel
        GameObject uiContainer = new GameObject("DDA_UI_Container");
        uiContainer.transform.SetParent(existingCanvas.transform, false);

        RectTransform rect = uiContainer.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-10f, -80f); // Position below standard minimap/UI
        rect.sizeDelta = new Vector2(180f, 320f); // Taller container for tracking stats

        // 1. Skill Index Text
        GameObject textObj = new GameObject("SkillIndexText");
        textObj.transform.SetParent(uiContainer.transform, false);
        skillIndexText = textObj.AddComponent<Text>();
        skillIndexText.font = uiFont;
        skillIndexText.fontSize = 12;
        skillIndexText.color = Color.white;
        skillIndexText.alignment = TextAnchor.UpperRight;
        skillIndexText.supportRichText = true;
        
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0f, 0.62f);
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
        saveRect.anchorMin = new Vector2(0f, 0.45f);
        saveRect.anchorMax = new Vector2(0.48f, 0.58f);
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
        loadRect.anchorMin = new Vector2(0.52f, 0.45f);
        loadRect.anchorMax = new Vector2(1f, 0.58f);
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



    private GameObject CreateMenuButton(Transform parent, string name, string text, Vector2 anchorPos, Font font, Color bgColor, System.Action onClickAction)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => onClickAction());

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(anchorPos.x, anchorPos.y);
        rect.anchorMax = new Vector2(anchorPos.x, anchorPos.y);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(280f, 50f);

        var outline = btnObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.text = text;
        txt.font = font;
        txt.fontSize = 16;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        return btnObj;
    }

    public bool IsMainMenuActive()
    {
        return false;
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

    private GameObject pauseMenuOverlay;

    public void TogglePauseMenu()
    {
        if (IsMainMenuActive()) return; // Don't show pause menu over main menu

        if (pauseMenuOverlay == null)
        {
            CreatePauseMenu();
        }

        bool isActive = !pauseMenuOverlay.activeSelf;
        pauseMenuOverlay.SetActive(isActive);

        if (isActive)
        {
            if (player != null) player.PlayerPause();
            Time.timeScale = 0f; // Pause game loop
        }
        else
        {
            if (player != null) player.PlayerResume();
            Time.timeScale = 1f; // Resume game loop
        }
    }

    private void CreatePauseMenu()
    {
        if (pauseMenuOverlay != null) return;

        Canvas existingCanvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
        if (existingCanvas == null) return;

        Font uiFont = GetDefaultFont();

        // Overlay background
        pauseMenuOverlay = new GameObject("PauseMenuOverlay");
        pauseMenuOverlay.transform.SetParent(existingCanvas.transform, false);

        RectTransform overlayRect = pauseMenuOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;

        Image bgImage = pauseMenuOverlay.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.1f, 0.85f); // Sleek dark overlay

        // Pause Box Panel
        GameObject boxObj = new GameObject("PauseBox");
        boxObj.transform.SetParent(pauseMenuOverlay.transform, false);

        RectTransform boxRect = boxObj.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(300f, 320f);

        Image boxImg = boxObj.AddComponent<Image>();
        boxImg.color = new Color(0.1f, 0.1f, 0.18f, 0.95f);
        
        var outline = boxObj.AddComponent<Outline>();
        outline.effectColor = Color.cyan;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Title
        GameObject titleObj = new GameObject("PauseTitle");
        titleObj.transform.SetParent(boxObj.transform, false);
        Text titleTxt = titleObj.AddComponent<Text>();
        titleTxt.text = "ПАУЗА";
        titleTxt.font = uiFont;
        titleTxt.fontSize = 24;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color = Color.white;
        titleTxt.alignment = TextAnchor.MiddleCenter;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.8f);
        titleRect.anchorMax = new Vector2(1f, 0.95f);
        titleRect.sizeDelta = Vector2.zero;

        // Button 1: Resume
        GameObject resumeBtn = CreateMenuButton(boxObj.transform, "ResumeBtn", "Продовжити", new Vector2(0f, 60f), uiFont, new Color(0.15f, 0.4f, 0.15f, 0.9f), () => {
            TogglePauseMenu();
        });
        RectTransform resRect = resumeBtn.GetComponent<RectTransform>();
        resRect.anchorMin = new Vector2(0.5f, 0.5f);
        resRect.anchorMax = new Vector2(0.5f, 0.5f);
        resRect.anchoredPosition = new Vector2(0f, 60f);
        resRect.sizeDelta = new Vector2(240f, 40f);

        // Button 2: Tutorial
        GameObject tutBtn = CreateMenuButton(boxObj.transform, "TutorialBtn", "Навчання (5 кімнат)", new Vector2(0f, 0f), uiFont, new Color(0.15f, 0.25f, 0.5f, 0.9f), () => {
            GameManager.Instance.isTutorialMode = true;
            Time.timeScale = 1f;
            pauseMenuOverlay.SetActive(false);

            // Reload the level using the SetActive(false)/SetActive(true) pattern
            GameManager.Instance.depth = 0;
            GameManager.Instance.level.gameObject.SetActive(false);
            if (player != null) player.transform.position = new Vector3(0, 0, 0);
            GameManager.Instance.myCamera.transform.position = new Vector3(0, 0, -10);
            GameManager.Instance.level.gameObject.SetActive(true);

            if (AdaptiveDifficultyManager.Instance != null)
            {
                AdaptiveDifficultyManager.Instance.ResetPlaythroughStats();
            }
            if (player != null) player.PlayerResume();
        });
        RectTransform tutRect = tutBtn.GetComponent<RectTransform>();
        tutRect.anchorMin = new Vector2(0.5f, 0.5f);
        tutRect.anchorMax = new Vector2(0.5f, 0.5f);
        tutRect.anchoredPosition = new Vector2(0f, 5f);
        tutRect.sizeDelta = new Vector2(240f, 40f);

        // Button 3: Main Menu
        GameObject mainBtn = CreateMenuButton(boxObj.transform, "MainMenuBtn", "Головне меню", new Vector2(0f, -60f), uiFont, new Color(0.4f, 0.15f, 0.15f, 0.9f), () => {
            Time.timeScale = 1f;
            pauseMenuOverlay.SetActive(false);
            if (mainMenuOverlay != null)
            {
                mainMenuOverlay.SetActive(true);
            }
            if (player != null) player.PlayerPause();
        });
        RectTransform mainRect = mainBtn.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainRect.anchoredPosition = new Vector2(0f, -50f);
        mainRect.sizeDelta = new Vector2(240f, 40f);

        // Initially hide
        pauseMenuOverlay.SetActive(false);
    }
}

