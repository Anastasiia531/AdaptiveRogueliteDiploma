using System;
using System.IO;
using UnityEngine;

public class SaveLoadSystem : MonoBehaviour
{
    public static SaveLoadSystem Instance { get; private set; }

    private string savePath;

    [System.Serializable]
    public class SaveData
    {
        public int coins;
        public float health;
        public int depth;
        public float skillIndex;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "save.json");
            Debug.Log("Save file path: " + savePath);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame()
    {
        if (GameManager.Instance == null || GameManager.Instance.player == null)
        {
            Debug.LogWarning("Save failed: GameManager or Player is null.");
            return;
        }

        SaveData data = new SaveData
        {
            coins = GameManager.Instance.player.coins,
            health = GameManager.Instance.player.Health,
            depth = GameManager.Instance.depth,
            skillIndex = (AdaptiveDifficultyManager.Instance != null) ? AdaptiveDifficultyManager.Instance.SkillIndex : 0.5f
        };

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
            Debug.Log("Save game success! Coins: " + data.coins + ", Depth: " + data.depth);
        }
        catch (Exception e)
        {
            Debug.LogError("Error during save: " + e.Message);
        }
    }

    public bool LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("Load failed: Save file not found.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (GameManager.Instance != null && GameManager.Instance.player != null)
            {
                // Set health
                float currentHealth = GameManager.Instance.player.Health;
                float healthDiff = data.health - currentHealth;
                if (healthDiff > 0)
                {
                    GameManager.Instance.player.AddHealth((int)healthDiff);
                }
                else if (healthDiff < 0)
                {
                    GameManager.Instance.player.ReduceHealth(-healthDiff);
                }

                // Set coins
                GameManager.Instance.player.coins = data.coins;

                // Set SkillIndex
                if (AdaptiveDifficultyManager.Instance != null)
                {
                    AdaptiveDifficultyManager.Instance.SkillIndex = data.skillIndex;
                }

                // Set depth (prepare for LevelUp which increments depth by 1)
                GameManager.Instance.depth = Mathf.Max(0, data.depth - 1);
                GameManager.Instance.LevelUp();

                // Sync UI status
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.PlayerUIInitialize();
                }

                Debug.Log("Load game success! Depth restored: " + data.depth);
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error during load: " + e.Message);
        }
        return false;
    }
}
