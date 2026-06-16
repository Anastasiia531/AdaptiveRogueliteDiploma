using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatRoom : Room
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void GenerateRoomContent()
    {
        base.GenerateRoomContent();
        
        // Lock doors if there are enemies
        if (monsterContainer != null && monsterContainer.childCount > 0 && !isCleared)
        {
            LockDoors();
        }
    }

    public void LockDoors()
    {
        foreach (var door in activeDoorList)
        {
            if (door != null)
            {
                Transform col = door.transform.Find("collider");
                if (col != null)
                {
                    BoxCollider2D boxCol = col.GetComponent<BoxCollider2D>();
                    if (boxCol != null) boxCol.isTrigger = false;
                }
                
                Transform dTrans = door.transform.Find("Door");
                if (dTrans != null)
                {
                    Animator anim = dTrans.GetComponent<Animator>();
                    if (anim != null) anim.Play("DoorClose");
                }
            }
        }
    }
}
