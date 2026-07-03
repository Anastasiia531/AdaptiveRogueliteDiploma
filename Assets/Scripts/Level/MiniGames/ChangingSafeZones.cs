using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangingSafeZones : ChallengeRoom
{
    private float challengeDuration = 25f;
    private GameObject safeZoneObj;
    private bool playerInSafeZone = false;
    private float safeZoneSize;
    private float moveInterval;
    private bool isDamagingRoom = false;

    protected override void StartChallenge()
    {
        // DDA variables
        safeZoneSize = (skillIndexAtStart < 0.4f) ? 2.5f : 1.2f;
        moveInterval = (skillIndexAtStart < 0.4f) ? 4.0f : 2.0f;

        Debug.LogFormat("Starting ChangingSafeZones: size={0:F1}, interval={1:F1}s", safeZoneSize, moveInterval);

        // Spawn Safe Zone GameObject
        safeZoneObj = new GameObject("SafeZoneFloor");
        safeZoneObj.transform.parent = itemContainer;
        safeZoneObj.transform.localPosition = Vector2.zero; // Start at center

        var sr = safeZoneObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(Color.white, Color.black);
        sr.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
        sr.sortingOrder = 1;

        safeZoneObj.transform.localScale = new Vector3(safeZoneSize, safeZoneSize, 1f);

        var col = safeZoneObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
        col.isTrigger = true;

        var helper = safeZoneObj.AddComponent<SafeZoneHelper>();
        helper.challenge = this;

        playerInSafeZone = false;
        isDamagingRoom = true;

        StartCoroutine(SafeZoneMovementRoutine());
        StartCoroutine(DamageTickRoutine());
        StartCoroutine(SurvivalTimerRoutine());
    }

    private IEnumerator SafeZoneMovementRoutine()
    {
        while (challengeActive && GameManager.Instance.player != null && GameManager.Instance.player.isLive)
        {
            yield return new WaitForSeconds(moveInterval - 0.5f);

            // Warning: Flash Safe Zone yellow/red before moving
            var sr = safeZoneObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1f, 0.5f, 0f, 0.4f); // Orange warning
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(700f, 0.1f, 0.1f);
                }
            }

            yield return new WaitForSeconds(0.5f);
            if (!challengeActive) yield break;

            // Move to a new random location in the room
            Vector2 newPos = new Vector2(Random.Range(-7f, 7f), Random.Range(-3f, 3f));
            safeZoneObj.transform.localPosition = newPos;

            // Restore green color
            if (sr != null)
            {
                sr.color = new Color(0f, 1f, 0f, 0.3f);
            }
        }
    }

    private IEnumerator DamageTickRoutine()
    {
        while (challengeActive && isDamagingRoom && GameManager.Instance.player != null && GameManager.Instance.player.isLive)
        {
            yield return new WaitForSeconds(1.0f); // Damage tick rate: 1s

            if (challengeActive && !playerInSafeZone && GameManager.Instance.player != null && GameManager.Instance.player.isLive)
            {
                Debug.Log("Player is outside the Safe Zone! Taking tick damage.");
                GameManager.Instance.player.BeAttacked(1, Vector2.zero);
            }
        }
    }

    private IEnumerator SurvivalTimerRoutine()
    {
        float timeLeft = challengeDuration;
        while (timeLeft > 0 && GameManager.Instance.player != null && GameManager.Instance.player.isLive)
        {
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        CleanupSafeZones();

        bool success = GameManager.Instance.player != null && GameManager.Instance.player.isLive;
        CompleteChallenge(success);
    }

    public void SetPlayerSafeState(bool isSafe)
    {
        playerInSafeZone = isSafe;
        Debug.Log("Player inside safe zone: " + isSafe);
    }

    private void CleanupSafeZones()
    {
        isDamagingRoom = false;
        if (safeZoneObj != null) Destroy(safeZoneObj);
    }

    protected override void CompleteChallenge(bool success)
    {
        CleanupSafeZones();
        base.CompleteChallenge(success);
    }
}

public class SafeZoneHelper : MonoBehaviour
{
    public ChangingSafeZones challenge;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (challenge != null) challenge.SetPlayerSafeState(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (challenge != null) challenge.SetPlayerSafeState(false);
        }
    }
}
