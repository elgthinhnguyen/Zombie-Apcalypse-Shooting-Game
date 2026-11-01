using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private const string HighScoreKey = "HighScore";

    [Header("Weapons Management")]
    public GameObject[] Weapons;
    [SerializeField] private int currentWeaponIndex = 0;

    [Header("Score Management")]
    public int highScore;
    public int currentScore;
    public TextMeshProUGUI scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private GameObject deathScreen;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        instance = this;
        highScore = PlayerPrefs.GetInt(HighScoreKey, highScore);
        Time.timeScale = 1f;
    }

    private void Start()
    {
        SwitchWeapon(currentWeaponIndex);
        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
        }
        else
        {
            Debug.LogError("Assign the Death Screen in the GameManager Inspector.", this);
        }

        UpdateScoreUI();
        UpdateHighScoreUI();
        if (highScoreText == null)
        {
            Debug.LogError("Assign the High Score Text in the GameManager Inspector.", this);
        }
    }

    private void Update()
    {
        if (IsGameOver)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchWeapon(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwitchWeapon(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SwitchWeapon(3);
        }

        UpdateScoreUI();
    }

    public void SwitchWeapon(int newIndex)
    {
        if (Weapons != null && newIndex >= 0 && newIndex < Weapons.Length)
        {
            if (currentWeaponIndex >= 0 && currentWeaponIndex < Weapons.Length &&
                Weapons[currentWeaponIndex] != null)
            {
                Weapons[currentWeaponIndex].SetActive(false);
            }

            if (Weapons[newIndex] == null)
            {
                return;
            }

            Weapons[newIndex].SetActive(true);
            currentWeaponIndex = newIndex;
        }
    }

    public void AddScore(int amount)
    {
        if (IsGameOver || amount <= 0)
        {
            return;
        }

        int difficultyAdjustedAmount = Mathf.RoundToInt(
            amount * SaveMenu.GetDifficultyMultiplier());
        currentScore += difficultyAdjustedAmount;
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
            UpdateHighScoreUI();
        }

        UpdateScoreUI();
    }

    public void ShowGameOver()
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        if (deathScreen != null)
        {
            deathScreen.SetActive(true);
        }

        UpdateHighScoreUI();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void Replay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }
    }

    private void UpdateHighScoreUI()
    {
        if (highScoreText != null)
        {
            highScoreText.text = $"High score: {highScore}";
        }
    }
}