using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class X_BubbleController : MonoBehaviour
{
    public GameObject[] bubbles;    // ステージ上の泡オブジェクト
    private Image im;
    private GameObject[] players;   // 全てのプレイヤー
    private GameObject player;      // プレイヤー
    private float gageTime = 90f;   // ゲージが0になるまでの時間
    private float gage = 90;
    private bool recovering = false;

    private void Start()
    {
    }

    private void Update()
    {
        GageUpdate();

        if (60 <= gage)      // 大サイズの泡
        {
            im = this.transform.GetChild(0).GetComponent<Image>();
            im.fillAmount = (gage - 60f) / 30f;
        }
        else if (30 <= gage && gage < 60) // 中サイズの泡
        {
            im = this.transform.GetChild(1).GetComponent<Image>();
            im.fillAmount = (gage - 30f) / 30f;
        }
        else if (0 <= gage && gage < 30)
        {
            im = this.transform.GetChild(2).GetComponent<Image>();
            im.fillAmount = gage / 30f;
        }
        else
        {
            im = this.transform.GetChild(0).GetComponent<Image>();
            this.transform.GetChild(0).gameObject.SetActive(true);
            this.transform.GetChild(1).gameObject.SetActive(true);
            this.transform.GetChild(2).gameObject.SetActive(true);
        }
    }

    private void GageUpdate()
    {
        float decreaseValue = 1f * Time.deltaTime;
        float increaseValue = 2f * Time.deltaTime;

        if (recovering)
        {
            gage = Mathf.Clamp(gage + increaseValue, 0, gageTime);
        }
        else
        {
            gage = Mathf.Clamp(gage - decreaseValue, 0, gageTime);
        }

        // ゲージがなくなった場合
        if (gage <= 0f)
        {
            EmptyGage();
        }
    }

    // ゲージが0になったときの処理
    private void EmptyGage()
    {
        // 操作しているプレイヤーの取得
        if (player == null)
        {
            players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject p in players)
            {
                if (p.gameObject.GetComponent<NetPlayerController>().isLocalPlayer)
                    player = p;
            }
        }

        float playerHeight = player.GetComponentInChildren<Collider>().bounds.size.y;
        float distance = 0f;
        float minDistance = float.MaxValue;
        int MoveIndex = -1;
        Vector3 movePos = Vector3.zero;

        // 一番距離が近い泡を取得
        for (int i = 0; i < bubbles.Length; i++)
        {
            distance = Vector3.Distance(player.transform.position, bubbles[i].transform.GetChild(0).transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                MoveIndex = i;
            }
        }

        if (MoveIndex == -1)
        {
            Debug.Log("移動先がない");
            return;
        }
        if (player == null)
        {
            Debug.Log("プレイヤーがnull");
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Image>().fillAmount = 1f;
        }

        // プレイヤー移動
        bubbles[MoveIndex].gameObject.GetComponentInChildren<ParticleSystem>().Play();
        player.transform.GetChild(4).GetComponent<ParticleSystem>().Play();
        gage = gageTime;
        movePos = bubbles[MoveIndex].transform.GetChild(0).transform.position;
        movePos.y += playerHeight;
        player.transform.position = movePos;
    }

    // 回復開始
    public void StartRecover()
    {
        recovering = true;
    }

    // 回復終了
    public void StopRecover()
    {
        recovering = false;
    }
}
