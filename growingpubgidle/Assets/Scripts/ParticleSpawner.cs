using UnityEngine;

/// <summary>
/// 간단한 파티클 스포너. JS createParticles() 대체.
/// </summary>
public class ParticleSpawner : MonoBehaviour
{
    public static ParticleSpawner Instance;

    [SerializeField] private GameObject particlePrefab;

    private void Awake()
    {
        Instance = this;
    }

    public static void Spawn(Vector3 position, Color color, int count)
    {
        if (Instance == null || Instance.particlePrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            GameObject p = Instantiate(Instance.particlePrefab, position, Quaternion.identity);
            var sr = p.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = color;

            var rb = p.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                rb.linearVelocity = dir * Random.Range(5f, 20f);
            }

            Destroy(p, Random.Range(0.3f, 0.8f));
        }
    }
}
