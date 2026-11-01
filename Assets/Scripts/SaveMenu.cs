using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveMenu : MonoBehaviour
{
    public const string DifficultyKey = "GameDifficulty";
    public const string HorizontalSensitivityKey = "HorizontalMouseSensitivity";
    public const string VerticalSensitivityKey = "VerticalMouseSensitivity";

    private const int NormalDifficulty = 0;
    private const int HardcoreDifficulty = 1;
    private const float DefaultSensitivity = 0.5f;

    [Header("Menu controls")]
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Slider horizontalSensitivitySlider;
    [SerializeField] private Slider verticalSensitivitySlider;

    private int selectedDifficulty;

    private void Start()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        selectedDifficulty = Mathf.Clamp(
            PlayerPrefs.GetInt(DifficultyKey, NormalDifficulty),
            NormalDifficulty,
            HardcoreDifficulty);

        if (difficultyDropdown != null)
        {
            difficultyDropdown.SetValueWithoutNotify(selectedDifficulty);
        }

        if (horizontalSensitivitySlider != null)
        {
            horizontalSensitivitySlider.SetValueWithoutNotify(
                PlayerPrefs.GetFloat(HorizontalSensitivityKey, DefaultSensitivity));
        }

        if (verticalSensitivitySlider != null)
        {
            verticalSensitivitySlider.SetValueWithoutNotify(
                PlayerPrefs.GetFloat(VerticalSensitivityKey, DefaultSensitivity));
        }
    }

    public void OnDifficultyChanged(int difficultyIndex)
    {
        selectedDifficulty = Mathf.Clamp(
            difficultyIndex,
            NormalDifficulty,
            HardcoreDifficulty);
    }

    public void SelectNormalDifficulty()
    {
        SetDifficulty(NormalDifficulty);
    }

    public void SelectHardcoreDifficulty()
    {
        SetDifficulty(HardcoreDifficulty);
    }

    public void OnHorizontalSensitivityChanged(float value)
    {
        float verticalSensitivity = verticalSensitivitySlider != null
            ? verticalSensitivitySlider.value
            : PlayerPrefs.GetFloat(VerticalSensitivityKey, DefaultSensitivity);
        ApplySensitivity(value, verticalSensitivity);
    }

    public void OnVerticalSensitivityChanged(float value)
    {
        float horizontalSensitivity = horizontalSensitivitySlider != null
            ? horizontalSensitivitySlider.value
            : PlayerPrefs.GetFloat(HorizontalSensitivityKey, DefaultSensitivity);
        ApplySensitivity(horizontalSensitivity, value);
    }

    public void SaveSettings()
    {
        if (difficultyDropdown != null)
        {
            selectedDifficulty = Mathf.Clamp(
                difficultyDropdown.value,
                NormalDifficulty,
                HardcoreDifficulty);
        }

        PlayerPrefs.SetInt(DifficultyKey, selectedDifficulty);

        float horizontalSensitivity = horizontalSensitivitySlider != null
            ? horizontalSensitivitySlider.value
            : PlayerPrefs.GetFloat(HorizontalSensitivityKey, DefaultSensitivity);
        float verticalSensitivity = verticalSensitivitySlider != null
            ? verticalSensitivitySlider.value
            : PlayerPrefs.GetFloat(VerticalSensitivityKey, DefaultSensitivity);

        PlayerPrefs.SetFloat(HorizontalSensitivityKey, horizontalSensitivity);
        PlayerPrefs.SetFloat(VerticalSensitivityKey, verticalSensitivity);
        PlayerPrefs.Save();

        ApplySensitivity(horizontalSensitivity, verticalSensitivity);
    }

    private void ApplySensitivity(float horizontalSensitivity, float verticalSensitivity)
    {
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            cameraController.SetMouseSensitivity(
                horizontalSensitivity,
                verticalSensitivity);
        }
    }

    public static float GetDifficultyMultiplier()
    {
        return PlayerPrefs.GetInt(DifficultyKey, NormalDifficulty) == HardcoreDifficulty
            ? 2f
            : 1f;
    }

    private void SetDifficulty(int difficulty)
    {
        selectedDifficulty = difficulty;
        if (difficultyDropdown != null)
        {
            difficultyDropdown.SetValueWithoutNotify(selectedDifficulty);
        }
    }
}
