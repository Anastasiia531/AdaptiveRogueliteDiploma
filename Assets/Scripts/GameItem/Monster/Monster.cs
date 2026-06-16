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

        HP = Mathf.Max(1f, Mathf.Round(HP * totalMultiplier));

        // Use reflection to scale speed field if it exists in the subclass
        System.Reflection.FieldInfo speedField = GetType().GetField("speed");
        if (speedField != null)
        {
            float curSpeed = (float)speedField.GetValue(this);
            speedField.SetValue(this, curSpeed * (0.8f + AdaptiveDifficultyManager.Instance.SkillIndex * 0.4f));
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