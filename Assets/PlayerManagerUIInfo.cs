using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManagerUIInfo : MonoBehaviour
{
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI climbTimeText;
    public TextMeshProUGUI wallrunTimeText;
    public TextMeshProUGUI dashTimeText;
    public TextMeshProUGUI slideTimeText;

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
        
        climbTimeText.text = pm.GetComponent<Climbing>().climbJumpsLeft + "ClimbT: " + pm.GetComponent<Climbing>().ClimbTimer.ToString("F2");
        wallrunTimeText.text = pm.GetComponent<WallRunning>().wallRunJumpsLeft + "WallrunT: " + pm.GetComponent<WallRunning>().WallRunTimer.ToString("F2");
        dashTimeText.text = pm.GetComponent<Dashing>().dashsLeft + "DashT: " + pm.GetComponent<Dashing>().DashCdTimer.ToString("F2");
        slideTimeText.text = "SlideT: " + pm.GetComponent<Sliding>().SlideTimer.ToString("F2");

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
