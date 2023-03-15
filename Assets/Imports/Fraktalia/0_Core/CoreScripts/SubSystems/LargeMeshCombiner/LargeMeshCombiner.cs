using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

#if UNITY_EDITOR              
using UnityEditor;
#endif

namespace Fraktalia.Core.LMS
{
    public class LargeMeshCombiner : MonoBehaviour
    {
        [Range(1000, 64000)]
        public int MaxVertex = 64000;
        public bool HideInHierachy = true;
        public bool SingleMeshOnly = false;
        public bool AttachOnRoot = false;
        private GameObject output;
        private MeshFilter currentPiece;
        private MeshFilter lastMeshPiece;
        public bool ConvexCollider = false;
        public bool isTrigger = false;
        public bool SupressMerges = false;
        public Material currentMaterial;

        /// <summary>
        /// Combines a array of meshes to a single mesh. 
        /// It splits up the Final result into multiple game objects to exceed the vertex limit
        /// The Previous result attached to this game object is replaced by the new result
        /// </summary>
        /// <param name="meshes">Array of meshes</param>
        /// <param name="material">The Material to use</param>
        /// <param name="NoCollider">Should the final result have a collider or not</param>
        public virtual void CombineMesh(CombineInstance[] meshes, bool NoCollider = false)
        {
            Reset();

            output = new GameObject("_CombinedMeshPieces");
            output.AddComponent<LargeMeshPiece>();
            output.transform.SetParent(transform);
            output.transform.localPosition = new Vector3();
            output.transform.localRotation = Quaternion.identity;
            output.transform.localScale = Vector3.one;
            output.layer = gameObject.layer;
            output.isStatic = gameObject.isStatic;

            List<CombineInstance> combinelist = new List<CombineInstance>();
            int Vertexcount = 0;

            int counter = 0;
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i].mesh == null) continue;

                Vertexcount += meshes[i].mesh.vertexCount;
                int maximumvertex = MaxVertex;
                if (meshes[i].mesh.vertexCount >= MaxVertex)
                {
                    maximumvertex = 65535;
                }

                if (Vertexcount >= maximumvertex)
                {

                    counter++;
                    CreatePiece("__Meshpiece" + counter, output.transform, combinelist.ToArray(), NoCollider);

                    combinelist = new List<CombineInstance>();
                    Vertexcount = meshes[i].mesh.vertexCount;

                    if (SingleMeshOnly) break;
                }


