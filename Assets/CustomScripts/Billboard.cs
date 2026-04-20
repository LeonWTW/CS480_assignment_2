using UnityEngine;
 
/// <summary>
/// Billboard - Makes a GameObject (typically a world-space UI canvas)
/// always face the main camera. Used for the DANGER warning floating above JohnLemon's head.
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera mainCam;
 
    void Start()
    {
        mainCam = Camera.main;
    }
 
    void LateUpdate()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            return;
        }
 
        // Make this object face the same direction as the camera
        transform.forward = mainCam.transform.forward;
    }
}