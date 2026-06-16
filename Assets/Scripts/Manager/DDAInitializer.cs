using UnityEngine;

public static class DDAInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeDDAEngine()
    {
        Debug.Log("DDA System: Auto-initializing adaptive roguelite engine...");

        GameObject engine = new GameObject("AdaptiveDDA_Engine");
        Object.DontDestroyOnLoad(engine);

        // Add all managers dynamically
        engine.AddComponent<AdaptiveDifficultyManager>();
        engine.AddComponent<DatabaseManager>();
        engine.AddComponent<SaveLoadSystem>();
        engine.AddComponent<StoryManager>();
        engine.AddComponent<AudioManager>();
        engine.AddComponent<VFXHelper>();

        Debug.Log("DDA System: All managers successfully initialized and injected!");
    }
}
