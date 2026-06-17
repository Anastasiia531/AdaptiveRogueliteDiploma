using UnityEngine;

public class AdaptiveDifficultyManager : MonoBehaviour
{
    public static AdaptiveDifficultyManager Instance { get; private set; }

    [Header("DDA Settings")]
    [Range(0f, 1f)]
    [SerializeField]
    private float skillIndex = 0.5f;

    public float SkillIndex
    {
        get { return skillIndex; }
        set
        {
            skillIndex = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("DDA_SkillIndex", skillIndex);
            PlayerPrefs.Save();
        }
    }

    [Header("Player Tracking Stats")]
    public int totalDamageTaken = 0;
    public int challengesCleared = 0;
    public int challengesFailed = 0;
    public int roomsCleared = 0;

    [Header("Accuracy Tracking")]
    public int bulletsShot = 0;
    public int bulletsHit = 0;

    [Header("Speed Tracking")]
    public float totalClearTime = 0f;
    public int timedRoomsClearedCount = 0;
    private float roomStartTime;
    private bool isTrackingRoomTime = false;

    public int GetGameScore()
    {
        int score = (roomsCleared * 100) + (challengesCleared * 250) - (totalDamageTaken * 25);
        return Mathf.Max(0, score);
    }

    public void ResetPlaythroughStats()
    {
        totalDamageTaken = 0;
        challengesCleared = 0;
        challengesFailed = 0;
        roomsCleared = 0;
        bulletsShot = 0;
        bulletsHit = 0;
        totalClearTime = 0f;
        timedRoomsClearedCount = 0;
        isTrackingRoomTime = false;
        Debug.Log("DDA: Playthrough stats reset for a new game.");
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            skillIndex = PlayerPrefs.GetFloat("DDA_SkillIndex", 0.5f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LogPlayerDamage(int damage)
    {
        totalDamageTaken += damage;
        // Reduce skill index upon taking damage. Higher damage has a bigger penalty.
        float penalty = damage * 0.04f;
        SkillIndex -= penalty;
        Debug.LogFormat("DDA: Player took {0} damage. SkillIndex decreased to {1:F2}", damage, SkillIndex);
    }

    public void LogChallengeResult(bool success)
    {
        if (success)
        {
            challengesCleared++;
            SkillIndex += 0.1f;
            Debug.LogFormat("DDA: Challenge Success! SkillIndex increased to {0:F2}", SkillIndex);
        }
        else
        {
            challengesFailed++;
            SkillIndex -= 0.08f;
            Debug.LogFormat("DDA: Challenge Failed! SkillIndex decreased to {0:F2}", SkillIndex);
        }
    }

    public void LogRoomClear()
    {
        roomsCleared++;
        // Reward slightly for successfully clearing standard rooms
        SkillIndex += 0.02f;
        Debug.LogFormat("DDA: Room Cleared. SkillIndex increased to {0:F2}", SkillIndex);
    }

    public void LogPlayerShoot()
    {
        bulletsShot++;
    }

    public void LogPlayerHit()
    {
        bulletsHit++;
    }

    public float GetAccuracy()
    {
        return bulletsShot > 0 ? (float)bulletsHit / bulletsShot : 1.0f;
    }

    public float GetAverageRoomClearTime()
    {
        return timedRoomsClearedCount > 0 ? totalClearTime / timedRoomsClearedCount : 0f;
    }

    public void OnRoomEntered(Room room)
    {
        if (room != null && room.roomType == RoomType.Normal && room.monsterContainer != null && room.monsterContainer.childCount > 0 && !room.isCleared)
        {
            roomStartTime = Time.time;
            isTrackingRoomTime = true;
        }
        else
        {
            isTrackingRoomTime = false;
        }
    }

    public void OnRoomCleared()
    {
        if (isTrackingRoomTime)
        {
            float duration = Time.time - roomStartTime;
            totalClearTime += duration;
            timedRoomsClearedCount++;
            isTrackingRoomTime = false;
        }
    }

    public void EvaluateSkillBeforeGeneration()
    {
        // 1. Accuracy modifier (evaluated before next floor generation)
        float accuracy = GetAccuracy();
        float accuracyMod = 0f;
        if (bulletsShot > 5)
        {
            if (accuracy > 0.65f) accuracyMod = 0.05f;       // Good accuracy increases difficulty
            else if (accuracy < 0.30f) accuracyMod = -0.05f; // Poor accuracy decreases difficulty
        }

        // 2. Room clear speed modifier
        float avgTime = GetAverageRoomClearTime();
        float speedMod = 0f;
        if (timedRoomsClearedCount > 0)
        {
            if (avgTime < 18f) speedMod = 0.05f;        // Fast speed increases difficulty
            else if (avgTime > 35f) speedMod = -0.05f;  // Slow speed decreases difficulty
        }

        // Apply DDA adjustments
        SkillIndex += accuracyMod + speedMod;

        Debug.LogFormat("DDA Pre-Generation Evaluation: Accuracy={0:F1}%, Avg Clear Time={1:F1}s. SkillIndex adjusted to: {2:F2}", 
            accuracy * 100f, avgTime, SkillIndex);
    }
}
