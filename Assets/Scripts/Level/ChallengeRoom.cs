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
                    if (anim != null) anim.Play("DoorClose");
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
}
