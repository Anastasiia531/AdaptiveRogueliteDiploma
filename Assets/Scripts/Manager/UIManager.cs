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
        if (dynamicCanvas != null)
        {
            dynamicCanvas.SetActive(false);
        }

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
        if (dynamicCanvas != null)
        {
            dynamicCanvas.SetActive(false);
        }
        if (pauseMenuOverlay != null)
        {
            Destroy(pauseMenuOverlay);
            pauseMenuOverlay = null;
        }
        Time.timeScale = 1f;

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

        if (dynamicCanvas != null)
        {
            bool shouldBeActive = (pauseMenuOverlay != null && pauseMenuOverlay.activeSelf);
            if (dynamicCanvas.activeSelf != shouldBeActive)
            {
                dynamicCanvas.SetActive(shouldBeActive);
            }
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

            int shot = AdaptiveDifficultyManager.Instance.bulletsShot;
            int hit = AdaptiveDifficultyManager.Instance.bulletsHit;
            float accuracy = AdaptiveDifficultyManager.Instance.GetAccuracy() * 100f;
            
            float avgTime = AdaptiveDifficultyManager.Instance.GetAverageRoomClearTime();
            float totalTime = AdaptiveDifficultyManager.Instance.totalClearTime;
            int timedRooms = AdaptiveDifficultyManager.Instance.timedRoomsClearedCount;
            
            string speedStr = avgTime > 0f ? string.Format("{0:F1}с ({1:F0}с/{2} кімн.)", avgTime, totalTime, timedRooms) : "немає даних";
            float playerSpeed = player != null ? player.speed : 0f;

            skillIndexText.text = string.Format(
                "DDA Skill Index: <b>{0:F2}</b>\n" +
                "Складність: <color=#{1}><b>{2}</b></color>\n" +
                "Рахунок гри: <b>{3}</b>\n" +
                "Кімнат зачищено: <b>{4}</b>\n" +
                "Отримано шкоди: <b>{5} HP</b> (штраф: -{11:F2})\n" +
                "Влучність: <b>{6:F1}%</b> ({7}/{8} куль)\n" +
                "Час зачистки: <b>{9}</b>\n" +
                "Швидкість персонажа: <b>{10:F1}</b>\n" +
                "Випробування: <color=#00FF00><b>+{12}</b></color> / <color=#FF0000><b>-{13}</b></color>",
                skill,
                diffColorHex,
                difficultyName,
                score,
                rooms,
                dmg,
                accuracy,
                hit,
                shot,
                speedStr,
                playerSpeed,
                dmg * 0.04f,
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
        if (num != null && num.font != null) return num.font;
        if (Coin != null && Coin.font != null) return Coin.font;

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
        if (dynamicCanvas != null && skillIndexText != null) return; // Already exists

        if (dynamicCanvas != null)
        {
            Destroy(dynamicCanvas);
            dynamicCanvas = null;
        }

        Canvas existingCanvas = (num != null) ? num.canvas : null;
        if (existingCanvas == null)
        {
            existingCanvas = GetComponentInParent<Canvas>();
        }
        if (existingCanvas == null)
        {
            existingCanvas = FindObjectOfType<Canvas>();
        }

        if (existingCanvas == null)
        {
            Debug.LogError("[UIManager] Could not find any active Canvas to attach dynamic UI elements!");
            return;
        }

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
            roomTaskText.horizontalOverflow = HorizontalWrapMode.Overflow;
            roomTaskText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform taskTxtRect = taskTextObj.GetComponent<RectTransform>();
            taskTxtRect.anchorMin = Vector2.zero;
            taskTxtRect.anchorMax = Vector2.one;
            taskTxtRect.sizeDelta = Vector2.zero;

            var taskOutline = taskTextObj.AddComponent<Outline>();
            taskOutline.effectColor = Color.black;
            taskOutline.effectDistance = new Vector2(1.5f, -1.5f);
            
            roomTaskText.text = "КІМНАТА: Знайдіть вихід!";
        }

        // UI Container Panel - Styled stats window on the middle-left
        GameObject uiContainer = new GameObject("DDA_UI_Container");
        uiContainer.transform.SetParent(existingCanvas.transform, false);

        RectTransform rect = uiContainer.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(15f, -40f); 
        rect.sizeDelta = new Vector2(270f, 320f);

        // Add semi-transparent dark charcoal-blue glass background
        Image bgImg = uiContainer.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.06f, 0.09f, 0.85f);
        
        // Add a premium amber/gold border outline
        var panelOutline = uiContainer.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.72f, 0.55f, 0.22f, 0.85f);
        panelOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Header Text
        GameObject headerObj = new GameObject("DDA_Header");
        headerObj.transform.SetParent(uiContainer.transform, false);
        Text headerTxt = headerObj.AddComponent<Text>();
        headerTxt.font = uiFont;
        headerTxt.fontSize = 12;
        headerTxt.fontStyle = FontStyle.Bold;
        headerTxt.color = new Color(0.95f, 0.75f, 0.35f, 1f); // Bright gold
        headerTxt.text = "ПАРАМЕТРИ АДАПТИВНОСТІ";
        headerTxt.alignment = TextAnchor.MiddleCenter;
        headerTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        headerTxt.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.88f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.sizeDelta = Vector2.zero;

        // 1. Skill Index Text
        GameObject textObj = new GameObject("SkillIndexText");
        textObj.transform.SetParent(uiContainer.transform, false);
        skillIndexText = textObj.AddComponent<Text>();
        skillIndexText.font = uiFont;
        skillIndexText.fontSize = 11;
        skillIndexText.color = new Color(0.92f, 0.92f, 0.95f, 1f);
        skillIndexText.alignment = TextAnchor.UpperLeft;
        skillIndexText.supportRichText = true;
        skillIndexText.horizontalOverflow = HorizontalWrapMode.Overflow;
        skillIndexText.verticalOverflow = VerticalWrapMode.Overflow;
        
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0.06f, 0.22f);
        txtRect.anchorMax = new Vector2(0.94f, 0.86f);
        txtRect.sizeDelta = Vector2.zero;

        // Subtle text shadow
        var outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1f, -1f);

        // 2. Save Button
        GameObject saveBtnObj = new GameObject("SaveButton");
        saveBtnObj.transform.SetParent(uiContainer.transform, false);
        Image saveImg = saveBtnObj.AddComponent<Image>();
        saveImg.color = new Color(0.12f, 0.35f, 0.15f, 0.9f); // Forest green
        Button saveBtn = saveBtnObj.AddComponent<Button>();
        saveBtn.onClick.AddListener(() => {
            if (SaveLoadSystem.Instance != null) SaveLoadSystem.Instance.SaveGame();
        });

        RectTransform saveRect = saveBtnObj.GetComponent<RectTransform>();
        saveRect.anchorMin = new Vector2(0.06f, 0.05f);
        saveRect.anchorMax = new Vector2(0.48f, 0.18f);
        saveRect.sizeDelta = Vector2.zero;

        GameObject saveTxtObj = new GameObject("SaveText");
        saveTxtObj.transform.SetParent(saveBtnObj.transform, false);
        Text saveTxt = saveTxtObj.AddComponent<Text>();
        saveTxt.text = "Зберегти";
        saveTxt.font = uiFont;
        saveTxt.fontSize = 11;
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
        loadImg.color = new Color(0.12f, 0.2f, 0.35f, 0.9f); // Royal blue
        Button loadBtn = loadBtnObj.AddComponent<Button>();
        loadBtn.onClick.AddListener(() => {
            if (SaveLoadSystem.Instance != null) SaveLoadSystem.Instance.LoadGame();
        });

        RectTransform loadRect = loadBtnObj.GetComponent<RectTransform>();
        loadRect.anchorMin = new Vector2(0.52f, 0.05f);
        loadRect.anchorMax = new Vector2(0.94f, 0.18f);
        loadRect.sizeDelta = Vector2.zero;

        GameObject loadTxtObj = new GameObject("LoadText");
        loadTxtObj.transform.SetParent(loadBtnObj.transform, false);
        Text loadTxt = loadTxtObj.AddComponent<Text>();
        loadTxt.text = "Завантаж.";
        loadTxt.font = uiFont;
        loadTxt.fontSize = 11;
        loadTxt.color = Color.white;
        loadTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform loadTxtRect = loadTxtObj.GetComponent<RectTransform>();
        loadTxtRect.anchorMin = Vector2.zero;
        loadTxtRect.anchorMax = Vector2.one;
        loadTxtRect.sizeDelta = Vector2.zero;

        dynamicCanvas = uiContainer;
        dynamicCanvas.SetActive(false); // Hide by default, only show when paused
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

        if (dynamicCanvas != null)
        {
            dynamicCanvas.SetActive(false);
        }
        if (pauseMenuOverlay != null)
        {
            Destroy(pauseMenuOverlay);
            pauseMenuOverlay = null;
        }
        Time.timeScale = 1f;
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

        if (dynamicCanvas == null || skillIndexText == null)
        {
            CreateDynamicUI();
        }

        if (dynamicCanvas != null)
        {
            dynamicCanvas.SetActive(isActive);
        }

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

    public void TryShowPickupPopup(string className, Sprite sprite)
    {
        Player activePlayer = GameManager.Instance != null ? GameManager.Instance.player : null;
        if (activePlayer == null) activePlayer = player;
        if (activePlayer == null) return;
        
        string cleanName = className.Replace("(Clone)", "").Trim();

        if (activePlayer.discoveredItems.Contains(cleanName)) return;

        activePlayer.discoveredItems.Add(cleanName);

        string itemName = cleanName;
        string itemDesc = "Таємничий артефакт.";

        switch (cleanName)
        {
            case "CPU":
                itemName = "Процесор (CPU)";
                itemDesc = "Дозволяє вільно перемикати зброю цифрами 1-5.";
                break;
            case "DataStructure":
                itemName = "Структури даних";
                itemDesc = "Збільшує максимальний боєзапас вашої зброї.";
                break;
            case "Internet":
                itemName = "Інтернет";
                itemDesc = "Значно збільшує швидкість пересування персонажа.";
                break;
            case "OS":
                itemName = "Операційна система (OS)";
                itemDesc = "Подвоює темп стрільби (зменшує інтервал атаки).";
                break;
            case "TheBrokenHeart":
                itemName = "Розбите серце";
                itemDesc = "Збільшує максимальне здоров'я та повністю лікує.";
                break;
            case "Python":
                itemName = "Мова Python";
                itemDesc = "Стріляє потужними, але повільними змієподібними кулями.";
                break;
            case "Cpp":
                itemName = "Мова C++";
                itemDesc = "Швидкострільний і точний автомат для постійної атаки.";
                break;
            case "Java":
                itemName = "Мова Java";
                itemDesc = "Дробовик середнього радіусу, що стріляє віялом із 3 куль.";
                break;
            case "SQL":
                itemName = "База даних SQL";
                itemDesc = "Потужний дробовик з широким розсіюванням на близькій відстані.";
                break;
            case "Machine":
                itemName = "Машинний код";
                itemDesc = "Снайперська зброя: величезна шкода, але довга перезарядка.";
                break;
            case "BookOfBelial":
                itemName = "Книга Беліала";
                itemDesc = "Тимчасово подвоює темп стрільби на 20 секунд.";
                break;
            case "Gamekid":
                itemName = "Геймкід";
                itemDesc = "Дарує абсолютну невразливість на 12 секунд.";
                break;
            case "Compass":
                itemName = "Компас";
                itemDesc = "Повністю відкриває карту поточного поверху.";
                break;
            default:
                break;
        }

        ShowItemPickupPopup(itemName, itemDesc, sprite);
    }

    public void ShowItemPickupPopup(string itemName, string description, Sprite itemSprite)
    {
        GameObject oldPopup = GameObject.Find("ItemPickupPopupPanel");
        if (oldPopup != null)
        {
            Destroy(oldPopup);
        }

        Canvas canvas = num != null ? num.canvas : null;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Font uiFont = num != null ? num.font : GetDefaultFont();

        GameObject container = new GameObject("ItemPickupPopupPanel");
        container.transform.SetParent(canvas.transform, false);
        container.transform.SetAsLastSibling();

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0f, 100f);
        containerRect.sizeDelta = new Vector2(500f, 150f);
        container.transform.localScale = Vector3.one;
        containerRect.localPosition = new Vector3(containerRect.localPosition.x, containerRect.localPosition.y, 0f);

        Image bgImg = container.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        Outline bgOutline = container.AddComponent<Outline>();
        bgOutline.effectColor = new Color(1f, 0.84f, 0f, 0.8f);
        bgOutline.effectDistance = new Vector2(2f, -2f);

        GameObject alertObj = new GameObject("AlertText");
        alertObj.transform.SetParent(container.transform, false);
        Text alertText = alertObj.AddComponent<Text>();
        alertText.font = uiFont;
        alertText.fontSize = 14;
        alertText.fontStyle = FontStyle.Bold;
        alertText.color = new Color(1f, 0.5f, 0f);
        alertText.alignment = TextAnchor.MiddleCenter;
        alertText.text = "ЗНАЙДЕНО НОВИЙ ПРЕДМЕТ!";
        alertText.horizontalOverflow = HorizontalWrapMode.Overflow;
        alertText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform alertRect = alertObj.GetComponent<RectTransform>();
        alertRect.anchorMin = new Vector2(0f, 0.8f);
        alertRect.anchorMax = new Vector2(1f, 0.95f);
        alertRect.anchoredPosition = Vector2.zero;
        alertRect.sizeDelta = Vector2.zero;

        if (itemSprite != null)
        {
            GameObject spriteObj = new GameObject("ItemSprite");
            spriteObj.transform.SetParent(container.transform, false);
            Image itemImg = spriteObj.AddComponent<Image>();
            itemImg.sprite = itemSprite;
            itemImg.preserveAspect = true;

            RectTransform spriteRect = spriteObj.GetComponent<RectTransform>();
            spriteRect.anchorMin = new Vector2(0.05f, 0.15f);
            spriteRect.anchorMax = new Vector2(0.25f, 0.75f);
            spriteRect.anchoredPosition = Vector2.zero;
            spriteRect.sizeDelta = Vector2.zero;
        }

        GameObject nameObj = new GameObject("ItemNameText");
        nameObj.transform.SetParent(container.transform, false);
        Text nameText = nameObj.AddComponent<Text>();
        nameText.font = uiFont;
        nameText.fontSize = 22;
        nameText.fontStyle = FontStyle.Bold;
        nameText.color = Color.white;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.text = itemName;
        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.28f, 0.45f);
        nameRect.anchorMax = new Vector2(0.95f, 0.75f);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = Vector2.zero;

        Outline nameOutline = nameObj.AddComponent<Outline>();
        nameOutline.effectColor = Color.black;
        nameOutline.effectDistance = new Vector2(1f, -1f);

        GameObject descObj = new GameObject("ItemDescText");
        descObj.transform.SetParent(container.transform, false);
        Text descText = descObj.AddComponent<Text>();
        descText.font = uiFont;
        descText.fontSize = 15;
        descText.fontStyle = FontStyle.Italic;
        descText.color = new Color(0.8f, 0.9f, 1f);
        descText.alignment = TextAnchor.MiddleLeft;
        descText.text = description;
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.28f, 0.1f);
        descRect.anchorMax = new Vector2(0.95f, 0.42f);
        descRect.anchoredPosition = Vector2.zero;
        descRect.sizeDelta = Vector2.zero;

        Shadow descShadow = descObj.AddComponent<Shadow>();
        descShadow.effectColor = Color.black;

        Image spriteComponent = null;
        Transform spriteTrans = container.transform.Find("ItemSprite");
        if (spriteTrans != null) spriteComponent = spriteTrans.GetComponent<Image>();

        StartCoroutine(AnimatePopupFadeAndDestroy(container, bgImg, alertText, spriteComponent, nameText, descText));
    }

    private IEnumerator AnimatePopupFadeAndDestroy(GameObject container, Image bg, Text t1, Image img, Text t2, Text t3)
    {
        yield return new WaitForSeconds(3.5f);

        float elapsed = 0f;
        float duration = 0.8f;
        while (elapsed < duration)
        {
            if (container == null) break;
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            if (bg != null) bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, alpha * 0.95f);
            if (t1 != null) t1.color = new Color(t1.color.r, t1.color.g, t1.color.b, alpha);
            if (img != null) img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            if (t2 != null) t2.color = new Color(t2.color.r, t2.color.g, t2.color.b, alpha);
            if (t3 != null) t3.color = new Color(t3.color.r, t3.color.g, t3.color.b, alpha);
            yield return null;
        }

        if (container != null)
        {
            Destroy(container);
        }
    }
}

