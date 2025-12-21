using System.Collections;
using UnityEngine;

public class NivyChaseManager : MonoBehaviour
{
    public static NivyChaseManager Instance { get; private set; }

    [SerializeField] private NivyChaseCinemachineSwitcher cinemachineSwitcher;
    [SerializeField] private GameObject quickTimeEventUI;
    [SerializeField] public GameObject homelessMan;
    [SerializeField] private PlayerController playerController;

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator StartQuickTimeEvent()
    {
        cinemachineSwitcher.SwitchCamera();
        yield return new WaitForSeconds(1.1f);
        Time.timeScale = 0f;
        quickTimeEventUI.SetActive(true);
    }

    public IEnumerator ResumeGame()
    {
        homelessMan.transform.position -= new Vector3(4f, 0f, 0f);
        Time.timeScale = 1f;
        quickTimeEventUI.SetActive(false);
        cinemachineSwitcher.SwitchCamera();
        homelessMan.GetComponent<ChaserController>().isChasing = true;
        homelessMan.GetComponent<Animator>().Play("homeless_roll_R");

        if (GameManager.Instance.Health <= 0)
        {
            playerController.KillPlayer();
            homelessMan.GetComponent<ChaserController>().isChasing = false;
        }

        yield return new WaitForSeconds(1.1f);
    }
}
