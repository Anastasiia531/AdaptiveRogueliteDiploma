using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    private GameObject storyCanvasObj;
    private Text storyTextComp;
    private Action onStoryCompleted;

    // Narrative scripts for level completions (levels 1-5)
    private string[] floorNarratives = new string[]
    {
        "", // Depth 0 (unused)
        "РІВЕНЬ 1 ЗАВЕРШЕНО\n\nВи подолали перший ярус процедурного підземелля. Світ навколо починає адаптуватися до вас. Ви відчуваєте, як невидима система DDA записує кожен ваш крок, вимірюючи отриману шкоду та час подолання випробувань...",
        "РІВЕНЬ 2 ЗАВЕРШЕНО\n\nТемрява згущується, а виклики стають складнішими. Алгоритми підлаштовують щільність ворогів та параметри міні-ігор. Кожен успіх робить наступну кімнату небезпечнішою. Будьте обережні!",
        "РІВЕНЬ 3 ЗАВЕРШЕНО\n\nТретій ярус позаду. Ви відчуваєте, що вороги стають міцнішими та швидшими, коли ваш Skill Index зростає. Спокійні кімнати SafeRoom тепер є справжнім порятунком для відновлення сил...",
        "РІВЕНЬ 4 ЗАВЕРШЕНО\n\nВи пройшли через четверте коло випробувань. Попереду фінальний бій. Завдяки вашій спритності та виваженим жертвам на вівтарях SacrificeAltar, ви отримали потужне спорядження. Зберіть волю в кулак!",
        "СЮЖЕТНУ КАМПАНІЮ ЗАВЕРШЕНО!\n\nВітаємо, герою! Ви успішно здолали всі 5 сюжетних рівнів та пройшли випробування адаптивного дипломного проєкту.\n\nТепер гра переходить у НЕСКІНЧЕННИЙ РЕЖИМ (Infinite Mode) із прогресуючим наростанням складності. Подивимось, як далеко ви зможете зайти!"
    };

    // Tutorial text for level starts (depth 0-4)
    private string[] levelTutorials = new string[]
    {
        "РІВЕНЬ 1: ВСТУП ТА КЕРУВАННЯ\n\nЛаскаво просимо до адаптивного підземелля!\nВаша мета — досліджувати кімнати, знищувати ворогів та знайти кімнату Боса, щоб спуститися глибше.\n\nКерування:\n• WASD або Стрілочки — рух персонажа.\n• Стрілочки (Вгору, Вниз, Вліво, Вправо) — стрільба в цьому напрямку.\n• Q / E — зміна поточної зброї.\n\nЗнайдіть кімнату з босом та скористайтеся люком, щоб перейти далі!",
        
        "РІВЕНЬ 2: СИСТЕМА АДАПТИВНОЇ СКЛАДНОСТІ (DDA)\n\nЦя гра використовує систему DDA. Вона відстежує ваші показники (здоров'я, час проходження кімнат, точність).\n\n• Якщо ви граєте вправно, гра автоматично збільшує здоров'я та швидкість ворогів.\n• Якщо ви часто отримуєте шкоду, гра знижує рівень складності (Skill Index), допомагаючи вам вижити.",
        
        "РІВЕНЬ 3: КІМНАТИ-ВИПРОБУВАННЯ (CHALLENGE ROOMS)\n\nПочинаючи з цього рівня, у деяких кімнатах вас чекають випадкові виклики та міні-ігри.\n\n• Коли ви заходите в таку кімнату, двері блокуються.\n• Пройдіть випробування (ухиляйтеся від пасток, вирішуйте головоломки, виживайте), щоб відімкнути вихід та отримати скриню з нагородою!",
        
        "РІВЕНЬ 4: СПЕЦІАЛЬНІ КІМНАТИ\n\nШукайте на карті особливі кімнати, які полегшують виживання:\n• Скарбниця (Treasure Room) — містить цінний артефакт або покращення.\n• Магазин (Shop Room) — дозволяє купити корисні предмети за монети.\n• Спокійна кімната (Safe Room) — безпечна зона, де ви можете підлікуватися.",
        
        "РІВЕНЬ 5: ФІНАЛЬНЕ ВИПРОБУВАННЯ\n\nВи дісталися останнього навчального рівня!\nПопереду на вас чекає найскладніший Бос. Використайте всі свої навички та накопичену зброю, щоб здолати його.\n\nПеремога завершить сюжетну кампанію та відкриє Нескінченний режим!"
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowStory(int completedDepth, Action onComplete)
    {
        // Only show narratives for completed levels 1 to 5
        if (completedDepth < 1 || completedDepth >= floorNarratives.Length)
        {
            onComplete?.Invoke();
            return;
        }

        onStoryCompleted = onComplete;
        Time.timeScale = 0f; // Pause the game loop

        CreateStoryCanvas(floorNarratives[completedDepth]);
    }

    public void ShowLevelTutorial(int currentDepth, Action onComplete)
    {
        if (currentDepth < 0 || currentDepth >= levelTutorials.Length)
        {
            onComplete?.Invoke();
            return;
        }

        onStoryCompleted = onComplete;
        Time.timeScale = 0f; // Pause the game loop

        CreateStoryCanvas(levelTutorials[currentDepth]);
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

    private void CreateStoryCanvas(string textContent)
    {
        if (storyCanvasObj != null) Destroy(storyCanvasObj);

        // 1. Create main canvas
        storyCanvasObj = new GameObject("StoryCanvas");
        Canvas canvas = storyCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        storyCanvasObj.AddComponent<CanvasScaler>();
        storyCanvasObj.AddComponent<GraphicRaycaster>();

        // 2. Background panel (glassmorphism overlay style)
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(storyCanvasObj.transform, false);
        Image bgImage = panelObj.AddComponent<Image>();
        bgImage.color = new Color(0.06f, 0.06f, 0.1f, 0.96f); // Deep midnight blue overlay

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Font storyFont = GetDefaultFont();

        // 3. Text display
        GameObject textObj = new GameObject("StoryText");
        textObj.transform.SetParent(panelObj.transform, false);
        storyTextComp = textObj.AddComponent<Text>();
        storyTextComp.text = textContent;
        storyTextComp.font = storyFont;
        storyTextComp.fontSize = 22;
        storyTextComp.color = Color.white;
        storyTextComp.alignment = TextAnchor.MiddleCenter;
        storyTextComp.lineSpacing = 1.3f;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.15f, 0.3f);
        textRect.anchorMax = new Vector2(0.85f, 0.85f);
        textRect.sizeDelta = Vector2.zero;

        // 4. Continue Button
        GameObject btnObj = new GameObject("ContinueButton");
        btnObj.transform.SetParent(panelObj.transform, false);
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.15f, 0.45f, 0.25f, 1f); // Sleek emerald green

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(OnContinuePressed);

        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.4f, 0.12f);
        btnRect.anchorMax = new Vector2(0.6f, 0.22f);
        btnRect.sizeDelta = Vector2.zero;

        // 5. Button Text
        GameObject btnTextObj = new GameObject("BtnText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        Text btnText = btnTextObj.AddComponent<Text>();
        btnText.text = "Продовжити";
        btnText.font = storyFont;
        btnText.fontSize = 18;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;

        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
    }

    private void OnContinuePressed()
    {
        if (storyCanvasObj != null)
        {
            Destroy(storyCanvasObj);
        }

        Time.timeScale = 1f; // Resume physics and updates
        onStoryCompleted?.Invoke();
    }
}
