using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    [SerializeField] float speed = 15f;
    [SerializeField] float lifetime = 5f;// how long afer it dissappears.

    float damage;
    bool initialized;

    public void Initialize(float fireballDamage)
    {
        damage = fireballDamage;
        initialized = true;

        Destroy(gameObject, lifetime);
    }

    void Start()
    {
        if (!initialized)
        {
            Debug.LogWarning(
                "FireballProjectile was spawned without Initialize()."
            );
        }

        // shoot the fireball forward
        GetComponent<Rigidbody>().velocity =
            transform.forward * speed;
    }


    // deal damage to enemy
    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.root == transform.root)
            return;

        EnemyCombat enemy =
            collision.collider.GetComponentInParent<EnemyCombat>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
