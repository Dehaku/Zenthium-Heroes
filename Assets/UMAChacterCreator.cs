using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UMA;
using UMA.CharacterSystem;
using UnityEngine.UI;
using System.IO;

public class UMAChacterCreator : MonoBehaviour
{
    public DynamicCharacterAvatar avatar;
    public Slider heightSlider;
    public Slider bellySlider;
    
    Dictionary<string, DnaSetter> dna;

    public List<string> hairModelsMale = new List<string>();
    public List<string> hairModelsFemale = new List<string>();
    int currentHairFemale = 0;
    int currentHairMale = 0;

    public string myRecipe;

    // https://www.youtube.com/watch?v=3uPrkH59Q0c&list=PLkDHFObfS19zFVfbrfB14P-u5QJJQyvtP&index=10
    // Comments include code for automating slider production, instead of doing tons of samey lines.

    private void OnEnable()
    {
        avatar.CharacterUpdated.AddListener(CharacterUpdated);
        heightSlider.onValueChanged.AddListener(HeightChange);
        bellySlider.onValueChanged.AddListener(BellyChange);
    }

    private void OnDisable()
    {
        avatar.CharacterUpdated.RemoveListener(CharacterUpdated);
        heightSlider.onValueChanged.RemoveListener(HeightChange);
        bellySlider.onValueChanged.RemoveListener(BellyChange);
    }

    void CharacterUpdated(UMAData data)
    {
        dna = avatar.GetDNA();
        heightSlider.value = dna["height"].Get();
        bellySlider.value = dna["belly"].Get();
    }

    public void SwitchGender(bool male)
    {
        if(male && avatar.activeRace.name != "HumanMale")
        {
            avatar.ChangeRace("HumanMale");
        }
        if (!male && avatar.activeRace.name != "HumanFemale")
        {
            avatar.ChangeRace("HumanFemale");
        }
    }

    public void HeightChange(float value)
    {
        dna["height"].Set(value);
        avatar.BuildCharacter();
    }

    public void BellyChange(float value)
    {
        dna["belly"].Set(value);
        avatar.BuildCharacter();
    }

    public void ChangeSkinColor(Color col)
    {
        avatar.SetColor("Skin", col);
        avatar.UpdateColors(true);

        Debug.Log(avatar.GetColor("Skin").color);
    }

    public void ChangeHair(bool plus)
    {
        if(avatar.activeRace.name == "HumanFemale")
        {
            if (plus)
            {
                currentHairFemale++;
            }
            else
                currentHairFemale--;

            // Wrapping for convenience.
            if (currentHairFemale < 0)
                currentHairFemale = hairModelsFemale.Count - 1;
            if (currentHairFemale > hairModelsFemale.Count - 1)
                currentHairFemale = 0;

            currentHairFemale = Mathf.Clamp(currentHairFemale, 0, hairModelsFemale.Count - 1);

            if (hairModelsFemale[currentHairFemale] == "None")
                avatar.ClearSlot("Hair");
            else
                avatar.SetSlot("Hair", hairModelsFemale[currentHairFemale]);
            avatar.BuildCharacter();

            //Debug.Log(avatar.GetWardrobeItemName("Hair"));
        }

        else if (avatar.activeRace.name == "HumanMale")
        {
            if (plus)
            {
                currentHairMale++;
            }
            else
                currentHairMale--;

            // Wrapping for convenience.
            if (currentHairMale < 0)
                currentHairMale = hairModelsMale.Count - 1;
            if (currentHairMale > hairModelsMale.Count - 1)
                currentHairMale = 0;

            currentHairMale = Mathf.Clamp(currentHairMale, 0, hairModelsMale.Count - 1);

            if (hairModelsMale[currentHairMale] == "None")
                avatar.ClearSlot("Hair");
            else
                avatar.SetSlot("Hair", hairModelsMale[currentHairMale]);
            avatar.BuildCharacter();

            //Debug.Log(avatar.GetWardrobeItemName("Hair"));
        }

    }

    public void SaveRecipe()
    {
        myRecipe = avatar.GetCurrentRecipe();
        string path = Application.persistentDataPath + "/charRecipe.txt";
        File.WriteAllText(path,myRecipe);
        Debug.Log(path);
    }

    public void LoadRecipe()
    {
        myRecipe = File.ReadAllText(Application.persistentDataPath + "/charRecipe.txt");
        avatar.ClearSlots();
        avatar.LoadFromRecipeString(myRecipe);
    }


}
