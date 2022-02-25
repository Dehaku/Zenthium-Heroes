using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    

     //Create a Damage Popup
     public static DamagePopup Create(Vector3 position, float damageAmount, int damageType, bool isCriticalHit)
     {
         Transform damagePopupTransform = Instantiate(GameAssets.i.pfDamagePopup, position, Quaternion.identity);
        DamagePopup damagePopup = damagePopupTransform.GetComponent<DamagePopup>();

        float rounded = (int)(damageAmount * 1000.0f) / 1000.0f;

        //damagePopup.Setup(Round(damageAmount,3) );
        damagePopup.Setup(rounded, damageType, isCriticalHit);

        return damagePopup;
     }

    static int sortingOrder;

    const float DISAPPEAR_TIMER_MAX = 1f;
    
    public TextMeshPro textMesh;
    public float damage;
    public int damageType;
    float disappearTimer;
    Color textColor;
    Vector3 moveVector;
    bool displayDamageTypeImage;


    private void Awake()
    {
        textMesh = transform.GetComponent<TextMeshPro>();
    }

    public void Setup(float damageAmount, int damageType, bool isCriticalHit)
    {
        displayDamageTypeImage = true;

        damage = damageAmount;
        this.damageType = damageType;

        string damageString = "";
        if(displayDamageTypeImage)
        {
            damageString += "<color=#000000> <sprite name=\"DamageIconsWhite_" + Damage.GetName(damageType) + "\" tint=1> </color>";
        }
            
        damageString += damageAmount.ToString();

        textMesh.SetText(damageString);
        disappearTimer = DISAPPEAR_TIMER_MAX;

        if(isCriticalHit)
        {
            textMesh.fontSize = textMesh.fontSize * 1.2f;
            textMesh.color = Color.red;
        }

            
        
        
        if (damageType < 0)
            textMesh.color = Color.green;

        textColor = textMesh.color;
        sortingOrder++;
        textMesh.sortingOrder = sortingOrder;
        moveVector = new Vector3(.7f, 1) * 6f;

        


    }

    Quaternion originalRotation;
    // Start is called before the first frame update
    void Start()
    {
        originalRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;

        
        if(disappearTimer > DISAPPEAR_TIMER_MAX * 0.5f)
        {
            float increaseScaleAmount = 0.1f;
            transform.localScale += Vector3.one * increaseScaleAmount * Time.deltaTime;
        }
        else
        {
            float decreaseScaleAmount = 0.1f;
            transform.localScale -= Vector3.one * decreaseScaleAmount * Time.deltaTime;
        }

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            float disappearSpeed = 3f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a < 0)
                Destroy(gameObject);
        }

        
        // Facing the camera
        transform.rotation = Camera.main.transform.rotation * originalRotation;

    }

    public void AddDamage(float damageAmount)
    {
        damage += damageAmount;
        string damageString = "";
        if (displayDamageTypeImage)
        {
            damageString += "<color=#000000> <sprite name=\"DamageIconsWhite_" + Damage.GetName(damageType) + "\" tint=1> </color>";
        }

        damageString += damage.ToString();

        textMesh.SetText(damageString);
    }

    public static float Round(float value, int digits)
    {
        float mult = Mathf.Pow(10.0f, (float)digits);
        return Mathf.Round(value * mult) / mult;
    }

}
