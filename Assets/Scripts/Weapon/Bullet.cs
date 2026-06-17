using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed;
    public float damage;
    public float knockback;
    public GameObject explosionPrefab; //传入爆炸特效的预制体
    new private Rigidbody2D rigidbody; //传入刚体
    private bool _isPlayerFlag;
    public bool isPlayerFlag
    {
        get { return _isPlayerFlag; }
        set
        {
            _isPlayerFlag = value;
            if (_isPlayerFlag)
            {
                if (AdaptiveDifficultyManager.Instance != null)
                {
                    AdaptiveDifficultyManager.Instance.LogPlayerShoot();
                }
            }
        }
    }
    void OnEnable()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        if (!isPlayerFlag)
        {
            gameObject.layer = 22;
            damage = 0.5f;
            knockback = 0.3f;
            speed = 5;

        }
        else
        {
            gameObject.layer = 17;
            speed = 15;
            Player player = GameManager.Instance != null ? GameManager.Instance.player : null;
            if (player != null && player.isTemporarySuperWeapon)
            {
                damage = 5f;
                transform.localScale = Vector3.one * 1.8f;
                GetComponent<SpriteRenderer>().color = new Color(1f, 0.7f, 0f, 1f);
            }
            else
            {
                damage = 1f;
                transform.localScale = Vector3.one;
                GetComponent<SpriteRenderer>().color = Color.white;
            }
        }
    }

    public void SetSpeed(Vector2 direction)
    {
        if (!isPlayerFlag)
        {
            speed = 5;
            gameObject.layer = 22;
            transform.GetComponent<SpriteRenderer>().color = Color.cyan;
        }
        else
        {
            speed = 15;
            gameObject.layer = 17;
        }
        direction.Normalize();
        rigidbody.velocity = direction * speed;
    }

    void Update()
    {

        }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        GameObject exp = ObjectPool.Instance.GetObject(explosionPrefab); //生成一个爆炸特效预制体
        exp.transform.position = transform.position;
        if (!isPlayerFlag && other.tag == "Player")
        {
            other.GetComponent<Player>().BeAttacked(damage, rigidbody.velocity / speed, knockback);
        }
        if (isPlayerFlag && other.tag == "Monster")
        {
            if (AdaptiveDifficultyManager.Instance != null)
            {
                AdaptiveDifficultyManager.Instance.LogPlayerHit();
            }
            other.GetComponent<Monster>().BeAttacked(damage, rigidbody.velocity / speed, knockback);
        }
        // Destroy(gameObject);
        ObjectPool.Instance.PushObject(gameObject);
    }
}
