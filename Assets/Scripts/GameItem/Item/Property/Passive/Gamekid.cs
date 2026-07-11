using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gamekid : Property
{
    protected override void SetID()
    {
        ID = 21;
    }

    protected override void Effect()
    {
        if (player != null)
        {
            player.StartCoroutine(ApplyInvincibility());
        }
    }

    private IEnumerator ApplyInvincibility()
    {
        player.isInvincible = true;
        
        SpriteRenderer pr = player.GetComponent<SpriteRenderer>();
        SpriteRenderer hr = player.Head != null ? player.Head.GetComponent<SpriteRenderer>() : null;

        float duration = 12f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (player == null) yield break;
            elapsed += Time.deltaTime;

            Color flashColor = new Color(0.2f, 1f, 0.2f, 0.7f); // green flash
            if (pr != null) pr.color = flashColor;
            if (hr != null) hr.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            
            if (pr != null) pr.color = Color.white;
            if (hr != null) hr.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }

        if (player != null)
        {
            player.isInvincible = false;
            if (pr != null) pr.color = Color.white;
            if (hr != null) hr.color = Color.white;
        }
    }
}
