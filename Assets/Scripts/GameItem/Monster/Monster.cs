using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Monster : GameItem, IsAttackable
{
    public Slider hpSlider;

    public override GameItemType gameItemType { get { return GameItemType.Monster; } }
    public GameObject[] guns = new GameObject[5];
    protected bool isLive = true;
    public float HP;
    protected float maxHP;
    protected float beKnockBackSeconds;
    protected float beKnockBackLength;

    protected override void Awake()
    {
        base.Awake();
        ApplyDDAScaling();
        ActiveHPSlider();
    }

    private void ApplyDDAScaling()
    {
        if (AdaptiveDifficultyManager.Instance == null) return;

        // DDA scales stats from 0.7x (lowest skill) to 1.3x (highest skill)
        float ddaMult = 0.7f + AdaptiveDifficultyManager.Instance.SkillIndex * 0.6f;
        int currentDepth = (GameManager.Instance != null) ? GameManager.Instance.depth : 1;
        // Floor depth scales stats by +15% per level
        float depthMult = 1f + (currentDepth - 1) * 0.15f;

        float totalMultiplier = ddaMult * depthMult;

        bool isBoss = name.Contains("Boss") || name.Contains("SBsuizhiyuan") || name.Contains("boss");
        bool isFlyMinion = name.Contains("children");
        bool isUndergrad = name.Contains("underGraduate");
        
        // Scale HP based on Isaac metrics
        float hpScale = 1.0f;
        if (isBoss) hpScale = 1.6f;
        else if (isFlyMinion) hpScale = 0.35f;
        else if (isUndergrad) hpScale = 0.9f;
        else hpScale = 0.6f; // teenager/standard
        
        HP = HP * totalMultiplier * hpScale;

        if (isBoss)
        {
            HP = Mathf.Clamp(HP, 120f, 240f);
        }
        else if (isFlyMinion)
        {
            HP = Mathf.Clamp(HP, 3f, 6f);
        }
        else if (isUndergrad)
        {
            HP = Mathf.Clamp(HP, 15f, 28f);
        }
        else
        {
            HP = Mathf.Clamp(HP, 10f, 18f);
        }
        HP = Mathf.Round(HP);

        // Use reflection to scale speed field if it exists in the subclass (bosses are faster)
        System.Reflection.FieldInfo speedField = GetType().GetField("speed");
        if (speedField != null)
        {
            float curSpeed = (float)speedField.GetValue(this);
            float speedMult = isBoss ? 1.3f : 1.0f;
            speedField.SetValue(this, curSpeed * speedMult * (0.8f + AdaptiveDifficultyManager.Instance.SkillIndex * 0.4f));
        }

        // Use reflection to scale damage field if it exists in the subclass (bosses deal double damage)
        System.Reflection.FieldInfo damageField = GetType().GetField("damage");
        if (damageField != null)
        {
            float curDamage = (float)damageField.GetValue(this);
            float dmgMult = isBoss ? 2.0f : 1.25f;
            float scaledDamage = curDamage * dmgMult * (0.8f + AdaptiveDifficultyManager.Instance.SkillIndex * 0.4f);
            damageField.SetValue(this, Mathf.Max(1f, Mathf.Round(scaledDamage)));
        }
    }
    protected virtual void Update()
    {
        UpdateHPSlider();
    }
    public virtual void BeAttacked(float damage, Vector2 direction, float forceMultiple = 1f)
    {
        if (!isLive) { return; }
        HP -= damage;
        direction.Normalize();
        if (HP <= 0)
        {
            hpSlider.gameObject.SetActive(false);
            StartCoroutine(Death());
        }
        else { StartCoroutine(knockBackCoroutine(direction * forceMultiple)); }
    }

    protected virtual IEnumerator Death()
    {

        isLive = false;
        transform.GetComponent<Collider2D>().enabled = false;
        transform.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        yield return null;
        Destroy(gameObject);
    }
    protected IEnumerator knockBackCoroutine(Vector2 force)
    {
        float timeleft = beKnockBackSeconds;
        while (timeleft > 0)
        {
            transform.Translate(force * beKnockBackLength * Time.deltaTime / beKnockBackSeconds);
            timeleft -= Time.deltaTime;
            yield return null;
        }
    }
    protected void ActiveHPSlider()
    {
        maxHP = HP;
        hpSlider.gameObject.SetActive(true);
        hpSlider.value = 1;
    }

    protected virtual void UpdateHPSlider()
    {
        hpSlider.value = Mathf.Lerp(hpSlider.value, HP / maxHP, Time.deltaTime * 5);
    }
}