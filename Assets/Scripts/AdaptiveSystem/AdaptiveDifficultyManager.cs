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
        set { skillIndex = Mathf.Clamp01(value); }
    }

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

    public void LogPlayerDamage(int damage)
    {
        // Reduce skill index upon taking damage. Higher damage has a bigger penalty.
        float penalty = damage * 0.04f;
        SkillIndex -= penalty;
        Debug.LogFormat("DDA: Player took {0} damage. SkillIndex decreased to {1:F2}", damage, SkillIndex);
    }

    public void LogChallengeResult(bool success)
    {
        if (success)
        {
            SkillIndex += 0.1f;
            Debug.LogFormat("DDA: Challenge Success! SkillIndex increased to {0:F2}", SkillIndex);
        }
        else
        {
            SkillIndex -= 0.08f;
            Debug.LogFormat("DDA: Challenge Failed! SkillIndex decreased to {0:F2}", SkillIndex);
        }
    }

    public void LogRoomClear()
    {
        // Reward slightly for successfully clearing standard rooms
        SkillIndex += 0.02f;
        Debug.LogFormat("DDA: Room Cleared. SkillIndex increased to {0:F2}", SkillIndex);
    }
}
