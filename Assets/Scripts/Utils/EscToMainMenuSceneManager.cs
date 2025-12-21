using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EscToMainMenuSceneManager : MonoBehaviour
{
    public static EscToMainMenuSceneManager instance;

    [SerializeField] private InputAction escAction;

    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            escAction = InputSystem.actions.FindAction("EscapeToMainMenu");
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        var activeScene = SceneManager.GetActiveScene();

        // Vráť sa do menu ak nie si v MainMenu a stlačíš ESC
        if (activeScene.name != "MainMenuScene" && escAction != null && escAction.WasPressedThisFrame())
        {
            Debug.Log("ESC pressed - returning to main menu");
            
            // Použiť GameManager na návrat (ak existuje)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
            else
            {
                SceneManager.LoadScene("MainMenuScene");
            }
        }
    }
}