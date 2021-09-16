using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class RadialMenu : MonoBehaviour
{
    [SerializeField]
    GameObject EntryPrefab;

    [SerializeField]
    float Radius = 300f;

    [SerializeField]
    List<Texture> Icons;

    [SerializeField]
    RawImage TargetIcon;
    List<RadialMenuEntry> Entries;


    // Start is called before the first frame update
    void Start()
    {
        Entries = new List<RadialMenuEntry>();
    }

    void AddEntry(string pLabel, Texture pIcon, RadialMenuEntry.RadialMenuEntryDelegate pCallback)
    {
        GameObject entry = Instantiate(EntryPrefab, transform);

        RadialMenuEntry rme = entry.GetComponent<RadialMenuEntry>();
        rme.SetLabel(pLabel);
        rme.SetIcon(pIcon);
        rme.SetCallback(pCallback);

        Entries.Add(rme);

    }


    public void Open()
    {
        for(int i =0; i< 7; i++)
        {
            AddEntry("Button" + i.ToString(), Icons[i], SetTargetIcon);
        }
        Rearrange();
    }

    public void Close()
    {
        for (int i = 0; i < 7; i++)
        {
            RectTransform rect = Entries[i].GetComponent<RectTransform>();
            GameObject entry = Entries[i].gameObject;
            Entries[i].SetBackerRaytraceTargetOnOff(false);

            rect.DOScale(Vector3.zero, .29f).SetEase(Ease.OutQuad);
            rect.DOAnchorPos(Vector3.zero, .3f).SetEase(Ease.OutQuad).onComplete = 
                delegate()
                {
                    Destroy(entry);
                };
        }
        Entries.Clear();
    }

    public void Toggle()
    {
        if(Entries.Count == 0)
        {
            Open();
        }
        else
        {
            Close();
        }
    }

    void Rearrange()
    {
        float radiansOfSeperation = (Mathf.PI * 2) / Entries.Count;
        for(int i = 0; i < Entries.Count; i++)
        {
            float x = Mathf.Sin(radiansOfSeperation * i) * Radius;
            float y = Mathf.Cos(radiansOfSeperation * i) * Radius;
            RectTransform rect = Entries[i].GetComponent<RectTransform>();

            rect.localScale = Vector3.zero;
            rect.DOScale(Vector3.one, .3f).SetEase(Ease.OutQuad).SetDelay(.05f * i);
            rect.DOAnchorPos(new Vector3(x, y, 0), .3f).SetEase(Ease.OutQuad).SetDelay(.05f * i);
        }
    }

    void SetTargetIcon(RadialMenuEntry pEntry)
    {
        TargetIcon.texture = pEntry.GetIcon();
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
