using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManagerUIInfo : MonoBehaviour
{
    public TextMeshProUGUI stateText;
    public PlayerMovementAdvanced pm;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        stateText.text = "State: " + pm.state.ToString();
    }
}
