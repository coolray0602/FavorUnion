using UnityEngine;

public class FloatingCloudsFade : MonoBehaviour
{
    [Header("雲設定")]
    public GameObject[] cloudPrefabs;     // 多種雲 Prefab
    public int cloudCount = 10;           // 雲的數量
    public float minY = 20f;              // 雲最低高度
    public float maxY = 50f;              // 雲最高高度
    public float minScale = 5f;           // 雲最小尺寸
    public float maxScale = 15f;          // 雲最大尺寸
    public Vector2 speedRange = new Vector2(0.1f, 0.5f); // 漂浮速度
    public float spawnRadius = 100f;      // 雲生成範圍
    public Camera playerCamera;           // 玩家攝像機
    public float fadeDistance = 20f;      // 超出範圍開始淡出距離
    public float fadeSpeed = 0.5f;        // 淡出速度

    private GameObject[] clouds;
    private float[] speeds;
    private Material[] cloudMaterials;

    void Start()
    {
        if (cloudPrefabs.Length == 0 || playerCamera == null)
        {
            Debug.LogError("請先設置至少一個雲 Prefab和玩家攝像機");
            return;
        }

        clouds = new GameObject[cloudCount];
        speeds = new float[cloudCount];
        cloudMaterials = new Material[cloudCount];

        for (int i = 0; i < cloudCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                Random.Range(minY, maxY),
                Random.Range(-spawnRadius, spawnRadius)
            );

            GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
            clouds[i] = Instantiate(prefab, pos, Quaternion.identity, transform);

            float scale = Random.Range(minScale, maxScale);
            clouds[i].transform.localScale = new Vector3(scale, scale, scale);

            float yRotation = Random.Range(0f, 360f);
            clouds[i].transform.Rotate(0f, yRotation, 0f);

            speeds[i] = Random.Range(speedRange.x, speedRange.y);

            // 取得材質實例，方便改 alpha
            Renderer rend = clouds[i].GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                cloudMaterials[i] = new Material(rend.material);
                rend.material = cloudMaterials[i];
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < cloudCount; i++)
        {
            if (clouds[i] == null) continue;

            // 水平漂浮
            clouds[i].transform.Translate(Vector3.right * speeds[i] * Time.deltaTime, Space.World);

            // 面向玩家
            Vector3 lookDir = playerCamera.transform.position - clouds[i].transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                clouds[i].transform.rotation = Quaternion.LookRotation(lookDir);

            // 計算離玩家中心的距離
            float distance = Vector3.Distance(new Vector3(clouds[i].transform.position.x, 0, clouds[i].transform.position.z),
                                              new Vector3(playerCamera.transform.position.x, 0, playerCamera.transform.position.z));

            // 開始淡出
            if (distance > spawnRadius)
            {
                if (cloudMaterials[i] != null)
                {
                    Color c = cloudMaterials[i].color;
                    c.a = Mathf.Max(0, c.a - fadeSpeed * Time.deltaTime);
                    cloudMaterials[i].color = c;

                    // 完全透明時銷毀雲
                    if (c.a <= 0)
                    {
                        Destroy(clouds[i]);
                        clouds[i] = null;
                        cloudMaterials[i] = null;
                    }
                }
            }
            else if (distance > spawnRadius - fadeDistance)
            {
                // 漸漸淡出
                if (cloudMaterials[i] != null)
                {
                    float alpha = Mathf.Clamp01(1 - (distance - (spawnRadius - fadeDistance)) / fadeDistance);
                    Color c = cloudMaterials[i].color;
                    c.a = alpha;
                    cloudMaterials[i].color = c;
                }
            }
            else
            {
                // 在範圍內恢復不透明
                if (cloudMaterials[i] != null)
                {
                    Color c = cloudMaterials[i].color;
                    c.a = 1f;
                    cloudMaterials[i].color = c;
                }
            }
        }
    }
}
