using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int MaxHP = 100;
    int HP;

    private void Start()
    {
        HP = MaxHP;
    }

    public void TakeDamage(int Damage)
    {
        HP -= Damage;

        if (HP <= 0)
        {
            Transform yourmom = transform.parent;
            GuardEnemy gh = yourmom.GetComponent<GuardEnemy>();
            if (gh != null) gh.Alert();
            Destroy(yourmom.gameObject);
            Destroy(gameObject);
        }
    }
}
