using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlay : MonoBehaviour
{
    public static GamePlay Instance { get; private set; }
    [System.Serializable]
    public class BasketConfig
    {
        public float startXPosition;
        public BasketMovement.MoveType moveType;
        public float speed;
        public float range;
    }

    public BasketConfig[] basketList;

    public GameObject basketPrefab;
    public Transform basketSpawnParent;
    public Transform endOfLifeTransform;
    [Range(0f, 0.45f)] public float horizontalViewportMargin = 0.1f;
    public float endOfLifeViewportY = -0.05f;
    public bool autoConfigureCamera = true;
    public float cameraOrthographicSize = 10f;
    public float cameraCenterX = 0f;
    public float cameraCenterY = 0f;
    [SerializeField] private float basketShiftDuration = 0.25f;
    [SerializeField] private float offscreenViewportY = -0.2f;

    private readonly float[] visibleViewportY = { 0.25f, 0.5f, 0.75f };
    private readonly List<GameObject> spawnedBaskets = new List<GameObject>();
    private int nextBasketConfigIndex;
    private bool isShiftingBaskets;
    private bool hasSkippedInitialLockShift;

    void Awake()
    {
        Instance = this;
        // Always use the list defined in code at runtime (ignore Inspector serialized values).
        basketList = CreateDefaultBasketList();
    }

    BasketConfig[] CreateDefaultBasketList()
    {
        return new BasketConfig[]
        {
            new BasketConfig { startXPosition = -0f, moveType = BasketMovement.MoveType.Static, speed = 0f, range = 0f },
            new BasketConfig { startXPosition = -1f, moveType = BasketMovement.MoveType.Horizontal, speed = 0.7f, range = 12f },
            new BasketConfig { startXPosition = -2f, moveType = BasketMovement.MoveType.Static, speed = 0f, range = 1.2f },
            new BasketConfig { startXPosition = -0f, moveType = BasketMovement.MoveType.Horizontal, speed = 0.7f, range = 25f },
            new BasketConfig { startXPosition = -0f, moveType = BasketMovement.MoveType.Static, speed = 0f, range = 0f },
            new BasketConfig { startXPosition = -0f, moveType = BasketMovement.MoveType.Static, speed = 0f, range = 1.6f },
            new BasketConfig { startXPosition = -0f, moveType = BasketMovement.MoveType.Static, speed = 0f, range = 2.8f },
            new BasketConfig { startXPosition = -0f, moveType = BasketMovement.MoveType.Static, speed = 0f, range = 0f },
            new BasketConfig { startXPosition = -0f, moveType = BasketMovement.MoveType.Static, speed = 0f, range = 2f },
            new BasketConfig { startXPosition = -0f, moveType = BasketMovement.MoveType.Static, speed = 0f, range = 1.8f }
        };
    }

    void Start()
    {
        ConfigureMainCamera();

        if (basketPrefab != null && basketPrefab.GetComponent<RectTransform>() != null)
        {
            Debug.LogWarning("Basket prefab UI (RectTransform) gorunuyor. World-space spawn icin Transform + SpriteRenderer kullan.", this);
        }

        if (basketSpawnParent != null && basketSpawnParent.GetComponentInParent<Canvas>() != null)
        {
            Debug.LogWarning("basketSpawnParent Canvas altinda. World-space icin root altinda bir parent kullan.", this);
        }

        PositionEndOfLife();
        ClearExistingBasketsInScene();
        SpawnInitialBaskets();
    }

    void ConfigureMainCamera()
    {
        if (!autoConfigureCamera || Camera.main == null)
        {
            return;
        }

        Camera cam = Camera.main;
        if (!cam.orthographic)
        {
            Debug.LogWarning("Main Camera perspective modda. Bu script orthographic kamera bekliyor.", this);
            return;
        }

        cam.orthographicSize = cameraOrthographicSize;
        Vector3 camPos = cam.transform.position;
        camPos.x = cameraCenterX;
        camPos.y = cameraCenterY;
        cam.transform.position = camPos;
    }

    void LateUpdate()
    {
        MaintainMainCameraLock();
    }

    void MaintainMainCameraLock()
    {
        if (!autoConfigureCamera || Camera.main == null)
        {
            return;
        }

        Camera cam = Camera.main;
        if (!cam.orthographic)
        {
            return;
        }

        if (!Mathf.Approximately(cam.orthographicSize, cameraOrthographicSize))
        {
            cam.orthographicSize = cameraOrthographicSize;
        }

        Vector3 camPos = cam.transform.position;
        if (!Mathf.Approximately(camPos.x, cameraCenterX) || !Mathf.Approximately(camPos.y, cameraCenterY))
        {
            camPos.x = cameraCenterX;
            camPos.y = cameraCenterY;
            cam.transform.position = camPos;
        }
    }

    void PositionEndOfLife()
    {
        if (endOfLifeTransform == null || Camera.main == null)
        {
            return;
        }

        float depth = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 worldPoint = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, endOfLifeViewportY, depth));
        endOfLifeTransform.position = new Vector3(0f, worldPoint.y, 0f);
    }

    void SpawnInitialBaskets()
    {
        if (basketPrefab == null || Camera.main == null || basketList == null || basketList.Length == 0)
        {
            return;
        }

        int spawnCount = Mathf.Min(3, basketList.Length);
        float depth = Mathf.Abs(Camera.main.transform.position.z);
        float centerReferenceX = basketList[0].startXPosition;
        float maxAbsX = GetMaxAbsRelativeX(centerReferenceX);

        for (int i = 0; i < spawnCount; i++)
        {
            BasketConfig config = basketList[i];
            float relativeX = config.startXPosition - centerReferenceX;
            float viewportX = NormalizeXToViewport(relativeX, maxAbsX);
            float viewportY = visibleViewportY[i];
            Vector3 spawnPosition = Camera.main.ViewportToWorldPoint(new Vector3(viewportX, viewportY, depth));
            spawnPosition.z = 0f;

            GameObject basketObject = Instantiate(basketPrefab, spawnPosition, Quaternion.identity, basketSpawnParent);

            BasketMovement movement = basketObject.GetComponent<BasketMovement>();
            if (movement != null)
            {
                movement.moveType = config.moveType;
                movement.speed = config.speed;
                movement.range = config.range;
                movement.SetAnchorPosition(spawnPosition);
            }

            spawnedBaskets.Add(basketObject);
        }

        nextBasketConfigIndex = spawnCount;
    }

    void ClearExistingBasketsInScene()
    {
        Basket[] existingBaskets = FindObjectsOfType<Basket>();
        for (int i = 0; i < existingBaskets.Length; i++)
        {
            Basket basket = existingBaskets[i];
            if (basket != null)
            {
                Destroy(basket.gameObject);
            }
        }

        spawnedBaskets.Clear();
    }

    float GetMaxAbsRelativeX(float centerReferenceX)
    {
        float maxAbs = 0f;

        for (int i = 0; i < basketList.Length; i++)
        {
            float absX = Mathf.Abs(basketList[i].startXPosition - centerReferenceX);
            if (absX > maxAbs)
            {
                maxAbs = absX;
            }
        }

        return maxAbs;
    }

    float NormalizeXToViewport(float x, float maxAbsX)
    {
        if (maxAbsX <= Mathf.Epsilon)
        {
            return 0.5f;
        }

        float normalized = Mathf.Clamp(x / maxAbsX, -1f, 1f);
        float halfWidth = 0.5f - horizontalViewportMargin;
        return 0.5f + normalized * halfWidth;
    }

    public void OnEggEnteredBasket(Basket basket)
    {
        RequestShift();
    }

    public void RequestShift()
    {
        if (!hasSkippedInitialLockShift)
        {
            hasSkippedInitialLockShift = true;
            return;
        }

        if (isShiftingBaskets || spawnedBaskets.Count < 3)
        {
            return;
        }

        isShiftingBaskets = true;
        StartCoroutine(ShiftBasketSequence());
    }

    IEnumerator ShiftBasketSequence()
    {
        if (basketList == null || basketList.Length == 0 || Camera.main == null)
        {
            isShiftingBaskets = false;
            yield break;
        }

        float depth = Mathf.Abs(Camera.main.transform.position.z);
        float centerReferenceX = basketList[0].startXPosition;
        float maxAbsX = GetMaxAbsRelativeX(centerReferenceX);
        BasketConfig nextConfig = basketList[nextBasketConfigIndex % basketList.Length];
        nextBasketConfigIndex++;

        float relativeX = nextConfig.startXPosition - centerReferenceX;
        float viewportX = NormalizeXToViewport(relativeX, maxAbsX);

        float bottomViewportY = visibleViewportY[0];
        float middleViewportY = visibleViewportY[1];
        float topViewportY = visibleViewportY[2];
        float slotStep = topViewportY - middleViewportY;
        float spawnViewportY = topViewportY + slotStep;

        Vector3 spawnPosition = Camera.main.ViewportToWorldPoint(new Vector3(viewportX, spawnViewportY, depth));
        spawnPosition.z = 0f;

        GameObject newBasket = Instantiate(basketPrefab, spawnPosition, Quaternion.identity, basketSpawnParent);
        BasketMovement newMovement = newBasket.GetComponent<BasketMovement>();
        if (newMovement != null)
        {
            newMovement.moveType = nextConfig.moveType;
            newMovement.speed = nextConfig.speed;
            newMovement.range = nextConfig.range;
            newMovement.SetAnchorPosition(spawnPosition);
            newMovement.enabled = false;
        }

        spawnedBaskets.Add(newBasket);

        List<GameObject> currentBaskets = new List<GameObject>(spawnedBaskets);
        List<Vector3> fromPositions = new List<Vector3>(currentBaskets.Count);
        List<Vector3> toPositions = new List<Vector3>(currentBaskets.Count);
        List<BasketMovement> movementComponents = new List<BasketMovement>(currentBaskets.Count);
        float[] targetViewportY = { offscreenViewportY, bottomViewportY, middleViewportY, topViewportY };

        for (int i = 0; i < currentBaskets.Count && i < targetViewportY.Length; i++)
        {
            GameObject basketObject = currentBaskets[i];
            Vector3 fromPosition = basketObject != null ? basketObject.transform.position : Vector3.zero;
            fromPositions.Add(fromPosition);

            BasketMovement movement = basketObject != null ? basketObject.GetComponent<BasketMovement>() : null;
            movementComponents.Add(movement);

            Vector3 xReferenceWorldPosition = basketObject != null ? basketObject.transform.position : Vector3.zero;
            if (movement != null)
            {
                Vector2 anchor = movement.GetAnchorPosition();
                xReferenceWorldPosition = new Vector3(anchor.x, anchor.y, xReferenceWorldPosition.z);
            }

            Vector3 viewportPoint = basketObject != null
                ? Camera.main.WorldToViewportPoint(xReferenceWorldPosition)
                : new Vector3(0.5f, targetViewportY[i], depth);
            Vector3 targetPosition = Camera.main.ViewportToWorldPoint(new Vector3(viewportPoint.x, targetViewportY[i], depth));
            targetPosition.z = 0f;
            toPositions.Add(targetPosition);

            if (movement != null)
            {
                movement.enabled = false;
            }
        }

        float elapsed = 0f;
        while (elapsed < basketShiftDuration)
        {
            elapsed += Time.deltaTime;
            float t = basketShiftDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / basketShiftDuration);

            for (int i = 0; i < currentBaskets.Count && i < fromPositions.Count && i < toPositions.Count; i++)
            {
                GameObject basketObject = currentBaskets[i];
                if (basketObject == null)
                {
                    continue;
                }

                basketObject.transform.position = Vector3.Lerp(fromPositions[i], toPositions[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < currentBaskets.Count && i < toPositions.Count; i++)
        {
            GameObject basketObject = currentBaskets[i];
            if (basketObject == null)
            {
                continue;
            }

            basketObject.transform.position = toPositions[i];
            BasketMovement movement = movementComponents[i];
            if (movement != null)
            {
                movement.SetAnchorPosition(toPositions[i]);
                movement.enabled = true;
            }
        }

        if (spawnedBaskets.Count > 3)
        {
            GameObject bottomBasket = spawnedBaskets[0];
            spawnedBaskets.RemoveAt(0);
            if (bottomBasket != null)
            {
                Destroy(bottomBasket);
            }
        }

        isShiftingBaskets = false;
    }

}
