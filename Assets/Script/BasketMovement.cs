using UnityEngine;

public class BasketMovement : MonoBehaviour
{
    public enum MoveType
    {
        Static,
        Horizontal,
        Vertical
    }

    public MoveType moveType = MoveType.Static;
    public float speed = 2f;
    public float range = 2f;

    private Vector2 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * range;

        switch (moveType)
        {
            case MoveType.Horizontal:
                transform.position = new Vector2(startPos.x + offset, startPos.y);
                break;

            case MoveType.Vertical:
                transform.position = new Vector2(startPos.x, startPos.y + offset);
                break;

            case MoveType.Static:
                break;
        }
    }
}