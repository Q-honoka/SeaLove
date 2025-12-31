using Mirror;
using System.Collections;
using UnityEngine;

public class JuwelCollector : MonoBehaviour
{
    [Header("宝石の再生成までの時間")]
    [SerializeField] private float RespawnSeconds;

    private GameObject juwel;
    private ParticleSystem shine;

    private void Awake()
    {
        juwel = this.transform.GetChild(0).gameObject;
        shine = this.GetComponentInChildren<ParticleSystem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject player = other.transform.parent.gameObject;
        if (player.CompareTag("Player"))
        {
            //Debug.Log("Gem取得！");
            // 宝石のカウントを追加
            player.GetComponent<NetPlayerController>().AddGem();

            /*  以下、上別府が変更しました. */
            player.GetComponent<PlayerAnimationController>().Get(); // 取得アニメーション再生

            // 宝石が表示中なら非表示にする
            if (juwel.activeSelf)
            {
                juwel.SetActive(false);
                shine.Stop();
                StartCoroutine(RespawnJuwelCoroutine());
            }
        }
    }

    /// <summary>
    /// 宝石の再生成コルーチン
    /// </summary>
    private IEnumerator RespawnJuwelCoroutine()
    {
        float RespawnTime = RespawnSeconds;
        if (TimerGageController.Instance.LastMinite() == true)
            RespawnTime /= 3;

        yield return new WaitForSeconds(RespawnTime);
        // 再生成の時間経過したら再表示する
        juwel.SetActive(true);
        shine.Play();
    }
}
