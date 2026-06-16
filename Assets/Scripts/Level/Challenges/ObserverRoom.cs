using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObserverRoom : ChallengeRoom
{
    private List<GameObject> eyeIndicators = new List<GameObject>();
    private GameObject finishZone;
    private bool goalReached = false;
    private Vector3 lastPlayerPosition;

    protected override void StartChallenge()
    {
        Debug.Log("Starting ObserverRoom Challenge (Red Light, Green Light).");
        goalReached = false;

        Player player = GameManager.Instance.player;
        if (player != null)
        {
            player.transform.position = transform.position + new Vector3(-8f, 0f, 0f);
            lastPlayerPosition = player.transform.position;
        }

        // Spawn 3 Eye indicators on the top wall
        Vector2[] eyePos = { new Vector2(-4f, 4f), new Vector2(0f, 4f), new Vector2(4f, 4f) };
        for (int i = 0; i < eyePos.Length; i++)
        {
            GameObject eye = new GameObject("ObserverEye_" + i);
            eye.transform.parent = itemContainer;
            eye.transform.localPosition = eyePos[i];

            var sr = eye.AddComponent<SpriteRenderer>();
            sr.color = Color.green; // Starts safe (green/closed)
            sr.sortingOrder = 3;
            eye.transform.localScale = new Vector3(1.2f, 0.6f, 1f);

            eyeIndicators.Add(eye);
        }

        // Spawn finish zone on the right
        finishZone = new GameObject("ObserverFinish");
        finishZone.transform.parent = itemContainer;
        finishZone.transform.localPosition = new Vector2(8f, 0f);

        var finishSr = finishZone.AddComponent<SpriteRenderer>();
        finishSr.color = Color.green * 0.8f;
        finishSr.sortingOrder = 1;
        finishZone.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        var finishCol = finishZone.AddComponent<BoxCollider2D>();
        finishCol.size = new Vector2(1f, 1f);
        finishCol.isTrigger = true;

        var trigger = finishZone.AddComponent<ObserverFinishTrigger>();
        trigger.challengeRoom = this;

        StartCoroutine(ObserverStateRoutine());
    }

    private IEnumerator ObserverStateRoutine()
    {
        // Alternate between open and closed eyes
        while (!goalReached && GameManager.Instance.player != null && GameManager.Instance.player.isLive)
        {
            // Green Light: Closed eyes (Safe to move)
            UpdateEyesColor(Color.green);
            if (AudioManager.Instance != null)
            {
                // Play soft double beep indicating green light
                AudioManager.Instance.PlaySound(523.25f, 0.1f, 0.15f);
            }
            yield return new WaitForSeconds(Random.Range(2.5f, 4.0f));

            if (goalReached || !challengeActive) yield break;

            // Red Light: Open eyes (DO NOT MOVE)
            UpdateEyesColor(Color.red);
            if (AudioManager.Instance != null)
            {
                // Play alarm buzz indicating red light
                AudioManager.Instance.PlaySound(220f, 0.3f, 0.25f);
            }

            // Small reaction window (0.15s) before checking movement
            yield return new WaitForSeconds(0.15f);

            float redLightDuration = Random.Range(1.5f, 3.0f);
            float elapsed = 0f;

            Player player = GameManager.Instance.player;
            if (player != null) lastPlayerPosition = player.transform.position;

            while (elapsed < redLightDuration && !goalReached && player != null && player.isLive)
            {
                elapsed += Time.deltaTime;

                // Check player movement
                if (player != null && player.isLive)
                {
                    float distMoved = Vector3.Distance(player.transform.position, lastPlayerPosition);
                    if (distMoved > 0.05f)
                    {
                        Debug.LogWarningFormat("Observer caught player moving! Distance: {0:F3}", distMoved);
                        player.BeAttacked(1, Vector2.zero);
                        // Brief invincibility prevents instant double damage, but update position to check next frame
                    }
                    lastPlayerPosition = player.transform.position;
                }

                yield return null;
            }
        }
    }

    private void UpdateEyesColor(Color color)
    {
        foreach (var eye in eyeIndicators)
        {
            if (eye != null)
            {
                var sr = eye.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = color;
            }
        }
    }

    public void OnGoalReached()
    {
        if (!challengeActive) return;
        goalReached = true;
        Debug.Log("Player reached the Observer room goal successfully!");
        CleanupObserver();
        CompleteChallenge(true);
    }

    private void CleanupObserver()
    {
        foreach (var eye in eyeIndicators)
        {
            if (eye != null) Destroy(eye);
        }
        eyeIndicators.Clear();

        if (finishZone != null) Destroy(finishZone);
    }

    protected override void CompleteChallenge(bool success)
    {
        goalReached = true;
        CleanupObserver();
        base.CompleteChallenge(success);
    }
}

public class ObserverFinishTrigger : MonoBehaviour
{
    public ObserverRoom challengeRoom;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (challengeRoom != null)
            {
                challengeRoom.OnGoalReached();
            }
        }
    }
}
