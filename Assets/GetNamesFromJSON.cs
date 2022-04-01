using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetNamesFromJSON : MonoBehaviour
{
    void SavePlayerData()
    {
        PlayerData pD = new PlayerData();
        pD.position = new Vector3(50, 25, 15.5f);
        pD.health = 75;

        string json = JsonUtility.ToJson(pD);
        Debug.Log(json);
        File.WriteAllText(Application.dataPath + "/saveFile.json", json);
    }

    void LoadPlayerData()
    {
        string json = File.ReadAllText(Application.dataPath + "/saveFile.json");

        PlayerData loadedPD = JsonUtility.FromJson<PlayerData>(json);
        Debug.Log(loadedPD);
        Debug.Log(loadedPD.position);
        Debug.Log(loadedPD.health);
    }

    void SaveNameData(NameData nD)
    {
        if(nD == null)
        {
            nD = new NameData();
            nD.firstNames.Add("Johnny");
            nD.firstNames.Add("Jack");
            nD.firstNames.Add("Jason");
            nD.firstNames.Add("Jennifer");

            nD.lastNames.Add("Smith");
            nD.lastNames.Add("Jackson");
            nD.lastNames.Add("Doe");
            nD.lastNames.Add("Cannon");
            nD.lastNames.Add("Tanner");
            nD.lastNames.Add("James");
        }
        

        string json = JsonUtility.ToJson(nD);
        Debug.Log(json);
        File.WriteAllText(Application.dataPath + "/Names.json", json);
    }

    NameData LoadNameData()
    {
        //string json = File.ReadAllText(Application.dataPath + "/Names.json");
        string json = File.ReadAllText(Application.dataPath + "/Resources/Misc/Names/Names.json"); 
        NameData loadedND = JsonUtility.FromJson<NameData>(json);
        return loadedND;
    }

    List<string> ConvertFileToStringList(string file)
    {
        // Apparently you don't use the file extension to hit them.
        List<string> outputNames = new List<string>();
        List<string> textArray = new List<string>();
        TextAsset textAsset;

        textAsset = Resources.Load(file) as TextAsset;
        textArray = textAsset.text.Split('\n').ToList ();
        string newString;
        foreach (var item in textArray)
        {
            if (item == "")
                continue;
            newString = item.Replace("\r", "").Replace("\n", "");
            outputNames.Add(newString);
        }

        return outputNames;
    }

    NameData NameDatabase;

    void ConvertTxtToJson()
    {
        List<string> firstNames = ConvertFileToStringList("Misc/Names/" + "NamesFirst");
        List<string> lastNames = ConvertFileToStringList("Misc/Names/" + "NamesLast");
        NameData nD = new NameData();
        nD.firstNames = firstNames;
        nD.lastNames = lastNames;
        SaveNameData(nD);
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("GetNamesFromJSON.Start");
        NameDatabase = LoadNameData();

    }

    string GetRandomFirstName()
    {
        return NameDatabase.firstNames[Random.Range(0, NameDatabase.firstNames.Count)];
    }

    string GetRandomLastName()
    {
        return NameDatabase.lastNames[Random.Range(0, NameDatabase.lastNames.Count)];
    }

    int amount = 0;
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.X))
        {
            List<string> first = new List<string>();
            List<string> last = new List<string>();
            float startTime = Time.realtimeSinceStartup;
            for(int i = 0; i != 500; i++)
            {
                first.Add(GetRandomFirstName());
                last.Add(GetRandomLastName());
            }
            //Debug.Log(GetRandomFirstName() + " " + GetRandomLastName());
            Debug.Log(((Time.realtimeSinceStartup - startTime)) + "ms" + ": " + amount);
            Debug.Log(first.Count);
            amount++;
            
        }

    }

    class NameData
    {
        public List<string> firstNames = new List<string>();
        public List<string> lastNames = new List<string>();
    }

    class PlayerData
    {
        public Vector3 position;
        public float health;
    }

}
