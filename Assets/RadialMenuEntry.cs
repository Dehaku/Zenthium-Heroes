using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RadialMenuEntry : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    TextMeshProUGUI Label;

    [SerializeField]
    RawImage Icon;

    public void SetLabel(string pText)
    {
        Label.text = pText;
    }

    public void SetIcon(Texture pIcon)
    {
        Icon.texture = pIcon;
    }

    public Texture GetIcon()
    {
        return (Icon.texture);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }
}
