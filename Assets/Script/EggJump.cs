using UnityEngine;

public class EggJump : MonoBehaviour
{
    public float jumpForce = 12f;
    public float basketCheckRadius = 2f;
    private Rigidbody2D rb;
    private CapsuleCollider2D eggCollider;
    private Egg egg;
    public bool waitingToReEnable = false;


    LayerMask basketLayer;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        eggCollider=GetComponent<CapsuleCollider2D>();
        egg = GetComponent<Egg>();
        basketLayer = LayerMask.GetMask("Basket");
    }

    void Update()
    {
        Basket currentBasket = GetBasketInRange();
        if (currentBasket == null && egg != null && egg.IsLockedInBasket)
        {
            currentBasket = egg.GetCurrentBasket();
        }

        bool canJumpFromBasket = egg != null && currentBasket != null && !waitingToReEnable;
        if (canJumpFromBasket && Input.GetMouseButtonDown(0))
        {
            if (egg != null)
            {
                egg.SaveBasketState(currentBasket.gameObject, transform.position);
                egg.OnJumpStarted();
            }
            jump();
        }

        if (waitingToReEnable && rb.velocity.y < -0.1f)
        {
            if (currentBasket == null)
            {
                eggCollider.isTrigger = false;
                waitingToReEnable = false;
                return;
            }

            if (currentBasket.HasEnteredCollider(eggCollider))
            {
                eggCollider.isTrigger = true;
            }
            else
            {
                eggCollider.isTrigger = false;
                waitingToReEnable = false;
            }
        }
    }


    void jump() {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        eggCollider.isTrigger = true;
        waitingToReEnable = true;


        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, basketCheckRadius);
    }

    Basket GetBasketInRange()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, basketCheckRadius, basketLayer);
        if (hit == null)
        {
            return null;
        }

        return hit.GetComponent<Basket>();
    }
}
