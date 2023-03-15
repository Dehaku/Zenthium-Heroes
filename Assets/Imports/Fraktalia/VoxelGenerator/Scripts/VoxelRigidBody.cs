using Fraktalia.VoxelGen;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VoxelGenerator))]
public class VoxelRigidBody : MonoBehaviour
{
	public bool IsKinematicInGame;

	VoxelGenerator generator;
	Rigidbody rigidBody;

	private void OnDrawGizmos()
	{
		if (Application.isPlaying) return;

		if(rigidBody == null)
		{
			rigidBody = GetComponent<Rigidbody>();
		}

		rigidBody.isKinematic = true;	
	}

	private void Awake()
	{
		rigidBody = GetComponent<Rigidbody>();
		generator = GetComponent<VoxelGenerator>();
	}

	private void LateUpdate()
	{
		if (generator.IsIdle)
		{
			rigidBody.isKinematic = IsKinematicInGame;
		}
	}
}
