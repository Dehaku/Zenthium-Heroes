using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fraktalia.Core.Pipe
{
    public class PipeExtruder : MonoBehaviour
    {


        public Vector2[] Querschnitt;

        public int QuerschnittPunkte;
        public float QuerschnittStartAngle;


        public Vector2 UV_Multiplier = new Vector2(1, 1);

        public MeshFilter filter;

        [HideInInspector]
        public List<Vector3> verticelist;

        [HideInInspector]
        public List<Vector2> uvlist;

        [HideInInspector]
        public List<int> indiceslist;

        int indexcounter = 0;

        public float ConnectTreshold = 0.1f;

        [HideInInspector]
        public Vector3[] lastPoints;

        public bool CullEnds = false;

        public void CreateLineMesh(Vector3[] Points, float CrossScale = 1)
        {
            ErrorHandling();

            int vertexcount = Points.Length * Querschnitt.Length * Querschnitt.Length;
            if (vertexcount > 65535) return;

            verticelist = new List<Vector3>(vertexcount);
            uvlist = new List<Vector2>(vertexcount);
            indiceslist = new List<int>(vertexcount * 2);
            indexcounter = 0;
            Mesh current;

            Vector3[] Startpoints = new Vector3[Querschnitt.Length];
            Vector3[] EndPoints = new Vector3[Querschnitt.Length];

            if (filter.sharedMesh && filter.sharedMesh.name == "__PIPEMESH")
            {
                current = filter.sharedMesh;
            }
            else
            {
                filter.sharedMesh = new Mesh();
                current = filter.sharedMesh;
            }
            current.Clear();
            current.name = "__PIPEMESH";
            lastPoints = Points;

            for (int i = 1; i < Points.Length - 2; i++)
            {

                Vector3 direction1 = Points[i] + Points[i - 1];
                Vector3 direction2 = Points[i] + Points[i + 1];

                Vector3 direction = (direction1 + direction2).normalized;
                Vector3 up = Vector3.Cross(direction1, direction2).normalized;


                Vector3 nextdirection1 = Points[i + 1] + Points[i];
                Vector3 nextdirection2 = Points[i + 1] + Points[i + 2];
                Vector3 nextdirection = (nextdirection1 + nextdirection2).normalized;
                Vector3 nextup = Vector3.Cross(nextdirection1, nextdirection2).normalized;

                for (int y = 0; y < Querschnitt.Length; y++)
                {
                    Vector2 QPoint_1;
                    Vector2 QPoint_2;
                    if (y == Querschnitt.Length - 1)
                    {
                        QPoint_1 = Querschnitt[Querschnitt.Length - 1];
                        QPoint_2 = Querschnitt[0];
                    }
                    else
                    {
                        QPoint_1 = Querschnitt[y];
                        QPoint_2 = Querschnitt[y + 1];
                    }

                    QPoint_1 *= CrossScale;
                    QPoint_2 *= CrossScale;

                    Vector3 localPoint1 = direction * QPoint_1.x + up * QPoint_1.y;
                    Vector3 localPoint2 = direction * QPoint_2.x + up * QPoint_2.y;

                    Vector3 nextlocalPoint1 = nextdirection * QPoint_1.x + nextup * QPoint_1.y;
                    Vector3 nextlocalPoint2 = nextdirection * QPoint_2.x + nextup * QPoint_2.y;

                    if (i == 1)
                    {
                        Startpoints[y] = (Points[i] + localPoint1);
                    }

                    if (i == Points.Length - 3)
                    {
                        EndPoints[y] = Points[i + 1] + nextlocalPoint1;
                    }

                    GenerateFace(
                      Points[i] + localPoint1,
                      Points[i] + localPoint2,
                      Points[i + 1] + nextlocalPoint1,
                      Points[i + 1] + nextlocalPoint2
                      );
                }

            }

            #region First and Last section which can look terrible
            if (!CullEnds)
            {
                if (Points.Length > 3)
                {
                    Vector3 direction1 = Points[Points.Length - 2] + Points[Points.Length - 3];
                    Vector3 direction2 = Points[Points.Length - 2] + Points[Points.Length - 1];

                    Vector3 direction = (direction1 + direction2).normalized;
                    Vector3 up = Vector3.Cross(direction1, direction2).normalized;


                    Vector3 nextdirection1 = Points[1] + Points[0];
                    Vector3 nextdirection2 = Points[1] + Points[2];
                    Vector3 nextdirection = (nextdirection1 + nextdirection2).normalized;
                    Vector3 nextup = Vector3.Cross(nextdirection1, nextdirection2).normalized;

                    Vector3 connectdirection1 = Points[0] + Points[Points.Length - 2];
                    Vector3 connectdirection2 = Points[0] + Points[1];
                    Vector3 connectdirection = (connectdirection1 + connectdirection2).normalized;
                    Vector3 connectup = Vector3.Cross(connectdirection1, connectdirection2).normalized;


                    for (int y = 0; y < Querschnitt.Length; y++)
                    {
                        Vector2 QPoint_1;
                        Vector2 QPoint_2;
                        if (y == Querschnitt.Length - 1)
                        {
                            QPoint_1 = Querschnitt[Querschnitt.Length - 1];
                            QPoint_2 = Querschnitt[0];
                        }
                        else
                        {
                            QPoint_1 = Querschnitt[y];
                            QPoint_2 = Querschnitt[y + 1];
                        }

                        QPoint_1 *= CrossScale;
                        QPoint_2 *= CrossScale;

                        Vector3 localPoint1 = direction * QPoint_1.x + up * QPoint_1.y;
                        Vector3 localPoint2 = direction * QPoint_2.x + up * QPoint_2.y;

                        Vector3 nextlocalPoint1 = nextdirection * QPoint_1.x + nextup * QPoint_1.y;
                        Vector3 nextlocalPoint2 = nextdirection * QPoint_2.x + nextup * QPoint_2.y;

                        Plane plane = new Plane(Points[1] - Points[0], Points[0]);
                        Plane plane2 = new Plane(Points[Points.Length - 2] - Points[Points.Length - 1], Points[Points.Length - 1]);


                        Vector3 projectednextlocalPoint1 = Vector3.ProjectOnPlane(nextlocalPoint1, plane.normal);
                        Vector3 projectednextlocalPoint2 = Vector3.ProjectOnPlane(nextlocalPoint2, plane.normal);

                        Vector3 projectedlocalPoint1 = Vector3.ProjectOnPlane(localPoint1, plane2.normal);
                        Vector3 projectedlocalPoint2 = Vector3.ProjectOnPlane(localPoint2, plane2.normal);



                        Vector3 connectlocalPoint1 = connectdirection * QPoint_1.x + connectup * QPoint_1.y;
                        Vector3 connectlocalPoint2 = connectdirection * QPoint_2.x + connectup * QPoint_2.y;



                        if ((Points[0] - Points[Points.Length - 1]).sqrMagnitude < ConnectTreshold * ConnectTreshold)
                        {

                            GenerateFace(
                              Points[0] + connectlocalPoint1,
                              Points[0] + connectlocalPoint2,
                              Points[1] + nextlocalPoint1,
                              Points[1] + nextlocalPoint2
                              );

                            Startpoints[y] = (Points[0] + connectlocalPoint1);

                            GenerateFace(
                             Points[Points.Length - 2] + localPoint1,
                             Points[Points.Length - 2] + localPoint2,
                             Points[Points.Length - 1] + connectlocalPoint1,
                             Points[Points.Length - 1] + connectlocalPoint2
                             );

                            EndPoints[y] = (Points[Points.Length - 1] + connectlocalPoint1);
                        }
                        else
                        {


                            GenerateFace(
                             Points[0] + projectednextlocalPoint1,
                             Points[0] + projectednextlocalPoint2,
                             Points[1] + nextlocalPoint1,
                             Points[1] + nextlocalPoint2
                             );

                            Startpoints[y] = (Points[0] + projectednextlocalPoint1);

                            GenerateFace(
                             Points[Points.Length - 2] + localPoint1,
                             Points[Points.Length - 2] + localPoint2,
                             Points[Points.Length - 1] + projectedlocalPoint1,
                             Points[Points.Length - 1] + projectedlocalPoint2
                             );

                            EndPoints[y] = (Points[Points.Length - 1] + projectedlocalPoint1);
                        }




                    }
                }
            }
            #endregion



            GenerateSurface(Startpoints);

            Vector3[] reverseendpoint = new Vector3[EndPoints.Length];
            for (int i = 0; i < EndPoints.Length; i++)
            {
                reverseendpoint[i] = EndPoints[EndPoints.Length - 1 - i];
            }

            GenerateSurface(reverseendpoint);






            current.SetVertices(verticelist);
            current.SetUVs(0, uvlist);
            current.SetTriangles(indiceslist, 0);
            current.RecalculateNormals();
            current.RecalculateTangents();
        }

        public void Reset()
        {
            ErrorHandling();

            if (filter.sharedMesh && filter.sharedMesh.name == "__PIPEMESH")
            {
                filter.sharedMesh.Clear();
            }

        }

        public void OnDuplicate()
        {
            ErrorHandling();

            if (filter.sharedMesh)
            {
                filter.sharedMesh = Instantiate(filter.sharedMesh);
            }



        }

        void GenerateFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {


            verticelist.Add(a);
            verticelist.Add(b);
            verticelist.Add(d);
            verticelist.Add(c);

            float lengthx = (b - a).magnitude * UV_Multiplier.x;

            float lengthy = (a - d).magnitude * UV_Multiplier.y;


            uvlist.Add(new Vector2(0, 0));
            uvlist.Add(new Vector2(0, lengthx));
            uvlist.Add(new Vector2(lengthy, lengthx));
            uvlist.Add(new Vector2(lengthy, 0));

            indiceslist.Add(0 + indexcounter);
            indiceslist.Add(1 + indexcounter);
            indiceslist.Add(2 + indexcounter);
            indiceslist.Add(2 + indexcounter);
            indiceslist.Add(3 + indexcounter);
            indiceslist.Add(0 + indexcounter);

            indexcounter += 4;

        }

        void GenerateSurface(Vector3[] Points)
        {
            Vector3[] vertices = new Vector3[Points.Length + 1];

            Vector3 center = new Vector3();
            for (int i = 0; i < Points.Length; i++)
            {
                vertices[i + 1] = Points[i];
                center += Points[i];
            }

            Vector2 uvcenter = new Vector3();
            for (int i = 0; i < Querschnitt.Length; i++)
            {
                uvcenter += Querschnitt[i];
            }
            uvcenter /= Querschnitt.Length;


            if (Points.Length > 0)
            {
                center /= Points.Length;
                uvcenter /= Querschnitt.Length;
            }
            vertices[0] = center;

            List<Vector2> uvs = new List<Vector2>();
            uvs.Add(new Vector2(uvcenter.x * UV_Multiplier.x, uvcenter.y * UV_Multiplier.y));

            List<int> indices = new List<int>();
            for (int i = 1; i < vertices.Length - 1; i++)
            {

                indices.Add(indexcounter);
                indices.Add(i + 1 + indexcounter);
                indices.Add(i + indexcounter);


                uvs.Add(new Vector2(Querschnitt[i].x * UV_Multiplier.x, Querschnitt[i].y * UV_Multiplier.y));
            }
            uvs.Add(new Vector2(Querschnitt[0].x * UV_Multiplier.x, Querschnitt[0].y * UV_Multiplier.y));

            indices.Add(indexcounter);
            indices.Add(1 + indexcounter);
            indices.Add(vertices.Length - 1 + indexcounter);

            verticelist.AddRange(vertices);
            uvlist.AddRange(uvs);
            indiceslist.AddRange(indices);

            indexcounter += vertices.Length;
        }

        void ErrorHandling()
        {
            if (!filter) filter = GetComponent<MeshFilter>();
            if (!filter) filter = gameObject.AddComponent<MeshFilter>();

            if (Querschnitt == null || Querschnitt.Length < 2)
            {
                Querschnitt = new Vector2[]
                {
                new Vector2(-0.5f,-0.5f),
                new Vector2(-0.5f,0.5f),
                new Vector2(0.5f,0.5f),
                new Vector2(0.5f,-0.5f)
                };
            }
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