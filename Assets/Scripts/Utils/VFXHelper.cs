using System.Collections;
using UnityEngine;

public class VFXHelper : MonoBehaviour
{
    public static VFXHelper Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Shake the camera to register high-impact events like damage or explosions.
    /// </summary>
    public void CameraShake(float duration = 0.2f, float magnitude = 0.12f)
    {
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.position;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null;
        }

        // Restore camera position (z must remain -10f)
        if (cam != null)
        {
            cam.transform.position = new Vector3(originalPos.x, originalPos.y, -10f);
        }
    }

    /// <summary>
    /// Spawns physics-based spark particles that fly out and fade away.
    /// </summary>
    public void SpawnProceduralSparks(Vector3 position, Color color, int count = 8)
    {
        // Cache bullet/player sprite if possible to use as shape, else default to quad
        Sprite particleSprite = null;
        Player playerObj = GameManager.Instance != null ? GameManager.Instance.player : null;
        if (playerObj != null)
        {
            var sr = playerObj.Body.GetComponent<SpriteRenderer>();
            if (sr != null) particleSprite = sr.sprite;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject spark = new GameObject("SparkParticle");
            spark.transform.position = position;

            var sr = spark.AddComponent<SpriteRenderer>();
            if (particleSprite != null) sr.sprite = particleSprite;
            sr.color = color;
            sr.sortingOrder = 10;

            spark.transform.localScale = Vector3.one * 0.12f;

            var rb = spark.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.drag = 3.0f; // Rapidly slow down for dust friction effect

            Vector2 dir = Random.insideUnitCircle.normalized;
            rb.velocity = dir * Random.Range(3f, 5f);

            Destroy(spark, 0.4f);
            StartCoroutine(FadeOutRoutine(sr, 0.4f));
        }
    }

    private IEnumerator FadeOutRoutine(SpriteRenderer sr, float duration)
    {
        float elapsed = 0f;
        Color baseCol = sr.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                sr.color = Color.Lerp(baseCol, new Color(baseCol.r, baseCol.g, baseCol.b, 0f), elapsed / duration);
            }
            yield return null;
        }
    }
}
