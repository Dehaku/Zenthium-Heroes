using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLock : MonoBehaviour
{
    private static MouseLock _instance;

    public static MouseLock Instance { get { return _instance; } }


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public static void LockMouse(bool MakeCursorVisible = false)
    {
        Cursor.lockState = CursorLockMode.Locked;

        if(MakeCursorVisible)
            Cursor.visible = true;
        else
            Cursor.visible = false;
    }

    public static void ConfineMouse(bool MakeCursorVisible = false)
    {
        Cursor.lockState = CursorLockMode.Confined;

        if (MakeCursorVisible)
            Cursor.visible = true;
        else
            Cursor.visible = false;
    }

    public static void UnlockMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
