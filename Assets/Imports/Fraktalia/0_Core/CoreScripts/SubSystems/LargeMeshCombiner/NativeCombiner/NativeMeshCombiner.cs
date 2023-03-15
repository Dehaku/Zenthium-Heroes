using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Fraktalia.Core.ProceduralUVCreator;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Fraktalia.Core.LMS
{
    public class NativeMeshCombiner : MonoBehaviour
    {
        [Tooltip("If true, the generated mesh is not saved into the scene file! " +
            "This can reduce loading and file size massively! " +
            "If true, the generation must be rebuild during play mode. " +
            "Even if the structure is visible in the editor, it will not be visible in playmode " +
            "unless it is rebuilt.")]
        public bool NoSceneSaving = false;
        public bool HideInHierachy = true;

        [Tooltip("If true, generated meshes have collision. Causes most performance impact!")]
        public bool HasCollision = false;
        public bool ConvexCollider = false;
        public bool isTrigger = false;
       

        public Material currentMaterial;
		public bool CreateBarycentricColors;

		[Tooltip("If true, every slot uses a different material.")]
		public bool UseSeperateMaterials;
		public Material[] SeperateMaterials = new Material[0];

        public List<NativeMeshPiece> pieces;

        public List<NativeMeshBuffer> buffers;
		public NativeTangentsCalculator_FirstStep tangentcalc_first;
		public NativeTangentsCalculator_SecondStep tangentcalc_second;

		public NativeBarycentricCalculator barycentriccalc;

		private bool requiresfullAssignment;
		public void CreateMeshFromNative(ref NativeArray<Vector3> vertices, ref NativeArray<int> triangles, ref NativeArray<Vector2> uvs,
	ref NativeArray<Vector4> tangents, ref NativeArray<Vector3> normals, int slot = 0, bool needTangents = false)
		{
			if (ErrorHandling()) return;
#if UNITY_EDITOR
			if (!Application.isPlaying && !NoSceneSaving) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif

			NativeUVModifier[] uvModifiers = GetComponents<NativeUVModifier>();
			for (int i = 0; i < uvModifiers.Length; i++)
			{
				uvModifiers[i].LaunchAlgorithm(ref vertices, ref uvs);
			}

			if (uvModifiers.Length > 0 || needTangents)
			{
				tangentcalc_first.vertices = vertices;	
				tangentcalc_first.uv = uvs;
				tangentcalc_first.triangles = triangles;
				tangentcalc_first.Initialize();
				tangentcalc_first.Schedule(triangles.Length/3, triangles.Length/3/SystemInfo.processorCount).Complete();
				tangentcalc_second.normals = normals;
				tangentcalc_second.tan1 = tangentcalc_first.tan1;
				tangentcalc_second.tan2 = tangentcalc_first.tan2;
				tangentcalc_second.tangents = tangents;
				tangentcalc_second.Schedule(vertices.Length, vertices.Length / SystemInfo.processorCount).Complete();
				tangentcalc_first.CleanUp();
			}

			requiresfullAssignment = false;
			while (pieces.Count <= slot)
			{
				pieces.Add(null);
				buffers.Add(new NativeMeshBuffer(0, 0));
				requiresfullAssignment = true;
			}
			if (!pieces[slot])
			{
				pieces[slot] = CreateMeshPiece("__NATIVEMESHPIECE__", slot);
				requiresfullAssignment = true;
			}

			if (UseSeperateMaterials)
			{
				if (slot < SeperateMaterials.Length)
				{
					pieces[slot].meshrenderer.sharedMaterial = SeperateMaterials[slot];
				}
				else
				{
					pieces[slot].meshrenderer.sharedMaterial = currentMaterial;
				}
			}

			if (buffers == null) buffers = new List<NativeMeshBuffer>();
			while (buffers.Count <= slot)
			{

				buffers.Add(new NativeMeshBuffer(0, 0));
				requiresfullAssignment = true;
			}

			NativeMeshBuffer buffer = buffers[slot];
			if (buffers[slot].vertices.Length != vertices.Length || buffer.triangles.Length != triangles.Length)
			{
				buffer.Resize(vertices.Length, triangles.Length);
				requiresfullAssignment = true;
			}

			Mesh mesh = pieces[slot].meshfilter.sharedMesh;
			if (mesh == null)
			{
				mesh = new Mesh();
				mesh.MarkDynamic();
				mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
				pieces[slot].meshfilter.sharedMesh = mesh;
			}



			if (mesh.vertexCount != vertices.Length)
			{
				requiresfullAssignment = true;
			}

			if (requiresfullAssignment)
			{

				buffer.vertices = vertices;
				triangles.CopyTo(buffer.triangles);
				buffer.uvs = uvs;
				buffer.tangents = tangents;
				buffer.normals = normals;
				buffer.ApplyToMesh(mesh);
			}
			else
			{
				buffer.vertices = vertices;
				buffer.uvs = uvs;
				buffer.tangents = tangents;
				buffer.normals = normals;
				buffer.ApplyVertices(mesh);
			}

			if (HasCollision)
			{
				if(pieces[slot].meshfilter.sharedMesh.vertexCount > 0)
				{
					pieces[slot].meshcollider.enabled = false;
					pieces[slot].meshcollider.sharedMesh = pieces[slot].meshfilter.sharedMesh;
					pieces[slot].meshcollider.enabled = true;
				}
				else
				{
					pieces[slot].meshcollider.enabled = false;
					pieces[slot].meshcollider.sharedMesh = null;
					pieces[slot].meshcollider.enabled = true;
				}
			}

			if (CreateBarycentricColors)
			{
				barycentriccalc.vertices = vertices;
				barycentriccalc.triangles = triangles;
				barycentriccalc.Initialize();
				barycentriccalc.Schedule(triangles.Length / 3, triangles.Length / 3 / 9).Complete();
				mesh.colors = (barycentriccalc.colorArray.ToArray());
				barycentriccalc.CleanUp();
			}
			else
			{
				mesh.colors = new Color[0];
			}





			pieces[slot].UpdateAttachments();

			var additionalUVs = GetComponentsInChildren<ProceduralUVGenerator>();
			for (int i = 0; i < additionalUVs.Length; i++)
			{
				ProceduralUVGenerator modifier = additionalUVs[i];

				if (modifier.TargetSpecificSlot && !modifier.TargetSlots.Contains(slot))
					continue;

				modifier.Initialize(mesh, pieces[slot].transform);
				modifier.ApplyFast(buffer.vertices);

			}
		}

        public void CreateMeshOnly(ref NativeArray<Vector3> vertices, ref NativeArray<int> triangles, ref NativeArray<Vector2> uvs,
 ref NativeArray<Vector4> tangents, ref NativeArray<Vector3> normals, ref Mesh mesh)
        {
			bool requiresfullAssignment = false;

            if (mesh.vertexCount != vertices.Length)
            {
                requiresfullAssignment = true;
            }

            if (requiresfullAssignment)
            {

                if (mesh.vertexCount <= vertices.Length)
                {
                    mesh.SetVertices(vertices);
                    mesh.triangles = triangles.ToArray();
                }
                else
                {
                    mesh.triangles = triangles.ToArray();
					mesh.SetVertices(vertices);

				}

                mesh.SetUVs(0 ,uvs);
                mesh.SetNormals(normals);
                mesh.SetTangents(tangents);

            }
            else
            {
                mesh.vertices = vertices.ToArray();
            }
            mesh.RecalculateBounds();
        }

		public void ApplyAdditionalUVs(ref NativeArray<Vector2> uvs2, ref NativeArray<Vector2> uvs3, ref NativeArray<Vector2> uvs4, ref NativeArray<Vector2> uvs5,
ref NativeArray<Vector2> uvs6, ref NativeArray<Vector2> uvs7, ref NativeArray<Vector2> uvs8, int slot = 0)
		{

			Mesh mesh = pieces[slot].meshfilter.sharedMesh;

			if (uvs2.IsCreated)
				mesh.SetUVs(1, uvs2);

			if (uvs3.IsCreated)
				mesh.SetUVs(2, uvs3);

			if (uvs4.IsCreated)
				mesh.SetUVs(3, uvs4);

			if (uvs5.IsCreated)
				mesh.SetUVs(4, uvs5);

			if (uvs6.IsCreated)
				mesh.SetUVs(5, uvs6);

			if (uvs7.IsCreated)
				mesh.SetUVs(6, uvs7);

			if (uvs8.IsCreated)
				mesh.SetUVs(7, uvs8);
		}

		public void ApplyAdditionalUV(NativeArray<Vector2> uvs, int UVCoordinate, int slot = 0)
		{
			if (slot < 0 || slot >= pieces.Count) return;

			Mesh mesh = pieces[slot].meshfilter.sharedMesh;

			if (UVCoordinate >= 0 && UVCoordinate < 8)
				mesh.SetUVs(UVCoordinate - 1, uvs);

			

		}
		
		public void CreateMeshOnly_fulluv(ref NativeArray<Vector3> vertices, ref NativeArray<int> triangles, ref NativeArray<Vector2> uvs,
ref NativeArray<Vector4> tangents, ref NativeArray<Vector3> normals, ref Mesh mesh,
ref NativeArray<Vector2> uvs2, ref NativeArray<Vector2> uvs3, ref NativeArray<Vector2> uvs4, ref NativeArray<Vector2> uvs5,
ref NativeArray<Vector2> uvs6, ref NativeArray<Vector2> uvs7, ref NativeArray<Vector2> uvs8)
		{
			bool requiresfullAssignment = false;

			if (mesh.vertexCount != vertices.Length)
			{
				requiresfullAssignment = true;
			}

			if (requiresfullAssignment)
			{

				if (mesh.vertexCount <= vertices.Length)
				{
					mesh.SetVertices(vertices);
					mesh.triangles = triangles.ToArray();
				}
				else
				{
					mesh.triangles = triangles.ToArray();
					mesh.SetVertices(vertices);

				}

				mesh.SetUVs(0, uvs);
				if (uvs2.IsCreated)
					mesh.SetUVs(1, uvs2);

				if (uvs3.IsCreated)
					mesh.SetUVs(2, uvs3);

				if (uvs4.IsCreated)
					mesh.SetUVs(3, uvs4);

				if (uvs5.IsCreated)
					mesh.SetUVs(4, uvs5);
				
				if (uvs6.IsCreated)
					mesh.SetUVs(5, uvs6);

				if (uvs7.IsCreated)
					mesh.SetUVs(6, uvs7);

				if (uvs8.IsCreated)
					mesh.SetUVs(7, uvs8);

				
				mesh.SetNormals(normals);
				mesh.SetTangents(tangents);
			}
			else
			{
				mesh.SetVertices(vertices);
			}
			mesh.RecalculateBounds();
		}


		public NativeMeshPiece CreateMeshPiece(string Name, int slot)
        {
            GameObject newobj = new GameObject(Name + slot);
            newobj.transform.SetParent(transform);

          
            newobj.transform.localRotation = Quaternion.identity;
            newobj.transform.localPosition = new Vector3(0, 0, 0);
            newobj.transform.localScale = Vector3.one;
            newobj.gameObject.isStatic = gameObject.isStatic;
            newobj.layer = gameObject.layer;
            NativeMeshPiece piece = newobj.AddComponent<NativeMeshPiece>();
            piece.meshfilter = newobj.AddComponent<MeshFilter>();
            piece.meshcollider = newobj.AddComponent<MeshCollider>();
            piece.meshrenderer = newobj.AddComponent<MeshRenderer>();
            piece.meshrenderer.material = currentMaterial;
			piece.slot = slot;
            Mesh visualhull = new Mesh();
            visualhull.MarkDynamic();
            visualhull.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;            
            piece.meshfilter.sharedMesh = visualhull;

			if (HasCollision)
			{
				piece.meshcollider.sharedMesh = visualhull;
				piece.meshcollider.convex = ConvexCollider;

			}
			else
			{
				piece.meshcollider.enabled = false;
			}

            MeshPieceAttachment[] options = GetComponents<MeshPieceAttachment>();
            for (int i = 0; i < options.Length; i++)
            {
				if (options[i].TargetSpecificSlot && !options[i].TargetSlots.Contains(slot)) continue;

                options[i].Effect(newobj);

            }

            HideFlags flag = EvaluateHideFlag();
            newobj.hideFlags = flag;
            newobj.transform.hideFlags = flag;
            visualhull.hideFlags = flag;

			piece.attachments = piece.GetComponents<MeshPieceAttachment>();

			return piece;
        }

        public NativeMeshPiece CreateMeshPiece(string Name, Transform parent)
        {
            GameObject newobj = new GameObject(Name);



            if (HideInHierachy) newobj.hideFlags = HideFlags.HideInHierarchy;
            newobj.transform.rotation = transform.rotation;
            newobj.transform.position = transform.position;
            newobj.transform.localScale = transform.localScale;
            newobj.transform.SetParent(parent);
            newobj.gameObject.isStatic = parent.gameObject.isStatic;
            newobj.layer = parent.gameObject.layer;
            NativeMeshPiece piece = newobj.AddComponent<NativeMeshPiece>();
            piece.meshfilter = newobj.AddComponent<MeshFilter>();
            piece.meshcollider = newobj.AddComponent<MeshCollider>();
            piece.meshrenderer = newobj.AddComponent<MeshRenderer>();
            piece.meshrenderer.material = currentMaterial;
            Mesh visualhull = new Mesh();
            visualhull.MarkDynamic();
            visualhull.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            piece.meshfilter.sharedMesh = visualhull;

            if (HasCollision)
            {
                piece.meshcollider.sharedMesh = visualhull;
                piece.meshcollider.convex = ConvexCollider;
                
            }
            else
            {
                piece.meshcollider.enabled = false;
            }

			piece.attachments = piece.GetComponents<MeshPieceAttachment>();

			HideFlags flag = EvaluateHideFlag();
            newobj.hideFlags = flag;
            newobj.transform.hideFlags = flag;
            visualhull.hideFlags = flag;

            return piece;
        }

        public void RemoveMeshAtSlot(int slot)
        {
            if (pieces.Count <= slot)
            {
                return;
            }

            if (!pieces[slot])
            {
                return;
            }

            if (pieces[slot].meshfilter.sharedMesh == null) return;
            pieces[slot].meshfilter.sharedMesh.Clear();
            if (pieces[slot].meshcollider.sharedMesh)
                pieces[slot].meshcollider.sharedMesh.Clear();
        }

        public void SetMeshVisibility(bool state)
        {
            NativeMeshPiece[] childs = GetComponentsInChildren<NativeMeshPiece>();
            for (int i = 0; i < childs.Length; i++)
            {
                childs[i].meshrenderer.enabled = state;
            }

        }

        /// <summary>
        /// Change the material of the whole result
        /// </summary>
        /// <param name="material">the material to assign</param>
        public void SetMaterial(Material material)
        {
            var childs = pieces;	

			for (int i = 0; i < childs.Count; i++)
            {
				if (childs[i] == null) continue;

				if (UseSeperateMaterials)
				{
					if (i < SeperateMaterials.Length)
					{
						childs[i].meshrenderer.sharedMaterial = SeperateMaterials[i];
					}
					else
					{
						childs[i].meshrenderer.sharedMaterial = currentMaterial;
					}
				}
				else
				childs[i].meshrenderer.sharedMaterial = material;
            }
            currentMaterial = material;
        }


        /// <summary>
        /// Destroys every result generated from this object
        /// </summary>
        public void Reset()
        {
            pieces = new List<NativeMeshPiece>();

            buffers = new List<NativeMeshBuffer>();

			barycentriccalc.CleanUp();

            NativeMeshPiece[] childs = GetComponentsInChildren<NativeMeshPiece>();
            for (int i = 0; i < childs.Length; i++)
            {
                DestroyImmediate(childs[i].meshfilter.sharedMesh);

#if UNITY_EDITOR              
                if (PrefabUtility.GetPrefabInstanceStatus(childs[i]) != PrefabInstanceStatus.Connected)
                {
                    DestroyImmediate(childs[i].gameObject);
                }
#else
                DestroyImmediate(childs[i].gameObject);
#endif  
            }
            pieces.AddRange(GetComponentsInChildren<NativeMeshPiece>());

			var additionalUVs = GetComponentsInChildren<ProceduralUVGenerator>();
			for (int i = 0; i < additionalUVs.Length; i++)
			{
				ProceduralUVGenerator modifier = additionalUVs[i];
				modifier.CleanUp();
			}
		}

		public void ClearMemory()
		{
			var additionalUVs = GetComponentsInChildren<ProceduralUVGenerator>();
			for (int i = 0; i < additionalUVs.Length; i++)
			{
				ProceduralUVGenerator modifier = additionalUVs[i];
				modifier.CleanUp();
			}
		}

        public void DuplicateMesh()
        {
            NativeMeshPiece[] childs = GetComponentsInChildren<NativeMeshPiece>();
            for (int i = 0; i < childs.Length; i++)
            {
                MeshFilter filter = childs[i].meshfilter;
                if (filter)
                    if (filter.sharedMesh) filter.sharedMesh = Instantiate<Mesh>(filter.sharedMesh);
            }
        }

        public void HideFromInspector()
        {

            NativeMeshPiece[] childs = GetComponentsInChildren<NativeMeshPiece>();
            for (int i = 0; i < childs.Length; i++)
            {
                childs[i].transform.hideFlags = HideFlags.HideInHierarchy;
            }
        }

        public void UnHideFromInspector()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                child.hideFlags = HideFlags.None;

                for (int k = 0; k < child.childCount; k++)
                {
                    child.GetChild(k).hideFlags = HideFlags.None;

                }


            }
        }

        public void UpdateHideFlags()
        {
            HideFlags flag = EvaluateHideFlag();
            NativeMeshPiece[] childs = GetComponentsInChildren<NativeMeshPiece>();
            for (int i = 0; i < childs.Length; i++)
            {
                childs[i].gameObject.hideFlags = flag;
                childs[i].transform.hideFlags = flag;
                childs[i].meshfilter.sharedMesh.hideFlags = flag;
            }
        }

        void OnDestroy()
        {
            Reset();
        }

        public bool ErrorHandling()
        {
            if (buffers == null) buffers = new List<NativeMeshBuffer>();
            if (pieces == null) pieces = new List<NativeMeshPiece>();


            return false;
        }

        public bool HasGenerations()
        {
            if (pieces == null) return false;
            if (pieces.Count == 0) return false;
            if (pieces[0] == null) return false;

            return true;
        }

        public void MeshToFile(Mesh mf, Material[] mats, string name, string path)
        {
            StreamWriter writer = new StreamWriter(path + name);
            writer.Write(MeshToString(mf, mats, name));
            writer.Close();
        }

        private string MeshToString(Mesh mesh, Material[] Mats, string name)
        {
            Mesh m = mesh;
            Material[] mats = Mats;

            StringBuilder sb = new StringBuilder();

            sb.Append("g ").Append(name).Append("\n");
            foreach (Vector3 v in m.vertices)
            {
                sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
            }
            sb.Append("\n");
            foreach (Vector3 v in m.normals)
            {
                sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
            }
            sb.Append("\n");
            foreach (Vector3 v in m.uv)
            {
                sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
            }
            for (int material = 0; material < m.subMeshCount; material++)
            {
                sb.Append("\n");
                sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                sb.Append("usemap ").Append(mats[material].name).Append("\n");

                int[] triangles = m.GetTriangles(material);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                        triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
                }
            }
            return sb.ToString();
        }

        private HideFlags EvaluateHideFlag()
        {
            HideFlags flag = HideFlags.None;
            if (HideInHierachy)
            {
                if (NoSceneSaving)
                {
                    flag = HideFlags.HideAndDontSave;
                }
                else
                {
                    flag = HideFlags.HideInHierarchy;
                }
            }
            else
            {
                if (NoSceneSaving)
                {
                    flag = HideFlags.DontSave;
                }
                else
                {
                    flag = HideFlags.None;
                }
            }
            return flag;
        }









    }
}
