using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowFPS : MonoBehaviour
{
    public float timer, refresh, avgFramerate;
    public string display = "{0} FPS";
    public Text m_Text;
    public RawImage Backer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float timelapse = Time.smoothDeltaTime;
        timer = timer <= 0 ? refresh : timer -= timelapse;

        if (timer <= 0) avgFramerate = (int)(1f / timelapse);
        m_Text.text = string.Format(display, avgFramerate.ToString());

        if(Backer != null)
        {
            if (avgFramerate < 30)
                Backer.color = Color.red;
            else if (avgFramerate < 60)
                Backer.color = Color.yellow;
            else if (avgFramerate < 100)
                Backer.color = Color.green;
            else
                Backer.color = Color.white;
        }
        
    }
}
