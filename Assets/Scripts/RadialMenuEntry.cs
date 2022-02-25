using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class RadialMenuEntry : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public delegate void RadialMenuEntryDelegate(RadialMenuEntry pEntry);


    [SerializeField]
    TextMeshProUGUI Label;

    [SerializeField]
    RawImage Icon;

    [SerializeField]
    RawImage Backer;

    RectTransform Rect;
    RadialMenuEntryDelegate Callback;

    void Start()
    {
        Rect = Icon.GetComponent<RectTransform>();

    }

    public void SetLabel(string pText)
    {
        Label.text = pText;
    }

    public void SetIcon(Texture pIcon)
    {
        Icon.texture = pIcon;
    }

    public void SetBackerRaytraceTargetOnOff(bool value)
    {
        Backer.raycastTarget = value;
    }

    public Texture GetIcon()
    {
        return (Icon.texture);
    }

    public void SetCallback(RadialMenuEntryDelegate pCallback)
    {
        Callback = pCallback;
    }

    

    public void OnPointerClick(PointerEventData eventData)
    {
        Callback?.Invoke(this);


    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Rect.DOComplete();
        Rect.DOScale(Vector3.one * 1.5f, .3f).SetEase(Ease.OutQuad);
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Rect.DOComplete();
        Rect.DOScale(Vector3.one, .3f).SetEase(Ease.OutQuad);
    }
}
