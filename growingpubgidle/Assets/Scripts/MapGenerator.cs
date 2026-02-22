using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 맵 오브젝트(빌딩, 바위) 생성기.
/// JS MapObject 클래스 + 60개 랜덤 배치를 자동화.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance;

    [Header("Prefabs")]
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject groundPrefab; // 바닥 타일

    private List<GameObject> spawnedObjects = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    public void GenerateMap()
    {
        ClearMap();

        // 바닥 생성
        if (groundPrefab != null)
        {
            GameObject ground = Instantiate(groundPrefab,
                new Vector3(GameConfig.MAP_SIZE / 2f, GameConfig.MAP_SIZE / 2f, 1f),
                Quaternion.identity);
            ground.transform.localScale = new Vector3(GameConfig.MAP_SIZE, GameConfig.MAP_SIZE, 1f);
            spawnedObjects.Add(ground);
        }

        // 맵 경계 벽 생성
        CreateBorderWalls();

        // 장애물 60개
        for (int i = 0; i < GameConfig.MAP_OBJECT_COUNT; i++)
        {
            bool isBuilding = Random.value > 0.4f;
            GameObject prefab = isBuilding ? buildingPrefab : rockPrefab;
            if (prefab == null) continue;

            float x = Random.Range(10f, GameConfig.MAP_SIZE - 10f);
            float y = Random.Range(10f, GameConfig.MAP_SIZE - 10f);

            GameObject obj = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity);

            if (isBuilding)
            {
                float w = Random.Range(6f, 18f);
                float h = Random.Range(6f, 16f);
                obj.transform.localScale = new Vector3(w, h, 1f);

                // BoxCollider2D 자동 조정
                var box = obj.GetComponent<BoxCollider2D>();
                if (box == null) box = obj.AddComponent<BoxCollider2D>();
                // Collider size는 localScale이 적용되므로 1x1로 유지
            }
            else
            {
                float radius = Random.Range(3f, 9f);
                obj.transform.localScale = new Vector3(radius, radius, 1f);

                var circle = obj.GetComponent<CircleCollider2D>();
                if (circle == null) circle = obj.AddComponent<CircleCollider2D>();
            }

            // Obstacle 레이어 설정
            obj.layer = LayerMask.NameToLayer("Obstacle");

            spawnedObjects.Add(obj);
        }
    }

    private void CreateBorderWalls()
    {
        float size = GameConfig.MAP_SIZE;
        float thickness = 2f;

        // 하단
        CreateWall(new Vector2(size / 2f, -thickness / 2f), new Vector2(size + thickness * 2f, thickness));
        // 상단
        CreateWall(new Vector2(size / 2f, size + thickness / 2f), new Vector2(size + thickness * 2f, thickness));
        // 좌측
        CreateWall(new Vector2(-thickness / 2f, size / 2f), new Vector2(thickness, size + thickness * 2f));
        // 우측
        CreateWall(new Vector2(size + thickness / 2f, size / 2f), new Vector2(thickness, size + thickness * 2f));
    }

    private void CreateWall(Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject("Wall");
        wall.transform.position = new Vector3(position.x, position.y, 0f);
        var box = wall.AddComponent<BoxCollider2D>();
        box.size = size;
        wall.layer = LayerMask.NameToLayer("Obstacle");
        spawnedObjects.Add(wall);
    }

    public void ClearMap()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObjects.Clear();
    }
}
