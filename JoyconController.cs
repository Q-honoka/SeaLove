using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public class JoyconController : MonoBehaviour
{
    [SerializeField, Header("joyconのエラーを表示するテキスト")]
    private TextMeshProUGUI logText;
    [SerializeField, Header("自動回転するヨーのしきい値（度）")]
    private float autoRotateYawThreshold = 40f;
    [SerializeField, Header("自動回転スピード（度/秒）")]
    private float autoRotateSpeed = 10f;
    [SerializeField, Header("しきい値")]
    private float threshold = 3f;
    [SerializeField, Header("測定回数")]
    private int inputCount = 10;
    [SerializeField, Header("スピード")]
    private float moveSpeed = 5f;
    [SerializeField, Header("壁に衝突したときに跳ね返る力")]
    private float repulsiveForce = 3f;
    [SerializeField, Header("接続チェックの間隔")]
    private float checkTime = 3f;

    private List<Joycon> joycons;
    private Joycon joyconR;
    private Joycon joyconL;

    /* 回転に使用している変数 */
    private Vector3 gyroL;      // ジャイロ値
    private float pitch;        // 上下の回転値
    private float yaw;          // 左右の回転値
    private float pitchOffset;  // ピッチのオフセット
    private float yawOffset;    // ヨーのオフセット
    private float oldPitch;     // ひとつ前のピッチ
    private float oldYaw;       // ひとつ前のヨー
    private float relativeYaw;  // 現在のヨー
    private float currentRightYawThreshold; // 現在のしきい値（右）
    private float currentLeftYawThreshold;  // 現在のしきい値（左）

    /* 移動に使用している変数 */
    private Rigidbody rb;   // プレイヤーのRigidbody
    private Vector3 accR;   // 加速度
    private float[] accY;   // 加速度のy
    private float high;     // 最高値
    private float low;      // 最低値
    private bool isMove;    // 移動フラグ
    private int currentCount;   // 現在の測定回数
    private float oldAccY;      // ひとつ前の測定値

    ///* アニメーションに使用している変数 */
    //private Animator playerAnimator;

    /* 再接続に使用している変数 */
    private float timer = 0;        // 経過時間

    // Start is called before the first frame update
    void Start()
    {
        // joyconの取得
        joycons = JoyconManager.Instance.j;
        if (joycons == null || joycons.Count <= 0)
        {
            Debug.Log("joyconが取得できない");
            Reconnect();
            return;
        }
        joyconL = joycons.Find(j => j.isLeft);
        joyconR = joycons.Find(j => !j.isLeft);

        // 変数初期化
        if(logText == null)
        {
            Debug.Log("TextMeshProが入ってない");
        }
        logText.enabled = false;

        pitchOffset = 0f;
        yawOffset = 0f;
        currentRightYawThreshold = autoRotateYawThreshold;
        currentLeftYawThreshold = -autoRotateYawThreshold;

        rb = GetComponent<Rigidbody>();
        accY = new float[inputCount];
        currentCount = 0;

        //playerAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (joyconR == null || joyconL == null)
        {
            Debug.Log("左右のjoyconが接続されていない");
            Reconnect();
            return;
        }

        // 定期的に接続をチェックする
        if (timer > checkTime)
        {
            Debug.Log("接続チェック");
            Reconnect();
            timer = 0;
        }

        // デバッグ用：Fキーでjoyconの接続を切断する
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("デバッグ用：joyconの接続を切断");
            if (joyconL != null) joyconL.Detach();
            if (joyconR != null) joyconR.Detach();
            joyconL = null;
            joyconR = null;
        }

        InputData();

        // Lボタンでリセット（ピッチのみ）
        if (joyconL.GetButtonDown(Joycon.Button.SHOULDER_1))
        {
            pitch = 0f;
            pitchOffset = 0f;
            oldPitch = 0f;
        }

        RotateCube();

        // ヨーが左右に向いていたら自動回転
        relativeYaw = yaw - yawOffset;

        // Joyconが静止しているか確認
        bool isGyroStill = Mathf.Abs(gyroL.y) < 0.05f;

        // 右向きならさらに右へ回転(徐々にしきい値を上げていく)
        if (isGyroStill && relativeYaw > currentRightYawThreshold)
        {
            yaw += autoRotateSpeed * Time.deltaTime;
            currentRightYawThreshold += autoRotateSpeed * Time.deltaTime;
            currentLeftYawThreshold += autoRotateSpeed * Time.deltaTime;
            //Debug.Log("右に曲がる");
            //playerAnimator.SetTrigger("TurnRight");    // 右に曲がるアニメーション
        }
        // 左向きならさらに左へ回転(徐々にしきい値を下げていく)
        else if (isGyroStill && relativeYaw < currentLeftYawThreshold)
        {
            yaw -= autoRotateSpeed * Time.deltaTime;
            currentRightYawThreshold -= autoRotateSpeed * Time.deltaTime;
            currentLeftYawThreshold -= autoRotateSpeed * Time.deltaTime;
            //Debug.Log("左に曲がる");
            //playerAnimator.SetTrigger("TurnLeft");  // 左に曲がるアニメーション
        }

        CubeMove();
    }

    /// <summary>
    /// データの取得関数
    /// </summary>
    private void InputData()
    {
        // データの取得
        gyroL = joyconL.GetGyro();

        float delta = Time.deltaTime * Mathf.Rad2Deg;

        // ピッチ（上下）：ジャイロ積分
        pitch += gyroL.z * delta;

        // ヨー（左右）：ジャイロ積分
        yaw += gyroL.y * delta;

        // 加速度測定
        if (currentCount < inputCount)
        {
            // データ取得
            accR = joyconR.GetAccel();
            accY[currentCount] = accR.y;

            // データが変わらなければ測定しない
            if (oldAccY == accY[currentCount])
            {
                return;
            }

            // 値の更新（高い）
            if (high < accY[currentCount])
            {
                high = accY[currentCount];
            }

            // 値の更新（低い）
            if (low > accY[currentCount])
            {
                low = accY[currentCount];
            }

            // ひとつ前の値の更新 と カウントのインクリメント
            oldAccY = accY[currentCount];
            currentCount++;
        }
    }


    /// <summary>
    /// 回転関数
    /// </summary>
    private void RotateCube()
    {
        // 数値の調整
        float currentPitch = Mathf.Clamp(pitch - pitchOffset, -60f, 60f);
        float currentYaw = yaw - yawOffset;

        // フィルター
        currentPitch = Mathf.Lerp(oldPitch, currentPitch, 0.1f);
        currentYaw = Mathf.Lerp(oldYaw, currentYaw, 0.1f);

        oldPitch = currentPitch;
        oldYaw = currentYaw;

        // 回転を適用
        transform.localEulerAngles = new Vector3(currentPitch, currentYaw, 0f);
    }

    /// <summary>
    /// 移動関数
    /// </summary>
    void CubeMove()
    {
        // 測定回数データを取得
        if (currentCount >= inputCount)
        {
            // 変数設定
            currentCount = 0;
            high = 0.0f;
            low = 5.0f;

            // 振れ幅がしきい値より大きいなら移動
            if (Mathf.Abs(high - low) > threshold && !isMove)
            {
                isMove = true;
                // 移動
                rb.AddForce(transform.forward * moveSpeed * 100f);

                // 測定したデータをリセット
                Array.Clear(accY, 0, accY.Length);
            }

        }

        // 勢いが弱まったら移動させる
        if (isMove && rb.velocity.magnitude < 1.0f)
        {
            rb.AddForce(transform.forward * moveSpeed * Time.deltaTime);
        }

        //// 初めて泳いだときのみ実行
        //if (isMove && !playerAnimator.GetBool("IsSwim"))
        //{
        //    Debug.Log("泳ぐ");
        //    playerAnimator.SetBool("IsSwim", true);     // 泳ぐアニメーション
        //}

    }

    /// <summary>
    /// 壁とかと衝突したときに反対方向を向く
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        // 衝突したオブジェクトの法線ベクトル（向き）を取得
        Vector3 normal = collision.contacts[0].normal;

        Vector3 backDir = -normal.normalized;

        // 進行方向を法線ベクトルで反射
        Vector3 reflectDirection = Vector3.Reflect(transform.forward, normal);

        // プレイヤーとぶつかったときは少し強く弾かれる
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("プレイヤーと衝突");
            //rb.AddForce(backDir * repulsiveForce * 1.5f, ForceMode.Impulse);
            return;
        }

        // 反射した方向を現在の回転に適用
        rb.AddForce(backDir * repulsiveForce, ForceMode.Impulse);
        transform.rotation = Quaternion.LookRotation(reflectDirection);
    }

    /// <summary>
    /// 再接続の処理
    /// </summary>
    private void Reconnect()
    {
        if (joyconL != null && joyconR != null)
        {
            Debug.Log("接続済み");
            return;
        }
        Debug.Log("再接続");

        // すべてのjoyconの接続を切る(重複を防ぐため)
        foreach (Joycon jc in JoyconManager.Instance.j)
        {
            jc.Detach();
        }
        JoyconManager.Instance.j.Clear();
        joyconL = null;
        joyconR = null;

        ushort vendorID = 0x057E;         // ベンダーID
        ushort[] productIDs = { 0x2006, 0x2007 }; // Joycon左: 0x2006、Joycon右: 0x2007

        // デバイスの数
        foreach (ushort pid in productIDs)
        {
            // 接続されているデバイス一覧の取得
            IntPtr list = HIDapi.hid_enumerate(vendorID, pid);
            // すべてのデバイスを走査
            while (list != IntPtr.Zero)
            {
                hid_device_info info = Marshal.PtrToStructure<hid_device_info>(list);

                IntPtr handle = HIDapi.hid_open_path(info.path);
                if (handle != IntPtr.Zero)
                {
                    bool isLeft = (info.product_id == 0x2006);  // 左のjoyconを取得
                    // joyconインスタンスの生成・接続
                    Joycon jc = new Joycon(handle, imu: true, localize: false, alpha: 0.5f, left: isLeft);
                    jc.Attach();
                    jc.Begin();
                    JoyconManager.Instance.j.Add(jc);
                }

                list = info.next;
            }
        }

        // 左右のjoyconの取得
        joycons = JoyconManager.Instance.j;
        joyconL = joycons.Find(j => j.isLeft);
        joyconR = joycons.Find(j => !j.isLeft);

        // 左右のjoyconの取得が出来たかチェック
        if (joyconL != null && joyconR != null)
        {
            Debug.Log("左右のjoycon取得");
            return;
        }
        Debug.Log("接続失敗");
    }

    /// <summary>
    /// ジョイコンを振動させる関数
    /// </summary>
    //public void JoyconRumble()
    //{
    //    joyconL.SetRumble(100, 160, 10, 1);
    //}
}
