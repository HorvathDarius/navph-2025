using System.Collections;
using UnityEngine;

public class NivyChaseManager : MonoBehaviour
{
    public static NivyChaseManager Instance { get; private set; }

    [SerializeField] private NivyChaseCinemachineSwitcher cinemachineSwitcher;
    [SerializeField] private GameObject quickTimeEventUI;

    private void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {

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
        Time.timeScale = 1f;
        quickTimeEventUI.SetActive(false);
        cinemachineSwitcher.SwitchCamera();
        yield return new WaitForSeconds(1.1f);
    }
}
