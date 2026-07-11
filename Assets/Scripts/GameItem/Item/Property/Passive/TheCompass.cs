using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheCompass : Property
{
    protected override void SetID()
    {
        ID = 22;
    }

    protected override void Effect()
    {
        if (UI != null && UI.miniMap != null)
        {
            UI.miniMap.ShowAllMinMap();
        }
    }
}
