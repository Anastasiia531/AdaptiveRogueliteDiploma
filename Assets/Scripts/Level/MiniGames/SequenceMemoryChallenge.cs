using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SequenceMemoryChallenge : ChallengeRoom
{
    private List<GameObject> buttons = new List<GameObject>();
    private List<int> correctSequence = new List<int>();
    private List<int> playerSequence = new List<int>();
    private bool showingSequence = true;
    private bool inputFailed = false;

    // Button colors: 0 = Red, 1 = Green, 2 = Blue, 3 = Yellow
    private Color[] btnColors = { Color.red, Color.green, Color.blue, Color.yellow };

    protected override void StartChallenge()
    {
        int sequenceLength = (skillIndexAtStart < 0.4f) ? 3 : 6;
        float flashInterval = (skillIndexAtStart < 0.4f) ? 0.8f : 0.4f;
        int buttonCount = 4; // Red, Green, Blue, Yellow

        Debug.LogFormat("Starting SequenceMemoryChallenge: length={0}, speed={1:F2}s", sequenceLength, flashInterval);

        // Spawn 4 buttons in a square/cross pattern around center
        Vector2[] positions = {
            new Vector2(-2f, 1f),  // Top Left
            new Vector2(2f, 1f),   // Top Right
            new Vector2(-2f, -1f), // Bottom Left
            new Vector2(2f, -1f)   // Bottom Right
        };

        for (int i = 0; i < buttonCount; i++)
        {
            GameObject btnGo = new GameObject("MemoryButton_" + i);
            btnGo.transform.parent = itemContainer;
            btnGo.transform.localPosition = positions[i];

            var sr = btnGo.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite(Color.white, Color.black);
            sr.color = btnColors[i] * 0.5f; // Dim color initially
            sr.sortingOrder = 1;

            btnGo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var col = btnGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            col.isTrigger = true;

            var trigger = btnGo.AddComponent<MemoryButtonTrigger>();
            trigger.buttonId = i;
            trigger.challengeRoom = this;

            buttons.Add(btnGo);
        }

        // Generate random sequence
        for (int i = 0; i < sequenceLength; i++)
        {
            correctSequence.Add(Random.Range(0, buttonCount));
        }

        StartCoroutine(ShowSequenceRoutine(flashInterval));
    }

    private IEnumerator ShowSequenceRoutine(float interval)
    {
        showingSequence = true;
        playerSequence.Clear();
        
        // Pause player control briefly so they pay attention
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            GameManager.Instance.player.PlayerPause();
        }

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < correctSequence.Count; i++)
        {
            int btnIndex = correctSequence[i];
            GameObject btnObj = buttons[btnIndex];
            
            // Light up button
            var sr = btnObj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = btnColors[btnIndex];
            
            // Play flash tone
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(300f + btnIndex * 100f, 0.15f, 0.15f);
            }

            yield return new WaitForSeconds(interval);

            // Dim button back
            if (sr != null) sr.color = btnColors[btnIndex] * 0.5f;

            yield return new WaitForSeconds(interval * 0.3f);
        }

        // Resume player control
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            GameManager.Instance.player.PlayerResume();
        }

        showingSequence = false;
        Debug.Log("Sequence complete. Player input active.");
    }

    public void OnButtonStepped(int id)
    {
        if (showingSequence || inputFailed || !challengeActive) return;

        playerSequence.Add(id);
        int currentInputIndex = playerSequence.Count - 1;

        // Visual flash response
        StartCoroutine(FlashButtonFeedback(id, playerSequence[currentInputIndex] == correctSequence[currentInputIndex]));

        if (playerSequence[currentInputIndex] != correctSequence[currentInputIndex])
        {
            // Wrong button!
            inputFailed = true;
            Debug.Log("Wrong sequence! Challenge failed.");
            
            if (GameManager.Instance != null && GameManager.Instance.player != null)
            {
                GameManager.Instance.player.BeAttacked(1, Vector2.zero);
            }

            CleanupButtons();
            CompleteChallenge(false);
        }
        else if (playerSequence.Count == correctSequence.Count)
        {
            // Successfully completed the sequence!
            Debug.Log("Correct sequence completed!");
            CleanupButtons();
            CompleteChallenge(true);
        }
    }

    private IEnumerator FlashButtonFeedback(int id, bool correct)
    {
        if (id < 0 || id >= buttons.Count) yield break;
        var sr = buttons[id].GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        sr.color = correct ? Color.white : Color.black;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(correct ? 600f : 150f, 0.1f, 0.15f);
        }

        yield return new WaitForSeconds(0.2f);
        if (sr != null) sr.color = btnColors[id] * 0.5f;
    }

    private void CleanupButtons()
    {
        foreach (var btn in buttons)
        {
            if (btn != null) Destroy(btn);
        }
        buttons.Clear();
    }
}

public class MemoryButtonTrigger : MonoBehaviour
{
    public int buttonId;
    public SequenceMemoryChallenge challengeRoom;
    private bool inside = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!inside && other.CompareTag("Player"))
        {
            inside = true;
            if (challengeRoom != null)
            {
                challengeRoom.OnButtonStepped(buttonId);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inside = false;
        }
    }
}
