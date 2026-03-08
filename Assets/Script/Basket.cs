using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Basket : MonoBehaviour
{
    /*
     * Basket interaction flow:
     * - enteredBasket=false: egg can lock via EnterBasket.
     * - enteredBasket=true: basket ignores egg colliders (egg falls through).
     * - enteredBasket is controlled by Egg (set on jump, reset on death).
     */
    public bool enteredBasket = false;
    [SerializeField] private float lockDistance = 0.6f;
    private readonly HashSet<Collider2D> enteredEggColliders = new HashSet<Collider2D>();
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    void OnTriggerEnter2D(Collider2D other)
    {
        TryLockEgg(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryLockEgg(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Egg"))
        {
            return;
        }

        enteredEggColliders.Remove(other);
    }

    void TryLockEgg(Collider2D other)
    {
        if (!other.CompareTag("Egg"))
        {
            return;
        }

        GameObject eggObject = other.gameObject;
        Egg egg = eggObject.GetComponent<Egg>();
        EggJump eggJump = eggObject.GetComponent<EggJump>();
        if (egg == null || eggJump == null)
        {
            return;
        }

        // Visited baskets are fully ignored by the egg.
        if (enteredBasket)
        {
            IgnoreEggCollider(other);
            return;
        }

        enteredEggColliders.Add(other);

        Vector3 basketCenterWorld = transform.TransformPoint(egg.BasketCenterLocalPosition);
        float distanceToCenter = Vector2.Distance(other.transform.position, basketCenterWorld);
        if (distanceToCenter > lockDistance)
        {
            return;
        }

        egg.EnterBasket(this.gameObject);
    }

    public void IgnoreEggCollider(Collider2D eggCollider)
    {
        if (eggCollider == null)
        {
            return;
        }

        Collider2D[] basketColliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < basketColliders.Length; i++)
        {
            Collider2D basketCollider = basketColliders[i];
            if (basketCollider == null || basketCollider == eggCollider)
            {
                continue;
            }

            Physics2D.IgnoreCollision(eggCollider, basketCollider, true);
        }

        enteredEggColliders.Remove(eggCollider);
    }

    public bool HasEnteredCollider(Collider2D eggCollider)
    {
        return eggCollider != null && enteredEggColliders.Contains(eggCollider);
    }
}
