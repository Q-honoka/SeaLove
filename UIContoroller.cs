using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIContoroller : MonoBehaviour
{
    private List<Joycon> joycons;
    private Joycon joyconL;
    private Joycon joyconR;

    public GameObject initSelectButton; // 初期選択ボタン
    public GameObject settingsMenu;     // 設定画面

    public GameObject selectedOb;       // 選択されているオブジェクト
    private GameObject initSelectSettingOb;    // 最初に選択されているオブジェクト
    private float moveCoolTime = 1f;   // 操作可能になるまでのクールタイム
    private float moveTime = 0f;
    private float[] stick = new float[2];
    private float stickX = 0f;
    private float stickY = 0f;
    private float stickThreshould = 0.5f;   // 入力のしきい値
    private bool isHold = false;            // 長押しかどうか
    private float buttonHoldTime = 0f;      // 長押し時間
    private float buttonHoldThreashould = 1f;   // 決定までに必要な時間
    private float inputTimer = 0f;
    private float inputDelay = 1f;   // 入力可能になるまでの時間
    private float inputStartTime = 0f;
    private Slider targetSlider;    // 選択中のスライダー
    private Toggle targetToggle;    // 選択中のトグル
    private float sliderSpeed = 0.5f;
    private float delta = 0f;
    private int rotDir = -1;

    void Start()
    {
        joycons = JoyconManager.Instance.j;
        for (int i = 0; i < joycons.Count; i++)
        {
            if (joycons[i].isLeft) joyconL = joycons[i];
            else joyconR = joycons[i];
        }

        // 初期に選択されているボタンを設定
        if (selectedOb == null)
        {
            if (initSelectButton == null)
            {
                Debug.Log("初期選択ボタンがないので設定できない");
                return;
            }
            EventSystem.current.SetSelectedGameObject(initSelectButton);
        }

        isHold = SceneManager.GetActiveScene().name == "TutorialScene" ? true : false;
        inputStartTime = Time.time + inputDelay;
        selectedOb = EventSystem.current.currentSelectedGameObject;
        if(settingsMenu != null)
        {
            initSelectSettingOb = settingsMenu.transform.GetChild(0).gameObject;
        }
    }

    void Update()
    {
        delta = Time.deltaTime;
        selectedOb = EventSystem.current.currentSelectedGameObject;
        // 入力可能時間になるまで処理しない
        if (inputTimer < inputStartTime)
        {
            inputTimer += delta;
            return;
        }

        moveTime += delta;
        if (joyconL == null || joyconR == null)
        {
            return;
        }

        // データ取得
        stick = joyconL.GetStick();
        stickX = stick[0];
        stickY = stick[1];
        moveTime += delta;

        // UI操作
        if (moveTime > moveCoolTime)
        {
            if (stickY > stickThreshould)      // 上に移動
            {
                MoveUI(Vector3.up);
                moveTime = 0;
            }
            else if (stickY < -stickThreshould) // 下に移動
            {
                MoveUI(Vector3.down);
                moveTime = 0;
            }
            else if (stickX > stickThreshould)  // 右に移動
            {
                MoveUI(Vector3.right);
                moveTime = 0;
            }
            else if (stickX < -stickThreshould) // 左に移動
            {
                MoveUI(Vector3.left);
                moveTime = 0;
            }
        }

        // 決定処理
        if (JoyconSubmit())
        {
            ExecuteEvents.Execute(selectedOb, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            if (selectedOb.GetComponent<Button>() != null && isHold && !settingsMenu.activeSelf)
            {
                Debug.Log("長押し決定完了");
                selectedOb.GetComponent<Button>().interactable = false;
                EventSystem.current.SetSelectedGameObject(initSelectButton);
            }
        }


        // キャンセル
        if (joyconR.GetButtonDown(Joycon.Button.DPAD_DOWN))
        {
            Debug.Log("キャンセル");
            ExecuteEvents.Execute(selectedOb, new BaseEventData(EventSystem.current), ExecuteEvents.cancelHandler);
            if (selectedOb.GetComponent<Button>() != null && isHold && !settingsMenu.activeSelf)
            {
                selectedOb.GetComponent<Button>().interactable = true;
            }
        }

        // 設定画面を開く
        if (joyconR.GetButtonDown(Joycon.Button.PLUS))
        {
            if (settingsMenu == null)
            {
                Debug.Log("設定画面がないので表示できない");
                return;
            }
            settingsMenu.SetActive(!settingsMenu.activeSelf);
            if (settingsMenu.activeSelf)
            {
                initSelectSettingOb = GameObject.Find("Setting_AutoToggle");
                EventSystem.current.SetSelectedGameObject(initSelectSettingOb);
                selectedOb = EventSystem.current.currentSelectedGameObject;
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(initSelectButton);
                selectedOb = EventSystem.current.currentSelectedGameObject;
            }
        }

        // 設定画面処理
        if(settingsMenu != null)
        {
            if (settingsMenu.activeSelf && selectedOb != null)
            {
                // スライダーの操作
                targetSlider = selectedOb.GetComponentInChildren<Slider>();
                if (targetSlider != null && Mathf.Abs(stickX) > stickThreshould)
                {
                    Debug.Log("スライダー");
                    targetSlider.value += stickX * sliderSpeed;
                }

                // トグルの操作
                targetToggle = selectedOb.GetComponent<Toggle>();
                if (targetToggle != null && JoyconSubmit())
                {
                    Debug.Log("トグル");
                    targetToggle.isOn = !targetToggle.isOn;
                }
            }
        }


        // 選択中のボタンがなければ初期選択ボタンを選択状態にする
        if (selectedOb == null)
        {
            if (settingsMenu != null && settingsMenu.activeSelf)
            {
                EventSystem.current.SetSelectedGameObject(initSelectSettingOb);
                selectedOb = EventSystem.current.currentSelectedGameObject;
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(initSelectButton);
                selectedOb= EventSystem.current.currentSelectedGameObject;
            }
        }
    }

    /// <summary>
    /// メニュー操作
    /// </summary>
    /// <param name="dir"></param>
    private void MoveUI(Vector3 dir)
    {
        GameObject current = selectedOb;
        if (current == null) return;

        Selectable selectable = current.GetComponent<Selectable>();
        if (selectable == null) return;

        Selectable next = null;
        if (dir == Vector3.up)           // 上
        {
            next = selectable.FindSelectableOnUp();
        }
        else if (dir == Vector3.down)    // 下
        {
            next = selectable.FindSelectableOnDown();
        }
        else if (dir == Vector3.left)    // 左
        {
            next = selectable.FindSelectableOnLeft();
        }
        else if (dir == Vector3.right)   // 右
        {
            next = selectable.FindSelectableOnRight();
        }

        if (next != null)
        {
            Debug.Log("移動");
            next.Select();
        }
        selectedOb = EventSystem.current.currentSelectedGameObject;
    }

    /// <summary>
    /// Joyconを使った決定処理
    /// </summary>
    /// <returns></returns>
    private bool JoyconSubmit()
    {
        if (isHold)      // 長押しで決定
        {
            if (joyconR.GetButton(Joycon.Button.DPAD_RIGHT))
            {
                buttonHoldTime += delta;

                // 一定時間押されたら決定
                if (buttonHoldTime > buttonHoldThreashould)
                {
                    buttonHoldTime = 0f;
                    return true;
                }
            }
            else
            {
                buttonHoldTime = 0f;    // リセット
                return false;
            }
        }
        else        // 単押しで決定
        {
            if (joyconR.GetButtonDown(Joycon.Button.DPAD_RIGHT))
            {
                return true;
            }
        }

        return false;
    }
}
