using UnityEngine;

public class NivyChaseCinemachineSwitcher : MonoBehaviour
{
    private Animator animator;
    private bool isMainCameraActive = true;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }


    public void SwitchCamera()
    {
        if (isMainCameraActive)
        {
            animator.Play("CloseUpCamera");
            isMainCameraActive = !isMainCameraActive;
        }
        else
        {
            animator.Play("MainCamera");
            isMainCameraActive = !isMainCameraActive;
        }
    }
}
