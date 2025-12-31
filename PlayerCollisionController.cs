using UnityEngine;

public class PlayerCollisionController : MonoBehaviour
{
    public float boundsPlayerForce = 1f;
    public float boundsWallForce = 0.5f;
    private X_BubbleController bubble;
    private Rigidbody rb;
    private Vector3 boundDir;   // 衝突方向

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        bubble = GameObject.Find("Canvas").gameObject.GetComponentInChildren<X_BubbleController>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))   // 衝突した相手がプレイヤーだった場合
        {
            if(rb != null)
            {
                // 衝突した相手との方向ベクトルを計算
                boundDir = (transform.position - collision.transform.position).normalized;
                rb.AddForce(boundDir * boundsPlayerForce, ForceMode.Impulse); // 反発力を加える
            }
        }
        else if(collision.gameObject.CompareTag("Wall"))    // 衝突した相手が壁だった場合
        {
            if (rb != null)
            {
                // 壁との方向ベクトルを計算
                boundDir = (transform.position - collision.contacts[0].point).normalized;
                rb.AddForce(boundDir * boundsWallForce, ForceMode.Impulse); // 少し弱い力で反発
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("RecoverArea") && bubble != null)
        {
            bubble.StartRecover();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("RecoverArea") && bubble != null)
        {
            bubble.StopRecover();
        }
    }
}
