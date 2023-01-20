using System.Collections;

using System.Collections.Generic;

using UnityEngine;



public class GrapplingRopeSpin : MonoBehaviour

{

    private List<Spring> spring = new List<Spring>();

    public List<LineRenderer> lineRenderers;

    public List<Vector3> currentGrapplePositions;

    public DualHooks grapplingGun;

    public int quality;

    public float damper;

    public float strength;

    public float velocity;

    public float waveCount;

    public float waveHeight;

    public AnimationCurve affectCurve;

    private float delta;

    public float debugSpeedGrappleThing = 12;


    void Awake()

    {

        //lr = GetComponent<LineRenderer>();

        spring.Add(new Spring());
        spring.Add(new Spring());
        spring[0].SetTarget(0);
        spring[1].SetTarget(0);

        currentGrapplePositions.Add(Vector3.zero);
        currentGrapplePositions.Add(Vector3.zero);

    }



    //Called after Update

    void LateUpdate()

    {
        DrawRope();

    }



    void DrawRope()

    {
        for (int i = 0; i < grapplingGun.amountOfSwingPoints; i++)
        {
            // if not grappling, don't draw rope
            if (!grapplingGun.grapplesActive[i] && !grapplingGun.swingsActive[i])
            
            {
                currentGrapplePositions[i] = grapplingGun.gunTips[i].position;

                spring[i].Reset();

                if (lineRenderers[i].positionCount > 0)

                    lineRenderers[i].positionCount = 0;

                continue;
            }

            if (lineRenderers[i].positionCount == 0)

            {

                spring[i].SetVelocity(velocity);

                lineRenderers[i].positionCount = quality + 1;

            }





            spring[i].SetDamper(damper);

            spring[i].SetStrength(strength);

            spring[i].Update(Time.deltaTime);



            //var grapplePoint = grapplingGun.swingPoints[i];
            var grapplePoint = grapplingGun.connectionPoints[i].position;

            var gunTipPosition = grapplingGun.gunTips[i].position;

            var up = Quaternion.LookRotation((grapplePoint - gunTipPosition).normalized) * Vector3.up;



            currentGrapplePositions[i] = Vector3.Lerp(currentGrapplePositions[i], grapplePoint, Time.deltaTime * debugSpeedGrappleThing);



            for (var t = 0; t < quality + 1; t++)

            {

                var right = Quaternion.LookRotation((grapplePoint - gunTipPosition).normalized) * Vector3.right;

                var delta = t / (float)quality;

                var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * spring[i].Value *

                             affectCurve.Evaluate(delta) +

                             right * waveHeight * Mathf.Cos(delta * waveCount * Mathf.PI) * spring[i].Value *

                             affectCurve.Evaluate(delta);



                lineRenderers[i].SetPosition(t, Vector3.Lerp(gunTipPosition, currentGrapplePositions[i], delta) + offset);

            }


        }
    }

}