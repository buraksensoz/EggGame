using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Egg : MonoBehaviour
{
    /*
     * Egg/Basket state flow:
     * 1) EnterBasket: egg locks to basket center.
     * 2) OnJumpStarted: current basket is marked enteredBasket=true.
     * 3) HandleEndOfLifeHit: LastBasket is reset to enteredBasket=false before respawn.
     */
    public GameObject LastBasket = null;
    public Vector3 LastBasketLocalPosition = Vector3.zero;
    public Vector3 BasketCenterLocalPosition = Vector3.zero;

    private Rigidbody2D rb;
    private CapsuleCollider2D eggCollider;
    private EggJump eggJump;
    private float defaultGravityScale;

    private Transform lockedBasketTransform;
    private bool isLockedInBasket;
    private bool suppressShiftOnEnter;

    public bool IsLockedInBasket => isLockedInBasket;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        eggCollider = GetComponent<CapsuleCollider2D>();
        eggJump = GetComponent<EggJump>();

        if (rb != null)
        {
            defaultGravityScale = rb.gravityScale;
        }
    }

    void LateUpdate()
    {
        if (!isLockedInBasket || lockedBasketTransform == null)
        {
            return;
        }

        if (eggJump != null && eggJump.waitingToReEnable)
        {
            return;
        }

        SnapToBasketCenter();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleEndOfLifeHit(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null)
        {
            return;
        }

        Basket basket = collision.collider.GetComponentInParent<Basket>();
        if (basket != null && basket.enteredBasket)
        {
            basket.IgnoreEggCollider(eggCollider);
            return;
        }

        HandleEndOfLifeHit(collision.collider);
    }

    public void EnterBasket(GameObject basket)
    {
        if (basket == null)
        {
            return;
        }

        bool isNewLock = !isLockedInBasket || lockedBasketTransform != basket.transform;

        LastBasket = basket;
        LastBasketLocalPosition = BasketCenterLocalPosition;
        lockedBasketTransform = basket.transform;
        isLockedInBasket = true;
        Debug.Log($"[ENTER] basket={basket.name} centerLocal={BasketCenterLocalPosition} worldBeforeSnap={transform.position}", this);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.simulated = false;
        }

        if (eggCollider != null)
        {
            eggCollider.isTrigger = true;
        }

        if (eggJump != null)
        {
            eggJump.waitingToReEnable = false;
        }

        SnapToBasketCenter();

        if (isNewLock && !suppressShiftOnEnter)
        {
            GamePlay gamePlay = GamePlay.Instance;
            if (gamePlay == null)
            {
                gamePlay = FindObjectOfType<GamePlay>();
            }

            if (gamePlay != null)
            {
                gamePlay.RequestShift();
            }
        }

    }

    public void OnJumpStarted()
    {
        Basket currentBasket = GetCurrentBasket();
        if (currentBasket != null)
        {
            currentBasket.enteredBasket = true;
        }

        isLockedInBasket = false;
        lockedBasketTransform = null;

        if (rb != null)
        {
            rb.simulated = true;
            rb.gravityScale = defaultGravityScale;
        }
    }

    public void SaveBasketState(GameObject basket, Vector3 eggWorldPosition)
    {
        LastBasket = basket;
        if (basket != null)
        {
            LastBasketLocalPosition = BasketCenterLocalPosition;

            Debug.Log($"[SAVE] basket={basket.name} local={LastBasketLocalPosition} world={eggWorldPosition}", this);
        }
    }

    public Basket GetCurrentBasket()
    {
        if (lockedBasketTransform != null)
        {
            return lockedBasketTransform.GetComponent<Basket>();
        }

        if (LastBasket != null)
        {
            return LastBasket.GetComponent<Basket>();
        }

        return null;
    }

    bool IsEndOfLifeCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag("EndOfLife"))
        {
            return true;
        }

        Transform t = other.transform;
        while (t != null)
        {
            if (t.name == "EndOfLife")
            {
                return true;
            }

            t = t.parent;
        }

        return false;
    }

    void HandleEndOfLifeHit(Collider2D other)
    {
        if (!IsEndOfLifeCollider(other) || LastBasket == null)
        {
            return;
        }

        Basket lastBasketComponent = LastBasket.GetComponent<Basket>();
        if (lastBasketComponent != null)
        {
            lastBasketComponent.enteredBasket = false;
        }

        Vector3 respawnPosition = LastBasket.transform.TransformPoint(LastBasketLocalPosition);
        Vector3 recalculatedLocal = LastBasket.transform.InverseTransformPoint(respawnPosition);
        Debug.Log($"[RESPAWN] basket={LastBasket.name} savedLocal={LastBasketLocalPosition} respawnWorld={respawnPosition} recalculatedLocal={recalculatedLocal}", this);

        if (rb != null)
        {
            rb.simulated = true;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = respawnPosition;
            transform.position = respawnPosition;
        }
        else
        {
            transform.position = respawnPosition;
        }

        if (eggCollider != null)
        {
            eggCollider.isTrigger = false;
        }

        if (eggJump != null)
        {
            eggJump.waitingToReEnable = false;
        }

        suppressShiftOnEnter = true;
        EnterBasket(LastBasket);
        suppressShiftOnEnter = false;
    }

    void SnapToBasketCenter()
    {
        Vector3 targetPosition = lockedBasketTransform.TransformPoint(BasketCenterLocalPosition);
        
        // Only snap if there's a noticeable drift (position mismatch)
        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance < 0.001f)
        {
            return; // Already at target, no need to snap
        }

        Debug.Log($"[SNAP] basket={lockedBasketTransform.name} targetWorld={targetPosition} currentWorld={transform.position}", this);

        transform.position = targetPosition;

        LastBasket = lockedBasketTransform.gameObject;
        LastBasketLocalPosition = BasketCenterLocalPosition;
    }

    void MarkBasketAsEntered(GameObject basketObject)
    {
        if (basketObject == null)
        {
            return;
        }

        Basket basket = basketObject.GetComponent<Basket>();
        if (basket != null)
        {
            basket.enteredBasket = true;
        }
    }
}
