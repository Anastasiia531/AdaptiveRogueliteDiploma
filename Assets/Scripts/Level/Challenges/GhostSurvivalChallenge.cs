using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostSurvivalChallenge : ChallengeRoom
{
    private float timer = 30f;
    private List<GameObject> ghosts = new List<GameObject>();

    protected override void StartChallenge()
    {
        // DDA: scale difficulty
        int ghostCount = (skillIndexAtStart < 0.4f) ? 2 : 5;
        float ghostSpeed = (skillIndexAtStart < 0.4f) ? 1.2f : 2.5f;
        bool homing = (skillIndexAtStart >= 0.4f);

        Debug.LogFormat("Starting GhostSurvivalChallenge: count={0}, speed={1:F1}, homing={2}", ghostCount, ghostSpeed, homing);

        // Try to find a sprite for ghosts (using player body renderer or general asset if available)
        Sprite ghostSprite = null;
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            var sr = GameManager.Instance.player.Body.GetComponent<SpriteRenderer>();
            if (sr != null) ghostSprite = sr.sprite;
        }

        for (int i = 0; i < ghostCount; i++)
        {
            GameObject ghostGo = new GameObject("ChallengeGhost");
            ghostGo.transform.parent = monsterContainer;
            
            // Position ghosts around the room boundaries
            float angle = i * (2f * Mathf.PI / ghostCount);
            ghostGo.transform.localPosition = new Vector2(Mathf.Cos(angle) * 6f, Mathf.Sin(angle) * 3f);
            
            var sr = ghostGo.AddComponent<SpriteRenderer>();
            if (ghostSprite != null)
            {
                sr.sprite = ghostSprite;
            }
            sr.color = new Color(0.7f, 0.85f, 1f, 0.5f); // Translucent blue ghost effect
            sr.sortingOrder = 5;

            var ghostScript = ghostGo.AddComponent<GhostFollowPlayer>();
            ghostScript.speed = ghostSpeed;
            ghostScript.homing = homing;
            ghosts.Add(ghostGo);
        }
        
        StartCoroutine(SurvivalTimerRoutine());
    }

    private IEnumerator SurvivalTimerRoutine()
    {
        float timeLeft = timer;
        while (timeLeft > 0 && GameManager.Instance.player != null && GameManager.Instance.player.isLive)
        {
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        // Cleanup
        foreach (var ghost in ghosts)
        {
            if (ghost != null) Destroy(ghost);
        }
        ghosts.Clear();

        bool success = GameManager.Instance.player != null && GameManager.Instance.player.isLive;
        CompleteChallenge(success);
    }
}

public class GhostFollowPlayer : MonoBehaviour
{
    public float speed;
    public bool homing;
    private Player player;
    private Vector2 randomDir;

    private void Start()
    {
        player = GameManager.Instance.player;
        randomDir = Random.insideUnitCircle.normalized;
        if (randomDir == Vector2.zero) randomDir = Vector2.right;

        // Add physical check for collision with player
        var col = gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.6f);
        col.isTrigger = true;
    }

    private void Update()
    {
        if (player == null || !player.isLive) return;

        if (homing)
        {
            Vector2 dir = (player.transform.position - transform.position).normalized;
            transform.Translate(dir * speed * Time.deltaTime);
        }
        else
        {
            transform.Translate(randomDir * speed * Time.deltaTime);
            // Bounce off boundaries of typical room sizes
            if (Mathf.Abs(transform.localPosition.x) > 8f)
            {
                randomDir.x = -randomDir.x;
                transform.localPosition = new Vector2(Mathf.Sign(transform.localPosition.x) * 8f, transform.localPosition.y);
            }
            if (Mathf.Abs(transform.localPosition.y) > 4f)
            {
                randomDir.y = -randomDir.y;
                transform.localPosition = new Vector2(transform.localPosition.x, Mathf.Sign(transform.localPosition.y) * 4f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var p = other.GetComponent<Player>();
            if (p != null)
            {
                p.BeAttacked(1, (other.transform.position - transform.position).normalized);
            }
        }
    }
}