                combinelist.Add(meshes[i]);




            }

            if (combinelist.Count != 0)
            {


                CreatePiece("__Meshpiece_Last", output.transform, combinelist.ToArray(), NoCollider);
            }

            if (HideInHierachy) HideFromInspector();
            else UnHideFromInspector();

        }

        /// <summary>
        /// Combines a array of meshes to a single mesh. 
        /// It splits up the Final result into multiple game objects to exceed the vertex limit
        /// The Previous result attached to this game object is not removed. The result is added to the previous result
        /// </summary>
        /// <param name="meshes">Array of meshes</param>
        /// <param name="material">The Material to use</param>
        /// <param name="NoCollider">Should the final result have a collider or not</param>
        /// <returns></returns>
        public bool AdditiveCombineMesh(CombineInstance[] meshes, bool NoCollider = false)
        {

            bool result = false;
            if (!output)
            {
                output = new GameObject("_CombinedMeshPieces");
                output.AddComponent<LargeMeshPiece>();
                output.transform.SetParent(transform);
                output.transform.localPosition = new Vector3();
                output.transform.localRotation = Quaternion.identity;
                output.transform.localScale = Vector3.one;
                output.layer = gameObject.layer;
            }
            if (currentPiece == null) currentPiece = CreatePiece("__MeshPiece", output.transform, new CombineInstance[0], NoCollider);

            List<CombineInstance> combinelist = new List<CombineInstance>();


            int vertexcount = currentPiece.sharedMesh.vertexCount;

            for (int i = 0; i < meshes.Length; i++)
            {
                CombineInstance current = meshes[i];

                int maximumvertexcount = MaxVertex / 8;


                if (current.mesh.vertexCount >= maximumvertexcount)
                {
                    maximumvertexcount = 65535;


                }


                if (vertexcount + current.mesh.vertexCount < maximumvertexcount)
                {
                    combinelist.Add(current);
                    vertexcount += current.mesh.vertexCount;
                    continue;
                }
                else
                {

                    AddToCurrentPiece(combinelist.ToArray());

                    if (SupressMerges || !MergeMeshFilters(lastMeshPiece, currentPiece))
                    {
                        if (SingleMeshOnly) break;
                        lastMeshPiece = currentPiece;
                        currentPiece = CreatePiece("__Meshpiece", output.transform, new CombineInstance[0], NoCollider);
                    }


                    combinelist.Clear();
                    vertexcount = 0;
                    combinelist.Add(current);
                    vertexcount += current.mesh.vertexCount;
                    result = true;
                }
            }

            AddToCurrentPiece(combinelist.ToArray());

            if (HideInHierachy) HideFromInspector();
            else UnHideFromInspector();
            return result;
        }


        public MeshFilter CreatePiece(string Name, Transform output, CombineInstance[] combinelist, bool NoCollider = false)
        {
            GameObject newobj = new GameObject(Name);
            newobj.layer = gameObject.layer;
            newobj.AddComponent<LargeMeshSelector>();



            MeshFilter filter = newobj.AddComponent<MeshFilter>();
            Renderer renderer = newobj.AddComponent<MeshRenderer>();
            newobj.isStatic = gameObject.isStatic;

            renderer.sharedMaterial = currentMaterial;

            newobj.transform.parent = output.transform;
            newobj.transform.localPosition = Vector3.zero;
            newobj.transform.localRotation = Quaternion.identity;
            newobj.transform.localScale = Vector3.one;

            filter.sharedMesh = new Mesh();

            filter.sharedMesh.Clear();


            filter.sharedMesh.CombineMeshes(combinelist, true, true);
            filter.sharedMesh.name = Name;


            if (!NoCollider)
            {
                MeshCollider meshcollider = newobj.AddComponent<MeshCollider>();
                meshcollider.sharedMesh = filter.sharedMesh;
                meshcollider.convex = ConvexCollider;
                meshcollider.isTrigger = isTrigger;

                if (SingleMeshOnly && AttachOnRoot)
                {
                    MeshCollider attachedcollider = gameObject.GetComponent<MeshCollider>();
                    if (!attachedcollider) attachedcollider = gameObject.AddComponent<MeshCollider>();
                    attachedcollider.sharedMesh = filter.sharedMesh;
                    attachedcollider.convex = ConvexCollider;
                    attachedcollider.isTrigger = isTrigger;

                    meshcollider.enabled = false;
                }
            }

            MeshPieceAttachment[] options = GetComponents<MeshPieceAttachment>();
            for (int i = 0; i < options.Length; i++)
            {
                options[i].Effect(newobj);

            }

            if (SingleMeshOnly && AttachOnRoot)
            {
                MeshRenderer attachedrenderer = gameObject.GetComponent<MeshRenderer>();
                if (!attachedrenderer) attachedrenderer = gameObject.AddComponent<MeshRenderer>();
                attachedrenderer.sharedMaterial = currentMaterial;

                MeshFilter attachedfilter = gameObject.GetComponent<MeshFilter>();
                if (!attachedfilter) attachedfilter = gameObject.AddComponent<MeshFilter>();
                attachedfilter.sharedMesh = filter.sharedMesh;

                renderer.enabled = false;
            }


            return filter;
        }

        public MeshFilter AddToCurrentPiece(CombineInstance[] combinelist)
        {


            MeshFilter filter = currentPiece;
            MeshCollider meshcollider = currentPiece.GetComponent<MeshCollider>();
            CombineInstance[] meshes = combinelist;

            CombineInstance[] finalcombine = new CombineInstance[meshes.Length + 1];
            for (int i = 0; i < meshes.Length; i++)
            {
                finalcombine[i].mesh = meshes[i].mesh;
                finalcombine[i].transform = meshes[i].transform;

            }

            CombineInstance oldmesh = new CombineInstance();
            oldmesh.mesh = filter.sharedMesh;
            oldmesh.transform = Matrix4x4.identity;

            finalcombine[finalcombine.Length - 1] = oldmesh;

            filter.sharedMesh = new Mesh();
            filter.sharedMesh.Clear();

            filter.sharedMesh.CombineMeshes(finalcombine, true, true);
            DestroyImmediate(oldmesh.mesh);

            if (meshcollider)
            {
                meshcollider.sharedMesh = filter.sharedMesh;
                meshcollider.convex = false;

                if (SingleMeshOnly && AttachOnRoot)
                {
                    MeshCollider attachedcollider = gameObject.GetComponent<MeshCollider>();
                    if (!attachedcollider) attachedcollider = gameObject.AddComponent<MeshCollider>();
                    attachedcollider.sharedMesh = filter.sharedMesh;
                    attachedcollider.convex = ConvexCollider;
                    attachedcollider.isTrigger = isTrigger;

                    meshcollider.enabled = false;
                }
            }

            if (SingleMeshOnly && AttachOnRoot)
            {
                MeshRenderer attachedrenderer = gameObject.GetComponent<MeshRenderer>();
                if (!attachedrenderer) attachedrenderer = gameObject.AddComponent<MeshRenderer>();
                attachedrenderer.sharedMaterial = currentMaterial;

                MeshFilter attachedfilter = gameObject.GetComponent<MeshFilter>();
                if (!attachedfilter) attachedfilter = gameObject.AddComponent<MeshFilter>();
                attachedfilter.sharedMesh = filter.sharedMesh;
            }

            return filter;
        }

        /// <summary>
        /// Change the material of the whole result
        /// </summary>
        /// <param name="material">the material to assign</param>
        public void SetMaterial(Material material)
        {
            LargeMeshPiece[] childs = GetComponentsInChildren<LargeMeshPiece>();
            for (int i = 0; i < childs.Length; i++)
            {
                for (int m = 0; m < childs[i].transform.childCount; m++)
                {
                    MeshRenderer renderer = childs[i].transform.GetChild(m).GetComponent<MeshRenderer>();

                    if (renderer)
                    {
                        renderer.sharedMaterial = material;
                    }

                }
            }
            currentMaterial = material;
        }


        /// <summary>
        /// Destroys every result generated from this object
        /// </summary>
        public void Reset()
        {
            LargeMeshPiece[] childs = GetComponentsInChildren<LargeMeshPiece>();
            for (int i = 0; i < childs.Length; i++)
            {
                for (int m = 0; m < childs[i].transform.childCount; m++)
                {
                    MeshFilter filter = childs[i].transform.GetChild(m).GetComponent<MeshFilter>();

                    if (filter)
                    {
                        DestroyImmediate(filter.sharedMesh);
                    }

                }

#if UNITY_EDITOR
                if (PrefabUtility.GetPrefabInstanceStatus(childs[i]) != PrefabInstanceStatus.Connected)
                {
                    DestroyImmediate(childs[i].gameObject);
                }
#else
                DestroyImmediate(childs[i].gameObject);
#endif             



            }
        }

        public void DuplicateMesh()
        {
            LargeMeshPiece[] childs = GetComponentsInChildren<LargeMeshPiece>();
            for (int i = 0; i < childs.Length; i++)
            {
                for (int m = 0; m < childs[i].transform.childCount; m++)
                {
                    MeshFilter filter = childs[i].transform.GetChild(m).GetComponent<MeshFilter>();
                    if (filter.sharedMesh) filter.sharedMesh = Instantiate<Mesh>(filter.sharedMesh);
                }
            }
        }

        public void HideFromInspector()
        {


            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                LargeMeshPiece piece = child.GetComponent<LargeMeshPiece>();

                if (piece)
                {
                    for (int k = 0; k < child.childCount; k++)
                    {
                        child.GetChild(k).hideFlags = HideFlags.HideInHierarchy;

                    }

                    child.hideFlags = HideFlags.HideInHierarchy;
                }
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

        public bool MergeMeshFilters(MeshFilter Filter, MeshFilter FilterToAdd)
        {
            if (!Filter || !FilterToAdd) return false;
            if (!Filter.sharedMesh || !FilterToAdd.sharedMesh) return false;

            int sumvertexcount = Filter.sharedMesh.vertexCount + FilterToAdd.sharedMesh.vertexCount;

            if (sumvertexcount >= MaxVertex) return false;
            Filter.gameObject.name = "__Meshpiece_Merged";

            CombineInstance[] meshtoadd = new CombineInstance[2];
            meshtoadd[0].mesh = FilterToAdd.sharedMesh;
            meshtoadd[0].transform = Matrix4x4.identity;
            meshtoadd[1].mesh = Filter.sharedMesh;
            meshtoadd[1].transform = Matrix4x4.identity;

            Filter.sharedMesh = new Mesh();
            Filter.sharedMesh.Clear();
            Filter.sharedMesh.CombineMeshes(meshtoadd, true, true);

            DestroyImmediate(FilterToAdd.sharedMesh);
            FilterToAdd.sharedMesh = new Mesh();
            FilterToAdd.sharedMesh.Clear();
            FilterToAdd.sharedMesh.name = FilterToAdd.name;

            MeshCollider meshcollider = Filter.GetComponent<MeshCollider>();
            if (meshcollider)
            {
                meshcollider.sharedMesh = Filter.sharedMesh;
                meshcollider.convex = false;
            }

            DestroyImmediate(meshtoadd[0].mesh);
            DestroyImmediate(meshtoadd[1].mesh);

            return true;
        }

        void OnDestroy()
        {
            Reset();
        }

        public virtual bool ErrorHandling(CombineInstance[] Meshes)
        {
            for (int i = 0; i < Meshes.Length; i++)
            {
                if (Meshes[i].mesh.vertexCount >= MaxVertex)
                {
                    Debug.LogError("The vertexcount of one mesh is higher than MaxVertex");

                    return true;
                }
            }

            return false;
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


    }

}
