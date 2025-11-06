using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EscToMainMenuSceneManager : MonoBehaviour
{
    // Static reference to the instance of our SceneManager
    public static EscToMainMenuSceneManager instance;

    [SerializeField] InputAction escAction;

    private void Awake()
    {
        // Check if instance already exists
        if (instance == null)
        {
            // If not, set instance to this
            instance = this;
            escAction = InputSystem.actions.FindAction("EscapeToMainMenu");
        }
        else if (instance != this)
        {
            // If instance already exists and it's not this, then destroy this to enforce the singleton.
            Destroy(gameObject);
        }

        // Set this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        var mainMenuScene = SceneManager.GetSceneByName("MainMenuScene");
        var activeScene = SceneManager.GetActiveScene();

        // Check if the user is on a non-main scene and presses the Escape key
        if (mainMenuScene.name != activeScene.name && escAction.triggered)
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}