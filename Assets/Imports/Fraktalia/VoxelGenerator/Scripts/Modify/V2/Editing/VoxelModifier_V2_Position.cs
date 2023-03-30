using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Fraktalia.VoxelGen.Modify
{


	public class VoxelModifier_V2_Position : MonoBehaviour
	{
		public Transform position;

		public VoxelModifier_V2 Modifier;

		[Header("Game Settings:")]
		public Camera GameCamera;
		public Transform ImpactIndicator;
		public LayerMask TargetLayer = -1;
		public float MaximumDistance;
		public KeyCode ActivationButton = KeyCode.LeftControl;

		[Header("Editor Settings:")]
		public bool ReliefPainting = false;
		public bool Paint2D = false;
		public bool PaintPlaneXY = false;
		public bool PaintPlaneXZ = false;
		public bool PaintPlaneYZ = false;
		public float PaintPlaneOffset = 0;
		public float PaintPlaneSize = 2000;
		public Color PaintPlaneColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		public Vector3 PaintPosition;

		public VoxelModifierMode ModeLeftClick = VoxelModifierMode.Additive;
		public VoxelModifierMode ModeRightClick = VoxelModifierMode.Subtractive;
		public VoxelModifierMode ModeMiddletClick = VoxelModifierMode.Smooth;

		[HideInInspector]
		public Vector3 PaintNormal;
		private void OnDrawGizmosSelected()
		{
			if (Modifier) Modifier.DrawEditorPreview(PaintPosition, PaintNormal);
			else Modifier = GetComponent<VoxelModifier_V2>();
		}


		void EditPosition(VoxelModifierMode vm)
        {

			if (Modifier)
			{
				var mode = Modifier.Mode;
				mode = vm; // Override
				Modifier.ApplyVoxelModifier(position.position);
				Modifier.Mode = mode;
			}
			else Modifier = GetComponent<VoxelModifier_V2>();
		}


		public VoxelModifierMode EditMode = VoxelModifierMode.Subtractive;
		public VoxelModifierMode EditAltMode = VoxelModifierMode.Additive;
		public bool Edit = false;
		public bool EditAlt = false;

		public bool EditAlwaysOn = false;
		public bool EditAltAlwaysOn = false;

		public float EditTimer = 0.05f;
		float _editTimer = 0;
		public float EditAltTimer = 0.05f;
		float _editAltTimer = 0;

		private void Update()
		{
			if (!Modifier || !GameCamera) return;


			if (EditAlwaysOn)
            {
				_editTimer -= Time.deltaTime;
				if(_editTimer < 0)
                {
					Edit = true;
				}
					
			}

			if (EditAltAlwaysOn)
			{
				_editAltTimer -= Time.deltaTime;
				if (_editAltTimer < 0)
				{
					EditAlt = true;
				}

			}

			if (Edit)
            {
				Edit = false;
				EditPosition(EditMode);
            }
			if (EditAlt)
			{
				EditAlt = false;
				EditPosition(EditAltMode);
			}

			if (Input.GetKey(ActivationButton) || ActivationButton == KeyCode.None)
			{
				
				Ray ray = GameCamera.ScreenPointToRay(Input.mousePosition);
				if (RayCastToVoxel(ray, MaximumDistance, TargetLayer))
				{
					if (ImpactIndicator)
					{
						ImpactIndicator.position = PaintPosition;
						ImpactIndicator.gameObject.SetActive(true);
						ImpactIndicator.localScale = Modifier.GetGameIndicatorSize();
					}
					if(Input.GetMouseButton(0))
					{
						ApplyModifier(ModeLeftClick);
					}

					if (Input.GetMouseButton(1))
					{
						ApplyModifier(ModeRightClick);
					}

					if (Input.GetMouseButton(2))
					{
						ApplyModifier(ModeMiddletClick);
					}
				}
				else
				{
					if (ImpactIndicator) ImpactIndicator.gameObject.SetActive(false);
				}
			}
			else
			{
				if (ImpactIndicator) ImpactIndicator.gameObject.SetActive(false);
			}

		}


		public bool RayCastToVoxel(Ray ray, float distance, LayerMask mask)
		{
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, distance, mask.value))
			{
				PaintPosition = hit.point;
				PaintNormal = hit.normal;
				return true;
			}
			return false;
		}

		public bool RayCastToSurface(Ray ray, Plane surface, float distance)
		{
			float enter;
			if (surface.Raycast(ray, out enter))
			{
				PaintPosition = ray.GetPoint(enter);
				PaintNormal = surface.normal;
				return true;
			}
			return false;
		}

		public void ApplyModifier()
		{
			if (Modifier) Modifier.ApplyVoxelModifier(PaintPosition);
			else Modifier = GetComponent<VoxelModifier_V2>();
		}

		public void ApplyModifier(VoxelModifierMode modeoverride)
		{
			if (Modifier)
			{
				var mode = Modifier.Mode;
				Modifier.Mode = modeoverride;
				Modifier.ApplyVoxelModifier(PaintPosition);
				Modifier.Mode = mode;
			}
			else Modifier = GetComponent<VoxelModifier_V2>();
		}
	}




}
