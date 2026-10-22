using UnityEngine;

public class Wander : MonoBehaviour
{
    //How fast animal is moving, with the radious size that it should never leave
    public float moveSpeed = 1.5f;
    public float wanderRadius = 6f;

    //where it starts
    Vector3 home;

    //the target the animal is moving towards, it must be within the radious. 
    Vector3 target;

    void Start()
    {
        //the animal start here
        home = transform.position;
        target = home;
    }

    void Update()
    {
        Vector3 toTarget = target - transform.position;   // the arrow from aniaml to the target

        if (toTarget.magnitude < 0.2f)   //Close enough? arrived!
        {
            Vector2 r = Random.insideUnitCircle * wanderRadius;   // a random spot inside a circle
            target = home + new Vector3(r.x, 0f, r.y);            // that is the new target
            return;
        }
        //Moves by changing the position directly, so the animal ignores walls and stays at its start height.
        transform.position += toTarget.normalized * moveSpeed * Time.deltaTime;   // take one small step
        transform.rotation = Quaternion.LookRotation(toTarget);                   // face the way it walks
    }
}