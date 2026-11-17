using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private const string VolumeKey = "TPS_MasterVolume_V2";

    [Header("Panels")]
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _loadingPanel;

    [Header("Loading")]
    [SerializeField] private Slider _loadingSlider;
    [SerializeField] private TMP_Text _loadingText;

    [Header("Audio")]
    [SerializeField] private Slider _volumeSlider;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _settingsPanel.SetActive(false);
        _loadingPanel.SetActive(false);
        
		float volume = PlayerPrefs.GetFloat("TPS_MasterVolume_V2", 1f);

		_volumeSlider.SetValueWithoutNotify(volume);
		AudioListener.volume = volume;

		_volumeSlider.onValueChanged.AddListener(ChangeVolume);
		Debug.Log("Загруженная громкость: " + volume);
    }

    public void PlayGame()
    {
        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        _loadingPanel.SetActive(true);
        _loadingSlider.value = 0f;

        AsyncOperation operation =
            SceneManager.LoadSceneAsync("scene_sandbox");

        if (operation == null)
        {
            Debug.LogError("Game scene not found!");
            _loadingPanel.SetActive(false);
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            float progress =
                Mathf.Clamp01(operation.progress / 0.9f);

            _loadingSlider.value = progress;
            _loadingText.text =
                $"ЗАГРУЗКА... {progress * 100f:0}%";

            yield return null;
        }

        _loadingSlider.value = 1f;
        _loadingText.text = "ЗАГРУЗКА... 100%";

        yield return null;
        operation.allowSceneActivation = true;
    }

    public void OpenSettings()
    {
        _settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        _settingsPanel.SetActive(false);
    }

    
	
	public void ChangeVolume(float value)
	{
    	float volume = Mathf.Clamp01(value);

    	AudioListener.volume = volume;

    	PlayerPrefs.SetFloat(VolumeKey, volume);
    	PlayerPrefs.Save();

    	Debug.Log("Сохранённая громкость: " + volume);
	}



    public void ExitGame()
    {
        Application.Quit();
    }
}

