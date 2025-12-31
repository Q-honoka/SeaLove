using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    /* エディタから変更可能な値 */
    public float sensitivity = 100f;    // 感度
    public float autoRotSpeed = 1f;     // 自動回転速度
    public float moveForce = 1.0f;      // 移動速度
    public float maxMoveForce = 3.0f;   // 最高速度
    public float minMoveSpeed = 1.0f;   // 最低速度
    public bool isAutoRun = true;       // 自動移動フラグ
    public float autoRunThreshould = 1.0f;  // 自動移動のしきい値

    private List<Joycon> joycons;
    private Joycon joyconR;
    private Joycon joyconL;

    /* 回転に使用する変数 */
    private float pitchThreshould = 45f;     // 上下の回転角度のしきい値
    private float gyroMinThreshould = 0.001f;    // ジャイロの下限値
    //private float gyroMaxThreshould = 5f;        // ジャイロの上限値
    private const float autoRotationThreshouldR = 30f;   // 自動回転のしきい値
    private float autoRotThreshouldR = 0.0f;
    private const float autoRotationThreshouldL = -30f;   // 自動回転のしきい値
    private float autoRotThreshouldL = 0.0f;
    private Vector3 gyro = Vector3.zero;    // ジャイロ値
    private float gyroZ = 0f;        // ジャイロZ
    private float gyroY = 0f;        // ジャイロY
    private float pitch = 0f;        // ピッチ（上下）
    private float yaw = 0f;          // ヨー（左右）
    private Vector3 currRot = Vector3.zero;    // 現在の回転値
    private Vector3 preRot = Vector3.zero;      // ひとつ前の回転値
    private bool isAutoRotR = false;    // 自動回転フラグ(右回転)
    private bool isAutoRotL = false;    // 自動回転フラグ(左回転)

    /* 移動処理に使用する変数 */
    public float moveThreshould = 2f;       // 移動するしきい値
    public float checkInterval = 0.2f;      // 移動の検知の間隔
    private Rigidbody rb;   // Rigidbody
    private float accY;     // 加速度Y
    private float timer = 0f;   // 経過時間
    private float maxAcc = float.MinValue;  // 加速度の最大値
    private float minAcc = float.MaxValue;  // 加速度の最低値
    private bool isRun = false;     // 加速しているかどうか

    void Start()
    {
        // joyconの取得
        joycons = JoyconManager.Instance.j;
        if(joycons.Count <= 0 || joycons == null)
        {
            //Debug.LogWarning("joyconが取得できない");
            return;
        }

        // 左右のjoyconの取得
        for(int i = 0; i < joycons.Count; i++)
        {
            if (joycons[i].isLeft) joyconL = joycons[i];
            else joyconR = joycons[i];
        }

        /* 変数初期化 */
        autoRotSpeed = autoRotSpeed * Time.deltaTime * sensitivity;
        autoRotThreshouldR = autoRotationThreshouldR;
        autoRotThreshouldL = autoRotationThreshouldL;
        preRot = currRot;
        rb = GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        if(joyconL == null || joyconR == null)
        {
            //Debug.LogWarning("joyconが接続されていない");
            if (Reconnect())  Debug.Log("成功");
            else Debug.Log("失敗");
            return;
        }

        InputData();

        PlayerRotate();

        PlayerMove();
    }

    /// <summary>
    /// データの取得関数
    /// </summary>
    /// <returns></returns>
    private void InputData()
    {
        /* ジャイロ値 */
        gyro = joyconL.GetGyro();
        // ジャイロ値に制限をかける
        gyro.z = Mathf.Abs(gyro.z) >= gyroMinThreshould ? gyro.z : 0;
        gyro.y = Mathf.Abs(gyro.y) >= gyroMinThreshould ? gyro.y : 0;
        //gyro.z = Mathf.Clamp(gyro.z, -gyroMaxThreshould, gyroMaxThreshould);
        //gyro.y = Mathf.Clamp(gyro.y, -gyroMaxThreshould, gyroMaxThreshould);
        // 補正をしたジャイロ値を代入
        gyroZ = gyro.z;
        gyroY = gyro.y;

        /* 加速度 */
        if (isRun && isAutoRun)     // 自動移動の場合は加速度の取得をしない
            return;
        accY = joyconR.GetAccel().y;
        if(accY > maxAcc) maxAcc = accY;
        if(accY < minAcc) minAcc = accY;
    }

    /// <summary>
    /// プレイヤーの回転関数
    /// </summary>
    private void PlayerRotate()
    {
        preRot = currRot;   // 回転処理前の回転を保存

        pitch = gyroZ * Time.deltaTime * sensitivity;   // 上下
        yaw = gyroY * Time.deltaTime * sensitivity;     // 左右

        currRot = transform.eulerAngles;    // 現在の回転を取得

        // 上下の回転の制限
        currRot.x += pitch;
        currRot.x = currRot.x > 180f ? currRot.x - 360f : currRot.x;    // -180～180度に変換
        currRot.x = Mathf.Clamp(currRot.x, -pitchThreshould, pitchThreshould);  // 上下の回転角度を制限
        currRot.x = (currRot.x + 360f) % 360f;  // 0～360度に変換
        // 左右の回転の制限
        currRot.y += yaw;

        // 自動回転処理
        AutoRotation();

        transform.rotation = Quaternion.Euler(currRot);     // 回転を適用
    }

    /// <summary>
    /// 自動回転処理
    /// </summary>
    private void AutoRotation()
    {
        // 右に自動回転
        if(currRot.y >= autoRotThreshouldR && !isAutoRotL)
        {
            isAutoRotR = true;
            if (currRot.y <= preRot.y)  // ひとつ前の回転のほうがプラスだったら
            {
                isAutoRotR = false;
                // 基準値の更新
                autoRotThreshouldR = (currRot.y + autoRotationThreshouldR + 360) % 360;
                autoRotThreshouldL = (currRot.y - autoRotationThreshouldL + 360) % 360;
            }
            else
            {
                currRot.y += autoRotSpeed;
            }
        }

        // 左に自動回転
        if (currRot.y <= autoRotThreshouldL && !isAutoRotR)
        {
            isAutoRotL = true;
            if (currRot.y <= preRot.y)  // ひとつ前の回転のほうがマイナスだったら
            {
                isAutoRotL = false;
                // 基準値の更新
                autoRotThreshouldR = (currRot.y + autoRotationThreshouldR + 360) % 360;
                autoRotThreshouldL = (currRot.y - autoRotationThreshouldL + 360) % 360;
            }
            else
            {
                currRot.y -= autoRotSpeed;
            }
        }
    }

    /// <summary>
    /// プレイヤーの移動関数
    /// </summary>
    private void PlayerMove()
    {
        // 自動移動オン
        if(isRun && isAutoRun)
        {
            if(rb.velocity.magnitude > autoRunThreshould)
            {
                MoveForward();  // 前進
            }

            return;     // 自動移動処理終了
        }


        // 自動移動オフ
        timer += Time.deltaTime;    // 経過時間計測

        if(timer >= checkInterval)
        {
            if(maxAcc - minAcc >= moveThreshould)   // 最高値と最低値の差がしきい値を超えたら
            {
                // 前進
                MoveForward();
            }

            // タイマーをリセット
            timer = 0;
            maxAcc = float.MinValue;
            minAcc = float.MaxValue;
        }

        // 速度がしきい値を下回ったら加速していない
        if(rb.velocity.magnitude < minMoveSpeed)
        {
            isRun = false;
        }
    }

    /// <summary>
    /// 前進関数
    /// </summary>
    private void MoveForward()
    {
        Vector3 forwardForce = transform.forward * moveForce;   // 前進速度

        Vector3 currVelocity = rb.velocity;     // 現在の速度取得

        // 前方向の速度を制限
        float forwardSpeed = Vector3.Dot(currVelocity, transform.forward);

        // 速度制限内なら加速
        if(forwardSpeed < maxMoveForce)
        {
            rb.AddForce(forwardForce, ForceMode.Impulse);
            isRun = true;
        }
    }

    /// <summary>
    /// 再接続処理
    /// </summary>
    /// <returns></returns>
    private bool Reconnect()
    {
        // joyconの接続状態を確認
        if (joyconL != null && joyconR != null)
        {
            return true;    // 接続されているなら処理を終える
        }

        // joyconの接続を切る
        foreach(Joycon jc in JoyconManager.Instance.j)
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

        // 左右のjoyconに接続
        joycons = JoyconManager.Instance.j;
        for(int i = 0; i < joycons.Count; i++)
        {
            if (joycons[i].isLeft) joyconL = joycons[i];
            else joyconR = joycons[i];
        }

        if (joyconL != null && joyconR != null) return true;        // 成功
        else return false;      // 失敗
    }

    /// <summary>
    /// 設定で変更した値を反映する関数
    /// </summary>
    public void ApplySetting()
    {
        isAutoRun = GameSettings.isAutoRun;
        sensitivity = GameSettings.sensitivity;
    }

}
