using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickTileReaction : ChallengeRoom
{
    private List<GameObject> tiles = new List<GameObject>();
    private int tilesRemaining;
    private float tileTimer;
    private bool challengeFailed = false;

    protected override void StartChallenge()
    {
        // DDA variables
        int tileCount = (skillIndexAtStart < 0.4f) ? 3 : 6;
        tileTimer = (skillIndexAtStart < 0.4f) ? 3.0f : 1.2f;
        float tileSize = (skillIndexAtStart < 0.4f) ? 1.4f : 0.8f;

        tilesRemaining = tileCount;
        challengeFailed = false;

        Debug.LogFormat("Starting QuickTileReaction: tiles={0}, timer={1:F1}s, size={2}", tileCount, tileTimer, tileSize);

        // Spawn tiles at random locations
        for (int i = 0; i < tileCount; i++)
        {
            GameObject tileGo = new GameObject("ReactionTile_" + i);
            tileGo.transform.parent = itemContainer;
            tileGo.transform.localPosition = new Vector2(Random.Range(-6f, 6f), Random.Range(-3f, 3f));

            var sr = tileGo.AddComponent<SpriteRenderer>();
            sr.color = Color.yellow;
            sr.sortingOrder = 1; // Floor layer

            tileGo.transform.localScale = new Vector3(tileSize, tileSize, 1f);

            var col = tileGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            col.isTrigger = true;

            var tileScript = tileGo.AddComponent<ReactionTile>();
            tileScript.timer = tileTimer;
            tileScript.challengeRoom = this;

            tiles.Add(tileGo);
        }
    }

    public void OnTileStepped(GameObject tileObj)
    {
        if (challengeFailed || !challengeActive) return;

        tilesRemaining--;
        tiles.Remove(tileObj);
        Destroy(tileObj);

        // Play brief beep
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(880f, 0.05f, 0.1f);
        }

        if (tilesRemaining <= 0)
        {
            CompleteChallenge(true);
        }
    }

    public void OnTileExpired(GameObject tileObj)
    {
        if (challengeFailed || !challengeActive) return;
        challengeFailed = true;

        Debug.Log("Reaction tile timer expired! Challenge failed.");

        // Deal 1 damage to player as penalty
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            GameManager.Instance.player.BeAttacked(1, Vector2.zero);
        }

        // Cleanup remaining tiles
        foreach (var tile in tiles)
        {
            if (tile != null) Destroy(tile);
        }
        tiles.Clear();

        CompleteChallenge(false);
    }
}

public class ReactionTile : MonoBehaviour
{
    public float timer;
    public QuickTileReaction challengeRoom;
    private float timeElapsed = 0f;
    private SpriteRenderer sr;
    private bool stepped = false;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (stepped) return;

        timeElapsed += Time.deltaTime;

        // Flash/transition color from yellow to red as time expires
        if (sr != null)
        {
            sr.color = Color.Lerp(Color.yellow, Color.red, timeElapsed / timer);
        }

        if (timeElapsed >= timer)
        {
            challengeRoom.OnTileExpired(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!stepped && other.CompareTag("Player"))
        {
            stepped = true;
            challengeRoom.OnTileStepped(gameObject);
        }
    }
}
