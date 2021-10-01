using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCreator : MonoBehaviour
{
    [Range(0, 100)]
    public float chestBulk = 50;
    float prevChestBulk = 100;
    [Range(0, 100)]
    float Slimness = 0;

    public bool prevHair = false;
    public bool nextHair = false;
    public int currentHair = 0;

    public bool prevHead = false;
    public bool nextHead = false;
    public int currentHead = 1;

    bool changeTorso = false;
    bool thickTorso = true;

    public bool leftFootBoot = false;
    public bool rightFootBoot = false;

    public bool changeLeftFoot = false;
    public bool changeRightFoot = false;


    [Space]
    public GameObject[] hairs;
    public GameObject[] heads;
    [Space]
    public GameObject TorsoSlim;
    public GameObject TorsoSlimCleavage;
    public GameObject TorsoSlimArmLeft;
    public GameObject TorsoSlimArmRight;
    [Space]
    public GameObject TorsoThick;
    public GameObject TorsoThickCollar;
    public GameObject TorsoThickArmLeft;
    public GameObject TorsoThickArmRight;
    [Space]
    public GameObject Legs;
    public GameObject ShinUpperLeft;
    public GameObject ShinUpperRight;
    [Space]
    public GameObject BootLeft;
    public GameObject ShoeLeft;
    public GameObject ShinLowerLeft;
    [Space]
    public GameObject BootRight;
    public GameObject ShoeRight;
    public GameObject ShinLowerRight;





    // Start is called before the first frame update
    void Start()
    {
        ChangeHair(1);
        ChangeHead(1);

        ChangeTorso();

        ChangeLeftFoot();
        ChangeRightFoot();
    }

    void ChangeLeftFoot()
    {
        changeLeftFoot = false;

        if(leftFootBoot)
        {
            BootLeft.SetActive(false);
            ShoeLeft.SetActive(true);
            ShinLowerLeft.SetActive(true);
        }
        else
        {
            BootLeft.SetActive(true);
            ShoeLeft.SetActive(false);
            ShinLowerLeft.SetActive(false);
        }
    }

    void ChangeRightFoot()
    {
        changeRightFoot = false;

        if (rightFootBoot)
        {
            BootRight.SetActive(false);
            ShoeRight.SetActive(true);
            ShinLowerRight.SetActive(true);
        }
        else
        {
            BootRight.SetActive(true);
            ShoeRight.SetActive(false);
            ShinLowerRight.SetActive(false);
        }
    }

    void ChangeTorso()
    {
        changeTorso = false;

        TorsoThick.SetActive(false);
        TorsoThickCollar.SetActive(false);
        TorsoThickArmLeft.SetActive(false);
        TorsoThickArmRight.SetActive(false);

        TorsoSlim.SetActive(false);
        TorsoSlimCleavage.SetActive(false);
        TorsoSlimArmLeft.SetActive(false);
        TorsoSlimArmRight.SetActive(false);

        

        if (thickTorso)
        {
            TorsoThick.SetActive(true);
            TorsoThickCollar.SetActive(true);
            TorsoThickArmLeft.SetActive(true);
            TorsoThickArmRight.SetActive(true);
        }
        else
        {
            TorsoSlim.SetActive(true);
            TorsoSlimCleavage.SetActive(true);
            TorsoSlimArmLeft.SetActive(true);
            TorsoSlimArmRight.SetActive(true);
        }

    }

    void ChangeHair(int direction)
    {
        nextHair = false;
        prevHair = false;

        int oldHair = currentHair;

        if(direction > 0)
        {
            currentHair += 1;
            if (currentHair >= hairs.Length)
                currentHair = 0;
        }
        if (direction < 0)
        {
            currentHair -= 1;
            if (currentHair < 0)
                currentHair = hairs.Length-1;
        }

        hairs[oldHair].SetActive(false);
        hairs[currentHair].SetActive(true);

        // Current hardcode method for handling head types and the bald hair matching method.
        // Head needs to be blended instead of seperate models.
        if (heads[currentHead].GetComponent<TagSlim>())
        {
            if (hairs[currentHair].GetComponent<SkinnedMeshRenderer>().sharedMesh.blendShapeCount > 0)
                hairs[currentHair].GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(0, 100);
        }
        else
        {
            if (hairs[currentHair].GetComponent<SkinnedMeshRenderer>().sharedMesh.blendShapeCount > 0)
                hairs[currentHair].GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(0, 0);
        }

        


    }

    void ChangeHead(int direction)
    {
        thickTorso = !thickTorso;

        nextHead = false;
        prevHead = false;

        int oldHead = currentHead;

        if (direction > 0)
        {
            currentHead += 1;
            if (currentHead >= heads.Length)
                currentHead = 0;
        }
        if (direction < 0)
        {
            currentHead -= 1;
            if (currentHead < 0)
                currentHead = heads.Length - 1;
        }

        heads[oldHead].SetActive(false);
        heads[currentHead].SetActive(true);

        if (heads[currentHead].GetComponent<TagSlim>())
            Slimness = 100;

        if (heads[currentHead].GetComponent<SkinnedMeshRenderer>().sharedMesh.blendShapeCount > 0)
            heads[currentHead].GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(0, Slimness);
    }

    void SetChestBulk(float chestBulk)
    {
        if (thickTorso)
            return;
        var chestRend = TorsoSlim.GetComponent<SkinnedMeshRenderer>();
        var cleavageRend = TorsoSlimCleavage.GetComponent<SkinnedMeshRenderer>();
        
        if (chestRend == null || cleavageRend == null)
            return;
        if (chestRend.sharedMesh.blendShapeCount < 2 || cleavageRend.sharedMesh.blendShapeCount < 2)
            return;


        float chestMax = 0;
        float chestMin = 0;
        
        if(chestBulk > 50)
        {
            chestMax = (chestBulk - 50)*2;
        }
        if(chestBulk < 50)
        {
            chestMin = (-chestBulk + 50) * 2;
        }
            

        chestRend.SetBlendShapeWeight(0, chestMax);
        chestRend.SetBlendShapeWeight(1, chestMin);

        cleavageRend.SetBlendShapeWeight(0, chestMax);
        cleavageRend.SetBlendShapeWeight(1, chestMin);

        //ChestMax
        //ChestMin

    }

    // Update is called once per frame
    void Update()
    {
        if (prevHair)
            ChangeHair(-1);
        if (nextHair)
            ChangeHair(1);
        if (prevHead)
        {
            ChangeHead(-1);
            ChangeTorso();
        }
            
        if (nextHead)
        {
            ChangeHead(1);
            ChangeTorso();
        }
            
        if (changeTorso)
        {
            thickTorso = !thickTorso;
            ChangeTorso();
        }
            

        if (changeLeftFoot)
        {
            leftFootBoot = !leftFootBoot;
            ChangeLeftFoot();
        }
            
        if (changeRightFoot)
        {
            rightFootBoot = !rightFootBoot;
            ChangeRightFoot();
        }
            
        if(chestBulk != prevChestBulk)
        {
            prevChestBulk = chestBulk;
            SetChestBulk(chestBulk);
        }
    }
}
