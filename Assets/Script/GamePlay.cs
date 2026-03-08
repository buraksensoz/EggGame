using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlay : MonoBehaviour
{
    [System.Serializable]
    public class BasketConfig
    {
        public float startXPosition;
        public BasketMovement.MoveType moveType;
        public float speed;
        public float range;
    }

    public BasketConfig[] basketList = new BasketConfig[]
    {
        new BasketConfig { startXPosition = -0f, moveType = BasketMovement.MoveType.Static, speed = 0f, range = 0f },
        new BasketConfig { startXPosition = -6.5f, moveType = BasketMovement.MoveType.Horizontal, speed = 1.6f, range = 1.5f },
        new BasketConfig { startXPosition = -5f, moveType = BasketMovement.MoveType.Vertical, speed = 1.2f, range = 1.2f },
        new BasketConfig { startXPosition = -3.5f, moveType = BasketMovement.MoveType.Horizontal, speed = 2.1f, range = 2.2f },
        new BasketConfig { startXPosition = -2f, moveType = BasketMovement.MoveType.Static, speed = 0f, range = 0f },
        new BasketConfig { startXPosition = -0.5f, moveType = BasketMovement.MoveType.Vertical, speed = 1.8f, range = 1.6f },
        new BasketConfig { startXPosition = 1f, moveType = BasketMovement.MoveType.Horizontal, speed = 2.4f, range = 2.8f },
        new BasketConfig { startXPosition = 2.5f, moveType = BasketMovement.MoveType.Static, speed = 0f, range = 0f },
        new BasketConfig { startXPosition = 4f, moveType = BasketMovement.MoveType.Vertical, speed = 1.4f, range = 2f },
        new BasketConfig { startXPosition = 5.5f, moveType = BasketMovement.MoveType.Horizontal, speed = 2f, range = 1.8f }
    };

    public GameObject basketPrefab;
    public Transform basketSpawnParent;
    public Transform endOfLifeTransform;
    [Range(0f, 0.45f)] public float horizontalViewportMargin = 0.1f;
    public float endOfLifeViewportY = -0.05f;
    public bool autoConfigureCamera = true;
    public float cameraOrthographicSize = 10f;
    public float cameraCenterX = 0f;
    public float cameraCenterY = 0f;

    private readonly float[] initialViewportY = { 0.25f, 0.5f, 0.75f };
    private readonly List<GameObject> spawnedBaskets = new List<GameObject>();

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
            float viewportY = initialViewportY[i];
            Vector3 spawnPosition = Camera.main.ViewportToWorldPoint(new Vector3(viewportX, viewportY, depth));
            spawnPosition.z = 0f;

            GameObject basketObject = Instantiate(basketPrefab, spawnPosition, Quaternion.identity, basketSpawnParent);

            BasketMovement movement = basketObject.GetComponent<BasketMovement>();
            if (movement != null)
            {
                movement.moveType = config.moveType;
                movement.speed = config.speed;
                movement.range = config.range;
            }

            spawnedBaskets.Add(basketObject);
        }
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
}
