using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChallengeRoom : Room
{
    protected float challengeStartTime;
    protected bool challengeActive = false;
    protected float skillIndexAtStart = 0.5f;

    public override void Initialize()
    {
        base.Initialize();
        CreateWorldSpaceLabel();
    }

    private void CreateWorldSpaceLabel()
    {
        GameObject labelObj = new GameObject("ChallengeRoomLabel");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 3.5f, 0f); // Center-ish top of the room in local coordinates

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = GetChallengeUAName();
        textMesh.characterSize = 0.15f;
        textMesh.fontSize = 40;
        textMesh.color = new Color(0.9f, 0.4f, 0.9f, 0.9f); // Soft purple
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        // Render shadow for the world-space text to make it extremely clear against obstacles
        GameObject shadowObj = new GameObject("ChallengeRoomLabelShadow");
        shadowObj.transform.SetParent(labelObj.transform, false);
        shadowObj.transform.localPosition = new Vector3(0.05f, -0.05f, 0.01f);
        TextMesh shadowMesh = shadowObj.AddComponent<TextMesh>();
        shadowMesh.text = textMesh.text;
        shadowMesh.characterSize = textMesh.characterSize;
        shadowMesh.fontSize = textMesh.fontSize;
        shadowMesh.color = new Color(0f, 0f, 0f, 0.7f);
        shadowMesh.anchor = textMesh.anchor;
        shadowMesh.alignment = textMesh.alignment;
    }

    protected string GetChallengeUAName()
    {
        string challName = GetType().Name;
        switch (challName)
        {
            case "ThreeCardsMonte": return "Три карти (Наперстки)";
            case "SequenceMemoryChallenge": return "Плитки пам'яті";
            case "RouletteWheel": return "Рулетка удачі";
            case "TimeMazeChallenge": return "Лабіринт часу";
            case "ObserverRoom": return "Око спостерігача";
            case "ChangingSafeZones": return "Безпечні зони";
            default: return "Кімната випробувань";
        }
    }

    public override void GenerateRoomContent()
    {
        isCleared = false;
        LockDoors();
        StartCoroutine(StartChallengeWithDelay());
    }

    private IEnumerator StartChallengeWithDelay()
    {
        yield return new WaitForSeconds(0.6f);
        skillIndexAtStart = (AdaptiveDifficultyManager.Instance != null) ? AdaptiveDifficultyManager.Instance.SkillIndex : 0.5f;
        challengeStartTime = Time.time;
        challengeActive = true;
        
        // Play procedural challenge start sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(600f, 0.2f, 0.15f);
            AudioManager.Instance.PlaySound(800f, 0.2f, 0.15f);
        }

        StartChallenge();
    }

    protected virtual void StartChallenge()
    {
        // Subclasses will implement their specific mini-game gameplay here
    }

    public void LockDoors()
    {
        foreach (var door in activeDoorList)
        {
            if (door != null)
            {
                Transform col = door.transform.Find("collider");
                if (col != null)
                {
                    BoxCollider2D boxCol = col.GetComponent<BoxCollider2D>();
                    if (boxCol != null) boxCol.isTrigger = false;
                }
                
                Transform dTrans = door.transform.Find("Door");
                if (dTrans != null)
                {
                    Animator anim = dTrans.GetComponent<Animator>();
                    if (anim != null && anim.runtimeAnimatorController != null)
                    {
                        try
                        {
                            anim.Play("DoorClose");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning("Could not play DoorClose animation: " + ex.Message);
                        }
                    }
                }
            }
        }
    }

    protected virtual void CompleteChallenge(bool success)
    {
        if (!challengeActive) return;
        challengeActive = false;

        float duration = Time.time - challengeStartTime;
        isCleared = true;
        OpenActivatedDoor();

        // 1. Report to DDA (Dynamic Difficulty Adjustment)
        if (AdaptiveDifficultyManager.Instance != null)
        {
            AdaptiveDifficultyManager.Instance.LogChallengeResult(success);
        }

        // Give weaker players a temporary super weapon boost on challenge completion
        if (AdaptiveDifficultyManager.Instance != null && AdaptiveDifficultyManager.Instance.SkillIndex < 0.45f)
        {
            if (GameManager.Instance != null && GameManager.Instance.player != null)
            {
                GameManager.Instance.player.EnableSuperWeapon(35f);
            }
        }

        // 2. Log to SQLite database local analytics
        if (DatabaseManager.Instance != null)
        {
            string challengeName = GetType().Name;
            DatabaseManager.Instance.LogChallenge(challengeName, success ? 1 : 0, duration, skillIndexAtStart);
        }

        // 3. Play procedural success/failure sounds
        if (AudioManager.Instance != null)
        {
            if (success)
            {
                AudioManager.Instance.PlaySound(523.25f, 0.1f, 0.2f); // C5
                AudioManager.Instance.PlaySound(659.25f, 0.1f, 0.2f); // E5
                AudioManager.Instance.PlaySound(783.99f, 0.3f, 0.2f); // G5
            }
            else
            {
                AudioManager.Instance.PlaySound(220f, 0.4f, 0.3f, true); // Low explosion/noise
            }
        }

        // 4. Spawn a normal chest/pickup reward on success
        if (success && level != null && level.pools != null)
        {
            GameObject reward = level.pools.GetRoomClearingReward(RoomType.Normal);
            if (reward != null)
            {
                GenerateGameObjectWithCoordinate(reward, new Vector2(0f, 0f), itemContainer);
            }
        }
    }

    public static Sprite CreateSquareSprite(Color mainColor, Color borderColor, int size = 64)
    {
        Texture2D texture = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = (x < 4 || x >= size - 4 || y < 4 || y >= size - 4);
                texture.SetPixel(x, y, isBorder ? borderColor : mainColor);
            }
        }
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    public static Sprite CreateCircleSprite(Color mainColor, Color borderColor, int size = 64)
    {
        Texture2D texture = new Texture2D(size, size);
        float radius = size / 2f;
        float innerRadius = radius - 4f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > radius)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else if (dist > innerRadius)
                {
                    texture.SetPixel(x, y, borderColor);
                }
                else
                {
                    texture.SetPixel(x, y, mainColor);
                }
            }
        }
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    public static Sprite CreateRouletteWheelSprite(int size = 128)
    {
        Texture2D texture = new Texture2D(size, size);
        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > radius)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else if (dist < 8f)
                {
                    texture.SetPixel(x, y, Color.yellow);
                }
                else if (dist > radius - 6f)
                {
                    texture.SetPixel(x, y, new Color(0.3f, 0.15f, 0f));
                }
                else
                {
                    float angle = Mathf.Atan2(y - center.y, x - center.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    int segment = Mathf.FloorToInt(angle / 45f);
                    if (segment % 3 == 0)
                    {
                        texture.SetPixel(x, y, Color.red);
                    }
                    else if (segment % 3 == 1)
                    {
                        texture.SetPixel(x, y, Color.black);
                    }
                    else
                    {
                        texture.SetPixel(x, y, new Color(0f, 0.6f, 0f));
                    }
                }
            }
        }
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    public static Sprite CreateCardSprite(Color backingColor, int width = 48, int height = 64)
    {
        Texture2D texture = new Texture2D(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isBorder = (x < 3 || x >= width - 3 || y < 3 || y >= height - 3);
                if (isBorder)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    bool isCenterPattern = (x >= width/2 - 2 && x <= width/2 + 2 && y >= height/2 - 6 && y <= height/2 + 6);
                    texture.SetPixel(x, y, isCenterPattern ? Color.yellow : backingColor);
                }
            }
        }
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
    }

    public static Sprite CreateEyeSprite(bool open, int width = 64, int height = 32)
    {
        Texture2D texture = new Texture2D(width, height);
        float radiusX = width / 2f;
        float radiusY = height / 2f;
        Vector2 center = new Vector2(radiusX, radiusY);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float ell = Mathf.Pow(x - center.x, 2) / Mathf.Pow(radiusX, 2) + Mathf.Pow(y - center.y, 2) / Mathf.Pow(radiusY, 2);
                if (ell > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float pupilDist = Vector2.Distance(new Vector2(x, y * 2), new Vector2(center.x, center.y * 2));
                    if (pupilDist < 8f)
                    {
                        texture.SetPixel(x, y, open ? Color.red : Color.black);
                    }
                    else if (pupilDist < 16f)
                    {
                        texture.SetPixel(x, y, open ? Color.yellow : new Color(0f, 0.4f, 0f));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }
        }
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
    }
}
