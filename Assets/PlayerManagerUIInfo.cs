using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManagerUIInfo : MonoBehaviour
{
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI stateText;
    
    public PlayerMovementAdvanced pm;

    public float speedAverageCount = 10;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        CalculateSpeedAverages();
        string momentStr = "";
        if (pm.keepMomentum)
            momentStr = "(M)";
        speedText.text = "Speed: " + GetSpeedAverage().ToString("F2") + " : " +pm.MoveSpeed.ToString("F2") + " : " + pm.DesiredMoveSpeed.ToString("F2") + momentStr;
        stateText.text = "State: " + pm.state.ToString();
    }

    List<float> speedAverages = new List<float>();
    void CalculateSpeedAverages()
    {
        if (!pm)
            return; 

        speedAverages.Add(pm.GetVelocity());

        if (speedAverages.Count > speedAverageCount)
            speedAverages.RemoveAt(0);
    }

    public float GetSpeedAverage()
    {
        float sum = 0;
        foreach (var item in speedAverages)
        {
            sum += item;
        }

        return sum / speedAverages.Count;
    }
}
