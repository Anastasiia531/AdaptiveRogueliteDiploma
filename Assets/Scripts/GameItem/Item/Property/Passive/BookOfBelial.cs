using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BookOfBelial : Property
{
    protected override void SetID()
    {
        ID = 20;
    }

    protected override void Effect()
    {
        if (player != null)
        {
            player.StartCoroutine(ApplyTemporaryFireRateBoost());
        }
    }

    private IEnumerator ApplyTemporaryFireRateBoost()
    {
        // Double fire rate = half attack interval for all guns
        foreach (GameObject gun in player.guns)
        {
            Gun g = gun.GetComponent<Gun>();
            if (g != null) g.interval /= 2f;
        }

        yield return new WaitForSeconds(20f);

        if (player != null)
        {
            foreach (GameObject gun in player.guns)
            {
                Gun g = gun.GetComponent<Gun>();
                if (g != null) g.interval *= 2f;
            }
        }
    }
}
