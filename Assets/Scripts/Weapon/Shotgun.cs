using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : Gun
{
    private int num;
    public float bulletAngle = 15;

    protected override void OnEnable()
    {
        base.OnEnable();
        num = NUM + 2;
    }
    protected override void Fire()
    {
        animator.SetTrigger("Shoot");

        float angleToUse = bulletAngle;
        if (!isPlayer && AdaptiveDifficultyManager.Instance != null)
        {
            // Easy: wider angle (25f), Hard: tighter angle (8f)
            angleToUse = Mathf.Lerp(25f, 8f, AdaptiveDifficultyManager.Instance.SkillIndex);
        }

        int median = num / 2;
        for (int i = 0; i < num; i++)
        {
            GameObject bullet = ObjectPool.Instance.GetObject(bulletPrefab);
            bullet.transform.position = muzzlePos.position;

            bullet.GetComponent<Bullet>().isPlayerFlag = isPlayer;
            if (num % 2 == 1)
            {
                bullet.GetComponent<Bullet>().SetSpeed(Quaternion.AngleAxis(angleToUse * (i - median), Vector3.forward) * direction);
            }
            else
            {
                bullet.GetComponent<Bullet>().SetSpeed(Quaternion.AngleAxis(angleToUse * (i - median) + angleToUse / 2, Vector3.forward) * direction);
            }
        }


        for (int i = 0; i < num; i++)
        {
            GameObject shell = ObjectPool.Instance.GetObject(shellPrefab);
            shell.transform.position = shellPos.position;
            shell.transform.rotation = shellPos.rotation;
        }
    }
}
