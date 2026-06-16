using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyTearsSurvivalChallenge : ChallengeRoom
{
    private float timer = 25f;
    private List<GameObject> activeTears = new List<GameObject>();
    private float warningDelay;
    private float spawnRate;

    protected override void StartChallenge()
    {
        // DDA variables
        warningDelay = (skillIndexAtStart < 0.4f) ? 1.5f : 0.6f;
        spawnRate = (skillIndexAtStart < 0.4f) ? 1.0f : 0.4f; // Interval between spawns

        Debug.LogFormat("Starting SkyTearsSurvivalChallenge: warning={0:F1}s, rate={1:F1}s", warningDelay, spawnRate);

        StartCoroutine(SpawnTearsRoutine());
        StartCoroutine(SurvivalTimerRoutine());
    }

    private IEnumerator SpawnTearsRoutine()
    {
        while (challengeActive && GameManager.Instance.player != null && GameManager.Instance.player.isLive)
        {
            // Pick a random spot in the room
            Vector2 targetPos = new Vector2(Random.Range(-7f, 7f), Random.Range(-3.5f, 3.5f));
            StartCoroutine(TearDropRoutine(targetPos));

            yield return new WaitForSeconds(spawnRate);
        }
    }

    private IEnumerator TearDropRoutine(Vector2 targetPos)
    {
        // 1. Spawn Warning Indicator (Red Circle)
        GameObject indicator = new GameObject("TearWarning");
        indicator.transform.parent = itemContainer;
        indicator.transform.localPosition = targetPos;
        
        var sr = indicator.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0f, 0f, 0.4f); // Translucent red
        sr.sortingOrder = 1;
        indicator.transform.localScale = Vector3.one * 1.2f;

        activeTears.Add(indicator);

        // Flash indicator over time
        float elapsed = 0f;
        while (elapsed < warningDelay)
        {
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                // Rapidly flash opacity as warning timer nears zero
                float flash = Mathf.PingPong(elapsed * (5f / warningDelay), 0.5f);
                sr.color = new Color(1f, 0f, 0f, 0.2f + flash);
            }
            yield return null;
        }

        // 2. Impact
        if (indicator != null)
        {
            Destroy(indicator);
            activeTears.Remove(indicator);
        }

        if (!challengeActive || GameManager.Instance.player == null) yield break;

        // Visual impact splash
        GameObject splash = new GameObject("TearImpact");
        splash.transform.parent = itemContainer;
        splash.transform.localPosition = targetPos;
        var splashSr = splash.AddComponent<SpriteRenderer>();
        splashSr.color = Color.red;
        splashSr.sortingOrder = 3;
        splash.transform.localScale = Vector3.one * 1.5f;
        
        // Play procedural drip/drop sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(440f, 0.05f, 0.05f);
        }

        // Check damage to player
        Player player = GameManager.Instance.player;
        if (player != null && player.isLive)
        {
            float dist = Vector3.Distance(player.transform.position, splash.transform.position);
            if (dist < 1.0f)
            {
                player.BeAttacked(1, (player.transform.position - splash.transform.position).normalized);
            }
        }

        yield return new WaitForSeconds(0.15f);
        Destroy(splash);
    }

    private IEnumerator SurvivalTimerRoutine()
    {
        float timeLeft = timer;
        while (timeLeft > 0 && GameManager.Instance.player != null && GameManager.Instance.player.isLive)
        {
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        CleanupTears();

        bool success = GameManager.Instance.player != null && GameManager.Instance.player.isLive;
        CompleteChallenge(success);
    }

    private void CleanupTears()
    {
        foreach (var tear in activeTears)
        {
            if (tear != null) Destroy(tear);
        }
        activeTears.Clear();
    }

    protected override void CompleteChallenge(bool success)
    {
        CleanupTears();
        base.CompleteChallenge(success);
    }
}
