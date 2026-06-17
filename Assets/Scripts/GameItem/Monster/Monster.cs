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
    private Text hpText;

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

        // Scale down HP globally to 15% of the original value for very small HP numbers
        float hpScale = 0.15f;
        bool isBoss = name.Contains("Boss") || name.Contains("SBsuizhiyuan");
        
        HP = HP * totalMultiplier * hpScale;

        if (isBoss)
        {
            HP = Mathf.Clamp(HP, 15f, 35f);
        }
        else
        {
            HP = Mathf.Clamp(HP, 2f, 8f);
        }
        HP = Mathf.Round(HP);

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

        if (hpText == null)
        {
            GameObject textObj = new GameObject("HP_Text");
            textObj.transform.SetParent(hpSlider.transform, false);

            Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            hpText = textObj.AddComponent<Text>();
            hpText.font = defaultFont;
            hpText.fontSize = 5;
            hpText.color = Color.white;
            hpText.alignment = TextAnchor.MiddleCenter;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 4f); // Position slightly above the bar
            rect.sizeDelta = new Vector2(80f, 15f);

            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(0.5f, -0.5f);
        }
        hpText.text = Mathf.Max(0, Mathf.RoundToInt(HP)) + " / " + Mathf.Max(1, Mathf.RoundToInt(maxHP));
    }

    protected virtual void UpdateHPSlider()
    {
        hpSlider.value = Mathf.Lerp(hpSlider.value, HP / maxHP, Time.deltaTime * 5);
        if (hpText != null)
        {
            hpText.text = Mathf.Max(0, Mathf.RoundToInt(HP)) + " / " + Mathf.Max(1, Mathf.RoundToInt(maxHP));
        }
    }
}