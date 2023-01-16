using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManagerUIInfo : MonoBehaviour
{
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI stateText;
    
    public PlayerMovementAdvanced pm;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        speedText.text = "Speed: " + pm.GetVelocity().ToString("F2") + " : " +pm.MoveSpeed.ToString("F2") + " : " + pm.DesiredMoveSpeed.ToString("F2");
        stateText.text = "State: " + pm.state.ToString();
    }
}
