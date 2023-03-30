using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        MouseLock.LockMouse();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                MouseLock.UnlockMouse();
            else if (Cursor.lockState == CursorLockMode.None)
                MouseLock.LockMouse();
        }
    }
}
