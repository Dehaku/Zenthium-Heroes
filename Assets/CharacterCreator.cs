using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCreator : MonoBehaviour
{
    public GameObject[] hairs;
    public GameObject[] heads;

    [Range(0,100)]
    public float Slimness = 0;

    public bool prevHair = false;
    public bool nextHair = false;
    public int currentHair = 0;

    public bool prevHead = false;
    public bool nextHead = false;
    public int currentHead = 0;

    // Start is called before the first frame update
    void Start()
    {
        ChangeHair(1);
        ChangeHead(1);
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

        // Current hardwire method for handling head types and the bald hair matching method.
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


    // Update is called once per frame
    void Update()
    {
        if (prevHair)
            ChangeHair(-1);
        if (nextHair)
            ChangeHair(1);
        if (prevHead)
            ChangeHead(-1);
        if (nextHead)
            ChangeHead(1);

    }
}
