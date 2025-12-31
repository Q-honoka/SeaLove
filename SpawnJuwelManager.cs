using UnityEngine;

public class SpawnJuwelManager : MonoBehaviour
{
    /*  大家さんのスクリプトの変数
     *  SpawnJuwelManager instance
     *  int gemCount = 0
     *  TextMeshProUGUI gemText
     */
    public static SpawnJuwelManager instance;

    [SerializeField, Header("生成する宝石")]
    GameObject[] juwelPrefabs;
    [SerializeField, Header("生成する範囲")]
    BoxCollider[] spawnAreas;
    [SerializeField, Header("生成範囲に対する宝石の生成個数")]
    int[] spawnCount;
    [SerializeField, Header("宝石についているレイヤー")]
    LayerMask juwelLayerMask;
    [SerializeField, Header("ステージについているレイヤー")]
    LayerMask stageLayerMask;
    [SerializeField, Header("地面と宝石の距離")]
    float juwelOffset;
    [SerializeField, Header("1つの宝石に対する座標の再生成数上限")]
    int maxRegenerate;

    private int juwelIndex;         // 生成する宝石の種類
    private BoxCollider currentArea;        // 現在、生成対象のBoxCollider

    /*  以下3つの関数は大家さんのスクリプトの関数をそのまま使っています
     *  Awake()
     *  AddGem()
     *  UpdateUI()
     */
    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        // 変数初期化
        juwelIndex = 0;

        // 生成エリアの数繰り返し
        for (int i = 0; i < spawnAreas.Length; i++)
        {
            currentArea = spawnAreas[i];
            // 生成する宝石数繰り返し
            for (int j = 0; j < spawnCount[i]; j++)
            {
                //Debug.Log("生成エリア:" + i + "宝石数:" + j);
                // 宝石生成
                SpawnJuwelObject();
            }
        }


    }

    /// <summary>
    /// 宝石の生成
    /// </summary>
    private void SpawnJuwelObject()
    {
        // 最大でmaxRegenerate分繰り返す
        for (int i = 0; i < maxRegenerate; i++)
        {
            // 座標の生成
            Vector3 spawnPos = GenerateSpawnPosition();
            //Debug.Log(spawnPos);

            // 重なっていない かつ 地面と衝突した 場合宝石を生成して次にいく
            if (!CheckOverlap(spawnPos) && spawnPos != Vector3.zero)
            {
                Instantiate(juwelPrefabs[juwelIndex], spawnPos, Quaternion.identity);
                juwelIndex = (juwelIndex + 1) % juwelPrefabs.Length;
                //Debug.Log("宝石No." + juwelIndex + "を生成");
                return;
            }
        }

        // 生成できなかったらスキップ
        Debug.LogWarning("宝石の生成ができませんでした");
    }

    /// <summary>
    /// 生成する宝石の座標を設定
    /// </summary>
    /// <returns></returns>
    private Vector3 GenerateSpawnPosition()
    {
        // 座標を設定
        Vector3 center = currentArea.center + transform.position;
        Vector3 halfSize = currentArea.size * 0.5f;
        float spawnHeight = center.y + halfSize.y;

        // 仮で宝石の生成座標を乱数で決める
        Vector3 localPos = new Vector3(
                Random.Range(-halfSize.x, halfSize.x),
                spawnHeight,
                Random.Range(-halfSize.z, halfSize.z)
                );
        Vector3 worldPos = currentArea.transform.TransformPoint(localPos);

        // 地面との衝突を検知する
        Ray ray = new Ray(worldPos, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, stageLayerMask))
        {
            // 地面と衝突したら生成したy座標を再設定
            worldPos.y = hit.point.y + juwelOffset;
            //Debug.Log("地面と衝突した! y座標:" + hit.point.y);
            return worldPos;
        }
        else
        {
            Debug.LogWarning("地面と衝突しなかった");
            return Vector3.zero;
        }
    }

    /// <summary>
    /// 宝石の重なりチェック
    /// </summary>
    /// <param name="checkPos"></param>
    /// <returns></returns>
    private bool CheckOverlap(Vector3 checkPos)
    {
        // PrefabについているBoxColliderのサイズを重なりのチェックに使う
        Vector3 boxSize = juwelPrefabs[juwelIndex].gameObject.GetComponent<BoxCollider>().size;
        Collider[] hitColls = Physics.OverlapBox(checkPos, boxSize / 2, Quaternion.identity, juwelLayerMask, QueryTriggerInteraction.UseGlobal);

        // 重なりチェック
        if (hitColls.Length == 0)
        {
            // 重なっていないので生成可能
            return false;
        }
        else
        {
            // 重なっているので再生成が必要
            Debug.LogWarning("再生成");
            return true;
        }
    }
}
