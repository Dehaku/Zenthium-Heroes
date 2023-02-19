using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UMA;
using UMA.CharacterSystem;


public class UMAFiddler : MonoBehaviour
{
    DynamicCharacterAvatar avatar;
    Dictionary<string, DnaSetter> dna;

    // Start is called before the first frame update
    void Start()
    {
        avatar = GetComponent<DynamicCharacterAvatar>();
        // Setup listener for completion
        // dna = avatar.GetDNA();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            if(dna == null)
            {
                dna = avatar.GetDNA();
            }

            dna["headSize"].Set(1f);
            avatar.BuildCharacter();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (dna == null)
            {
                dna = avatar.GetDNA();
            }

            dna["headSize"].Set(0.5f);
            avatar.BuildCharacter();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            avatar.SetSlot("Legs", "FemalePants1");
            avatar.BuildCharacter();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            avatar.ClearSlot("Legs");
            avatar.BuildCharacter();
        }


        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            avatar.SetColor("Skin", Color.blue, default, 1);
            avatar.BuildCharacter();
        }
    }
}
