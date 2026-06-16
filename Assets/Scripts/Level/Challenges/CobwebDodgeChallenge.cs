using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CobwebDodgeChallenge : ChallengeRoom
{
    private List<GameObject> activeObjects = new List<GameObject>();
    private GameObject finishZone;

    protected override void StartChallenge()
    {
        Debug.Log("Starting CobwebDodgeChallenge.");

        // Move player to the left side
        Player player = GameManager.Instance.player;
        if (player != null)
        {
            player.transform.position = transform.position + new Vector3(-8f, 0f, 0f);
        }

        // Spawn a large cobweb area in the center of the room
        GameObject web = new GameObject("CobwebArea");
        web.transform.parent = itemContainer;
        web.transform.localPosition = new Vector2(0f, 0f);

        var webSr = web.AddComponent<SpriteRenderer>();
        webSr.color = new Color(0.9f, 0.9f, 0.9f, 0.3f); // Semi-transparent white web color
        webSr.sortingOrder = 1;
        web.transform.localScale = new Vector3(12f, 6f, 1f); // Covers a huge part of the room

        var webCol = web.AddComponent<BoxCollider2D>();
        webCol.size = new Vector2(1f, 1f);
        webCol.isTrigger = true;
        web.AddComponent<CobwebSlowTrigger>();

        activeObjects.Add(web);

        // Spawn 2 corner turrets
        Vector2[] turretPos = { new Vector2(-7f, 3f), new Vector2(7f, -3f) };
        for (int i = 0; i < turretPos.Length; i++)
        {
            GameObject turret = new GameObject("StaticTurret_" + i);
            turret.transform.parent = monsterContainer;
            turret.transform.localPosition = turretPos[i];

            var sr = turret.AddComponent<SpriteRenderer>();
            sr.color = Color.grey * 0.7f;
            sr.sortingOrder = 3;
            turret.transform.localScale = new Vector3(1f, 1.2f, 1f);

            var turretScript = turret.AddComponent<TurretShooter>();
            // DDA: fire rate scales with SkillIndex
            turretScript.fireInterval = (skillIndexAtStart < 0.4f) ? 2.5f : 1.2f;

            activeObjects.Add(turret);
        }

        // Spawn destination zone on the right
        finishZone = new GameObject("CobwebFinish");
        finishZone.transform.parent = itemContainer;
        finishZone.transform.localPosition = new Vector2(8f, 0f);

        var finishSr = finishZone.AddComponent<SpriteRenderer>();
        finishSr.color = Color.green * 0.8f;
        finishSr.sortingOrder = 1;
        finishZone.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        var finishCol = finishZone.AddComponent<BoxCollider2D>();
        finishCol.size = new Vector2(1f, 1f);
        finishCol.isTrigger = true;

        var trigger = finishZone.AddComponent<CobwebFinishTrigger>();
        trigger.challengeRoom = this;

        activeObjects.Add(finishZone);
    }

    public void OnGoalReached()
    {
        if (!challengeActive) return;
        Debug.Log("Player navigated the cobweb and reached the goal!");

        // Restore player speed if they are still slowed
        Player player = GameManager.Instance.player;
        if (player != null)
        {
            player.speed = player.SPEED;
        }

        CleanupCobwebs();
        CompleteChallenge(true);
    }

    private void CleanupCobwebs()
    {
        // Restore player speed just in case
        Player player = GameManager.Instance.player;
        if (player != null)
        {
            player.speed = player.SPEED;
        }

        foreach (var go in activeObjects)
        {
            if (go != null) Destroy(go);
        }
        activeObjects.Clear();
    }

    protected override void CompleteChallenge(bool success)
    {
        CleanupCobwebs();
        base.CompleteChallenge(success);
    }
}

public class CobwebSlowTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<Player>();
            if (player != null)
            {
                player.speed = player.SPEED * 0.5f; // Slow down by 50%
                Debug.Log("Player entered cobweb: slowed speed.");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<Player>();
            if (player != null)
            {
                player.speed = player.SPEED; // Restore speed
                Debug.Log("Player left cobweb: restored speed.");
            }
        }
    }
}

public class TurretShooter : MonoBehaviour
{
    public float fireInterval = 2f;
    private float fireTimer = 0f;
    private Player player;

    private void Start()
    {
        player = GameManager.Instance.player;
        fireTimer = Random.Range(0f, fireInterval); // Stagger shots
    }

    private void Update()
    {
        if (player == null || !player.isLive) return;

        fireTimer += Time.deltaTime;
        if (fireTimer >= fireInterval)
        {
            fireTimer = 0f;
            ShootAtPlayer();
        }
    }

    private void ShootAtPlayer()
    {
        Vector2 direction = (player.transform.position - transform.position).normalized;

        GameObject bullet = new GameObject("TurretProjectile");
        bullet.transform.position = transform.position;

        var sr = bullet.AddComponent<SpriteRenderer>();
        sr.color = Color.red;
        sr.sortingOrder = 4;
        bullet.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        var col = bullet.AddComponent<CircleCollider2D>();
        col.radius = 0.2f;
        col.isTrigger = true;

        var move = bullet.AddComponent<TurretBulletMove>();
        move.direction = direction;
        move.speed = 4f;
    }
}

public class TurretBulletMove : MonoBehaviour
{
    public Vector2 direction;
    public float speed;
    private float lifeTime = 4f;

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);

        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<Player>();
            if (player != null)
            {
                player.BeAttacked(1, direction);
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}

public class CobwebFinishTrigger : MonoBehaviour
{
    public CobwebDodgeChallenge challengeRoom;

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
