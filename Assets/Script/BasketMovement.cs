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
    private Vector2 cachedHalfExtents;
    private bool hasCachedHalfExtents;
    private bool isPausedByShift;
    private float movementTime;

    void Start()
    {
        startPos = transform.position;
        RefreshCachedHalfExtents();
    }

    void OnEnable()
    {
        GamePlay.ShiftStateChanged += OnShiftStateChanged;

        GamePlay gamePlay = GamePlay.Instance;
        if (gamePlay != null)
        {
            isPausedByShift = gamePlay.IsShiftingBaskets;
        }
    }

    void OnDisable()
    {
        GamePlay.ShiftStateChanged -= OnShiftStateChanged;
    }

    void OnShiftStateChanged(bool isShifting)
    {
        isPausedByShift = isShifting;
    }

    void Update()
    {
        if (isPausedByShift)
        {
            return;
        }

        float clampedRange = GetClampedRangeInsideCamera();
        movementTime += Time.deltaTime * Mathf.Max(0f, speed);
        float offset = Mathf.Sin(movementTime) * clampedRange;

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

    public void SetAnchorPosition(Vector2 anchorPosition)
    {
        startPos = anchorPosition;
        transform.position = anchorPosition;
        movementTime = 0f;
    }

    public Vector2 GetAnchorPosition()
    {
        return startPos;
    }

    float GetClampedRangeInsideCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
        {
            return Mathf.Max(0f, range);
        }

        if (!hasCachedHalfExtents)
        {
            RefreshCachedHalfExtents();
        }

        float halfWidth = cachedHalfExtents.x;
        float halfHeight = cachedHalfExtents.y;

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;

        float leftLimit = camPos.x - camHalfWidth + halfWidth;
        float rightLimit = camPos.x + camHalfWidth - halfWidth;
        float bottomLimit = camPos.y - camHalfHeight + halfHeight;
        float topLimit = camPos.y + camHalfHeight - halfHeight;

        if (moveType == MoveType.Horizontal)
        {
            float leftDistance = startPos.x - leftLimit;
            float rightDistance = rightLimit - startPos.x;
            float maxSymmetricRange = Mathf.Max(0f, Mathf.Min(leftDistance, rightDistance));
            return Mathf.Min(Mathf.Max(0f, range), maxSymmetricRange);
        }

        if (moveType == MoveType.Vertical)
        {
            float downDistance = startPos.y - bottomLimit;
            float upDistance = topLimit - startPos.y;
            float maxSymmetricRange = Mathf.Max(0f, Mathf.Min(downDistance, upDistance));
            return Mathf.Min(Mathf.Max(0f, range), maxSymmetricRange);
        }

        return 0f;
    }

    void RefreshCachedHalfExtents()
    {
        Bounds basketBounds = GetBasketBounds();
        cachedHalfExtents = basketBounds.extents;
        hasCachedHalfExtents = true;
    }

    Bounds GetBasketBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            return bounds;
        }

        return new Bounds(transform.position, Vector3.zero);
    }
}
