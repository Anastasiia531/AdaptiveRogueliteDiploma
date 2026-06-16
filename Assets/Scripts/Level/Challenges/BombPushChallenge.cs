using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombPushChallenge : ChallengeRoom
{
    private GameObject bombObj;
    private GameObject targetObj;
    private float bombFuse;
    private float elapsedFuse = 0f;
    private bool challengeEnded = false;

    protected override void StartChallenge()
    {
        // DDA: scale fuse timer
        bombFuse = (skillIndexAtStart < 0.4f) ? 20.0f : 10.0f;
        elapsedFuse = 0f;
        challengeEnded = false;

        Debug.LogFormat("Starting BombPushChallenge: fuse={0:F1}s", bombFuse);

        // Spawn target at upper part of the room
        targetObj = new GameObject("BombTarget");
        targetObj.transform.parent = itemContainer;
        targetObj.transform.localPosition = new Vector2(0f, 3f);

        var targetSr = targetObj.AddComponent<SpriteRenderer>();
        targetSr.color = Color.green;
        targetSr.sortingOrder = 1;
        targetObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        var targetCol = targetObj.AddComponent<BoxCollider2D>();
        targetCol.size = new Vector2(1.2f, 1.2f);
        targetCol.isTrigger = true;

        // Spawn bomb at lower part
        bombObj = new GameObject("PushBomb");
        bombObj.transform.parent = itemContainer;
        bombObj.transform.localPosition = new Vector2(0f, -2.5f);

        var bombSr = bombObj.AddComponent<SpriteRenderer>();
        bombSr.color = Color.black;
        bombSr.sortingOrder = 4;
        bombObj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        var bombCol = bombObj.AddComponent<CircleCollider2D>();
        bombCol.radius = 0.4f;

        var rb = bombObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.drag = 2.0f; // High linear drag so it doesn't slide forever
        rb.angularDrag = 2.0f;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var pushScript = bombObj.AddComponent<PhysicsPushBomb>();
        pushScript.challengeRoom = this;

        // Spawn some obstacles/barricades based on DDA
        if (skillIndexAtStart >= 0.4f)
        {
            // Obstacles on the sides to narrow the path
            Vector2[] obsPos = { new Vector2(-2.5f, 0f), new Vector2(2.5f, 0f) };
            for (int i = 0; i < obsPos.Length; i++)
            {
                GameObject obs = new GameObject("BombObstacle_" + i);
                obs.transform.parent = defaultContainer;
                obs.transform.localPosition = obsPos[i];
                var obsSr = obs.AddComponent<SpriteRenderer>();
                obsSr.color = Color.gray;
                obsSr.sortingOrder = 2;
                obs.transform.localScale = new Vector3(2f, 0.8f, 1f);
                var obsCol = obs.AddComponent<BoxCollider2D>();
                obsCol.size = new Vector2(1f, 1f);
            }
        }
    }

    private void Update()
    {
        if (!challengeActive || challengeEnded) return;

        elapsedFuse += Time.deltaTime;

        // Flash bomb red as fuse runs out
        if (bombObj != null)
        {
            var sr = bombObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float t = elapsedFuse / bombFuse;
                sr.color = Color.Lerp(Color.black, Color.red, t);
                
                // Pulsing scale
                float pulse = 1f + Mathf.PingPong(elapsedFuse * (1f + t * 4f), 0.2f);
                bombObj.transform.localScale = new Vector3(0.8f * pulse, 0.8f * pulse, 1f);
            }
        }

        if (elapsedFuse >= bombFuse)
        {
            OnBombExploded();
        }
    }

    public void OnBombReachedTarget()
    {
        if (challengeEnded) return;
        challengeEnded = true;
        Debug.Log("Bomb reached target! Success.");
        
        CleanupBombChallenge();
        CompleteChallenge(true);
    }

    private void OnBombExploded()
    {
        if (challengeEnded) return;
        challengeEnded = true;
        Debug.Log("Bomb exploded! Failure.");

        // Deal damage if player is close
        Player player = GameManager.Instance.player;
        if (player != null && bombObj != null)
        {
            float dist = Vector3.Distance(player.transform.position, bombObj.transform.position);
            if (dist < 3.5f)
            {
                player.BeAttacked(1, (player.transform.position - bombObj.transform.position).normalized);
            }
        }

        // Play loud sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(100f, 0.5f, 0.4f, true);
        }

        CleanupBombChallenge();
        CompleteChallenge(false);
    }

    private void CleanupBombChallenge()
    {
        if (bombObj != null) Destroy(bombObj);
        if (targetObj != null) Destroy(targetObj);
        
        // Remove obstacles
        for (int i = 0; i < defaultContainer.childCount; i++)
        {
            var child = defaultContainer.GetChild(i).gameObject;
            if (child.name.StartsWith("BombObstacle"))
            {
                Destroy(child);
            }
        }
    }
}

public class PhysicsPushBomb : MonoBehaviour
{
    public BombPushChallenge challengeRoom;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Detect bullet collision
        Bullet bullet = other.GetComponent<Bullet>();
        if (bullet != null && bullet.isPlayerFlag)
        {
            if (rb != null)
            {
                var bulletRb = other.GetComponent<Rigidbody2D>();
                Vector2 dir = (bulletRb != null) ? bulletRb.velocity.normalized : Vector2.up;
                rb.AddForce(dir * 12f, ForceMode2D.Impulse);
                Debug.Log("Bomb pushed by bullet!");
            }

            // Recycle bullet
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.PushObject(other.gameObject);
            }
            else
            {
                Destroy(other.gameObject);
            }
        }
        // Detect target trigger
        else if (other.gameObject.name == "BombTarget")
        {
            if (challengeRoom != null)
            {
                challengeRoom.OnBombReachedTarget();
            }
        }
    }
}
