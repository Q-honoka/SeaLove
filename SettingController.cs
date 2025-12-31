using UnityEngine;
using UnityEngine.UI;

public class SettingController : MonoBehaviour
{
    public Sprite[] toggleLoveSprite;
    public Sprite[] toggleSeaSprite;
    public Toggle autoRunToggle;
    public Slider sensitivitySlider;
    public Slider volumeSlider;
    private GameObject[] players;
    private GameObject player;
    private GameObject Background;
    private GameObject Checkmark;

    private void OnEnable()
    {
        autoRunToggle.isOn = GameSettings.isAutoRun;
        sensitivitySlider.value = GameSettings.sensitivity;
        volumeSlider.value = GameSettings.volume;
    }

    private void OnDisable()
    {
        GameSettings.isAutoRun = autoRunToggle.isOn;
        GameSettings.sensitivity = sensitivitySlider.value;
        GameSettings.volume = volumeSlider.value;
        player.GetComponent<PlayerController>().isAutoRun = autoRunToggle.isOn;
        player.GetComponent<PlayerController>().sensitivity = sensitivitySlider.value;
    }

    // Start is called before the first frame update
    void Start()
    {
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        autoRunToggle.onValueChanged.AddListener(OnAutoRunClicked);

        if (autoRunToggle != null)
        {
            Background = autoRunToggle.transform.GetChild(0).gameObject;
        }
        if (Background != null)
        {
            Checkmark = Background.transform.GetChild(0).gameObject;
        }

        // 操作しているプレイヤーの取得
        players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            if (p.gameObject.GetComponent<NetPlayerController>().isLocalPlayer)
                player = p;
        }

        // 人魚に合わせた画像の挿入
        if(player != null)
        {
            if (player.GetComponent<NetPlayerController>().playerId == 1 && Background != null)    // player1の場合
            {
                Background.GetComponent<Image>().sprite = toggleLoveSprite[0];
                Checkmark.GetComponent<Image>().sprite = toggleLoveSprite[1];
            }
            else if(player.GetComponent<NetPlayerController>().playerId == 2 && Checkmark != null)  // player2の場合
            {
                Background.GetComponent<Image>().sprite = toggleSeaSprite[0];
                Checkmark.GetComponent<Image>().sprite = toggleSeaSprite[1];
            }
        }
    }

    public void OnAutoRunClicked(bool isAutoRun)
    {
        if (player != null)
        {
            player.GetComponent<PlayerController>().isAutoRun = isAutoRun;
            GameSettings.isAutoRun = isAutoRun;
        }
    }

    private void OnSensitivityChanged(float sensitivity)
    {
        if (player != null)
        {
            player.GetComponent<PlayerController>().sensitivity = sensitivity;
            GameSettings.sensitivity = sensitivity;
        }
    }

    private void OnVolumeChanged(float volume)
    {
        SoundManager.bgmVolume = volume;
    }
}
