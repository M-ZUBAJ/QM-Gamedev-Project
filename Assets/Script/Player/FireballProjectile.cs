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


        // check if fireball hit the enemy 
        EnemyCombat enemy =
            collision.collider.GetComponentInParent<EnemyCombat>();

        // check if fireball hit animal or other resource 
        ResourceNode resource = collision.collider.GetComponentInParent<ResourceNode>();


        // if fireball hit an enemy, make it take damage
        if (enemy != null)
        {
            // [Tony] replaced (21/11/2026): enemy.TakeDamage(damage);
            // The same hit, through Tony's PerkEffects: with the Vampire perk the player heals a share of the damage.
            PerkEffects.Hurt(enemy, () => enemy.TakeDamage(damage));             // [Tony] replaced
        }
        // otherwise if it hit a resource/animal, make that take damage instead.
        else if (resource != null)
        {
            // [Tony] added (22/11/2026): a rock or tree that needs a tool (ores: a good enough pickaxe; the snow's spruce:
            // an Iron Axe or better) isn't broken by a fireball either, like a sword can't break it. It says which tool
            // it needs. To undo: delete the 6 lines marked [Tony] added below.
            // Log: Docs/TonyEditMate/Marko/22-11-2026 Fireball Leaves Ores And Spruce Alone.md
            if (ToolRules.NeededFor(resource) != ToolTier.None)                  // [Tony] added
            {                                                                    // [Tony] added
                ToolRules.TellCantMine(resource);                                // [Tony] added: "Needs an Iron Axe or better"
                Destroy(gameObject);                                             // [Tony] added
                return;                                                          // [Tony] added
            }                                                                    // [Tony] added
            // [Tony] replaced (21/11/2026): resource.TakeDamage(damage);
            PerkEffects.Hurt(resource, () => resource.TakeDamage(damage));       // [Tony] replaced (Vampire: animals only)
        }

        Destroy(gameObject);
    }
}
