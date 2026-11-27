using System.Collections.Generic;
using UnityEngine;

namespace GeodesicAutomata
{
    public class GeodesicGrid : MonoBehaviour
    {
        [Min(0)]
        public int subdivisionLevels = 2;

        [Min(0.01f)]
        public float radius = 1.0f;

        public Material hexagonMaterial;
        public Material pentagonMaterial;

        [SerializeField]
        private GeodesicPanel[] _panels = new GeodesicPanel[0];

        /// <summary>
        /// 具体型配列で公開し、外部ループのインターフェース経由オーバーヘッドを回避
        /// </summary>
        public GeodesicPanel[] Panels => _panels;

        /// <summary>
        /// シーン保存・Gitコミット用に、エディタ上で生成された全パネルメッシュと子オブジェクトを完全に消去します。
        /// インスペクターのコンポーネント右クリックメニュー（または3点リーダー）から実行できます。
        /// </summary>
        [ContextMenu("Clear All Panels (For Git Commit / Clean Scene)")]
        public void ClearAllPanelsForCommit()
        {
            ClearChildren();

#if UNITY_EDITOR
            // シーンに変更があったことをUnityに通知してCtrl+Sで保存できるようにマーク
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                Debug.Log($"[GeodesicGrid] 全パネルとMeshを消去しました。Ctrl+Sでシーンを保存するとファイルサイズが最小化されます。");
            }
#endif
        }

        [ContextMenu("Build Geodesic Grid")]
        public void BuildGeodesicGrid()
        {
            ClearChildren();

            CreateBaseIcosahedron(out Vector3[] baseVertices, out int[] baseTriangles);
            Subdivide(baseVertices, baseTriangles, subdivisionLevels, out Vector3[] vertices, out int[] triangles);
            ProjectToSphere(vertices, radius);

            Vector3[] faceCenters = CalculateFaceCenters(vertices, triangles, radius);
            List<List<int>> orderedFaceLoops = BuildDualFaceLoops(vertices, triangles, faceCenters);

            // 各頂点（=各デュアルパネル）の隣接頂点配列を構築
            int[][] neighborsPerVertex = BuildNeighborGraph(vertices.Length, triangles);

            List<GeodesicPanel> panelList = new List<GeodesicPanel>(orderedFaceLoops.Count);

            for (int i = 0; i < orderedFaceLoops.Count; i++)
            {
                List<int> loop = orderedFaceLoops[i];
                if (loop.Count < 3)
                {
                    continue;
                }

                Vector3[] panelVerts = new Vector3[loop.Count];
                for (int j = 0; j < loop.Count; j++)
                {
                    panelVerts[j] = faceCenters[loop[j]];
                }

                bool isPentagon = loop.Count == 5;
                string panelName = isPentagon ? $"Pentagon_{i}" : $"Hexagon_{i}";
                Material mat = isPentagon ? pentagonMaterial : hexagonMaterial;

                GeodesicPanel panel = CreatePanelObject(panelName, panelVerts, mat);

                Color initialColor = Color.white;
                if (mat != null)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        initialColor = mat.GetColor("_BaseColor");
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        initialColor = mat.GetColor("_Color");
                    }
                }

                panel.Initialize(i, isPentagon, neighborsPerVertex[i], initialColor);
                panelList.Add(panel);
            }

            _panels = panelList.ToArray();
        }

        private static int[][] BuildNeighborGraph(int vertexCount, int[] triangles)
        {
            HashSet<int>[] neighborSets = new HashSet<int>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                neighborSets[i] = new HashSet<int>();
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];

                neighborSets[a].Add(b);
                neighborSets[a].Add(c);
                neighborSets[b].Add(a);
                neighborSets[b].Add(c);
                neighborSets[c].Add(a);
                neighborSets[c].Add(b);
            }

            int[][] result = new int[vertexCount][];
            for (int i = 0; i < vertexCount; i++)
            {
                var set = neighborSets[i];
                int[] arr = new int[set.Count];
                set.CopyTo(arr);
                result[i] = arr;
            }

            return result;
        }

        private void ClearChildren()
        {
            _panels = new GeodesicPanel[0];

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;

                MeshFilter mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(mf.sharedMesh);
                    }
                    else
                    {
                        DestroyImmediate(mf.sharedMesh);
                    }
                }

                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private static void CreateBaseIcosahedron(out Vector3[] vertices, out int[] triangles)
        {
            float t = (1.0f + Mathf.Sqrt(5.0f)) * 0.5f;

            Vector3[] rawVerts = new Vector3[]
            {
                new Vector3(-1f,  t,  0f),
                new Vector3( 1f,  t,  0f),
                new Vector3(-1f, -t,  0f),
                new Vector3( 1f, -t,  0f),

                new Vector3( 0f, -1f,  t),
                new Vector3( 0f,  1f,  t),
                new Vector3( 0f, -1f, -t),
                new Vector3( 0f,  1f, -t),

                new Vector3( t,  0f, -1f),
                new Vector3( t,  0f,  1f),
                new Vector3(-t,  0f, -1f),
                new Vector3(-t,  0f,  1f)
            };

            vertices = new Vector3[rawVerts.Length];
            for (int i = 0; i < rawVerts.Length; i++)
            {
                vertices[i] = rawVerts[i].normalized;
            }

            int[] rawTris = new int[]
            {
                0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
                1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
                3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
                4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
            };

            List<int> orientedTris = new List<int>(rawTris.Length);
            for (int i = 0; i < rawTris.Length; i += 3)
            {
                Vector3 a = vertices[rawTris[i]];
                Vector3 b = vertices[rawTris[i + 1]];
                Vector3 c = vertices[rawTris[i + 2]];

                Vector3 normal = Vector3.Cross(b - a, c - a);
                Vector3 centroid = (a + b + c) / 3.0f;

                if (Vector3.Dot(normal, centroid) < 0f)
                {
                    orientedTris.Add(rawTris[i]);
                    orientedTris.Add(rawTris[i + 2]);
                    orientedTris.Add(rawTris[i + 1]);
                }
                else
                {
                    orientedTris.Add(rawTris[i]);
                    orientedTris.Add(rawTris[i + 1]);
                    orientedTris.Add(rawTris[i + 2]);
                }
            }

            triangles = orientedTris.ToArray();
        }

        private static void Subdivide(
            Vector3[] inVertices,
            int[] inTriangles,
            int levels,
            out Vector3[] outVertices,
            out int[] outTriangles)
        {
            List<Vector3> currentVerts = new List<Vector3>(inVertices);
            int[] currentTris = (int[])inTriangles.Clone();

            for (int level = 0; level < levels; level++)
            {
                Dictionary<long, int> midpointCache = new Dictionary<long, int>();
                List<int> nextTris = new List<int>(currentTris.Length * 4);

                for (int i = 0; i < currentTris.Length; i += 3)
                {
                    int a = currentTris[i];
                    int b = currentTris[i + 1];
                    int c = currentTris[i + 2];

                    int ab = GetMidpointIndex(a, b, currentVerts, midpointCache);
                    int bc = GetMidpointIndex(b, c, currentVerts, midpointCache);
                    int ca = GetMidpointIndex(c, a, currentVerts, midpointCache);

                    nextTris.Add(a); nextTris.Add(ab); nextTris.Add(ca);
                    nextTris.Add(b); nextTris.Add(bc); nextTris.Add(ab);
                    nextTris.Add(c); nextTris.Add(ca); nextTris.Add(bc);
                    nextTris.Add(ab); nextTris.Add(bc); nextTris.Add(ca);
                }

                currentTris = nextTris.ToArray();
            }

            outVertices = currentVerts.ToArray();
            outTriangles = currentTris;
        }

        private static int GetMidpointIndex(
            int i,
            int j,
            List<Vector3> vertices,
            Dictionary<long, int> cache)
        {
            long smaller = i < j ? i : j;
            long greater = i < j ? j : i;
            long key = (smaller << 32) | greater;

            if (cache.TryGetValue(key, out int existingIndex))
            {
                return existingIndex;
            }

            Vector3 midpoint = (vertices[i] + vertices[j]) * 0.5f;
            int newIndex = vertices.Count;
            vertices.Add(midpoint);
            cache[key] = newIndex;
            return newIndex;
        }

        private static void ProjectToSphere(Vector3[] vertices, float sphereRadius)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                float mag = v.magnitude;
                if (mag > 1e-6f)
                {
                    vertices[i] = (v / mag) * sphereRadius;
                }
            }
        }

        private static Vector3[] CalculateFaceCenters(Vector3[] vertices, int[] triangles, float sphereRadius)
        {
            int faceCount = triangles.Length / 3;
            Vector3[] centers = new Vector3[faceCount];

            for (int f = 0; f < faceCount; f++)
            {
                int idx = f * 3;
                Vector3 center = (vertices[triangles[idx]] + vertices[triangles[idx + 1]] + vertices[triangles[idx + 2]]) / 3.0f;
                float mag = center.magnitude;
                centers[f] = mag > 1e-6f ? (center / mag) * sphereRadius : center;
            }

            return centers;
        }

        private static List<List<int>> BuildDualFaceLoops(
            Vector3[] vertices,
            int[] triangles,
            Vector3[] faceCenters)
        {
            List<int>[] vertexToFaces = new List<int>[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertexToFaces[i] = new List<int>(6);
            }

            int faceCount = triangles.Length / 3;
            for (int f = 0; f < faceCount; f++)
            {
                int idx = f * 3;
                vertexToFaces[triangles[idx]].Add(f);
                vertexToFaces[triangles[idx + 1]].Add(f);
                vertexToFaces[triangles[idx + 2]].Add(f);
            }

            List<List<int>> loops = new List<List<int>>(vertices.Length);
            for (int v = 0; v < vertices.Length; v++)
            {
                loops.Add(SortFaceCentersAroundVertex(vertices[v], vertexToFaces[v], faceCenters));
            }

            return loops;
        }

        private static List<int> SortFaceCentersAroundVertex(
            Vector3 vertexPos,
            List<int> faceIds,
            Vector3[] faceCenters)
        {
            Vector3 normal = vertexPos.normalized;
            Vector3 reference = Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right;

            Vector3 tangentU = Vector3.Cross(reference, normal).normalized;
            Vector3 tangentV = Vector3.Cross(normal, tangentU).normalized;

            List<(float angle, int faceId)> sorted = new List<(float angle, int faceId)>(faceIds.Count);
            for (int i = 0; i < faceIds.Count; i++)
            {
                int fId = faceIds[i];
                Vector3 dir = faceCenters[fId] - vertexPos;
                float x = Vector3.Dot(dir, tangentU);
                float y = Vector3.Dot(dir, tangentV);
                float angle = Mathf.Atan2(y, x);
                sorted.Add((angle, fId));
            }

            sorted.Sort((a, b) => a.angle.CompareTo(b.angle));

            List<int> ordered = new List<int>(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
            {
                ordered.Add(sorted[i].faceId);
            }

            if (ordered.Count >= 3)
            {
                Vector3 p0 = faceCenters[ordered[0]];
                Vector3 p1 = faceCenters[ordered[1]];
                Vector3 p2 = faceCenters[ordered[2]];
                Vector3 polyNormal = Vector3.Cross(p1 - p0, p2 - p0);

                if (Vector3.Dot(polyNormal, normal) < 0f)
                {
                    ordered.Reverse();
                }
            }

            return ordered;
        }

        private GeodesicPanel CreatePanelObject(string panelName, Vector3[] worldVertices, Material mat)
        {
            GameObject panelObj = new GameObject(panelName);
            panelObj.transform.SetParent(transform, false);

            MeshFilter meshFilter = panelObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = panelObj.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = panelObj.AddComponent<MeshCollider>();
            GeodesicPanel panelComp = panelObj.AddComponent<GeodesicPanel>();

            if (mat != null)
            {
                meshRenderer.sharedMaterial = mat;
            }

            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < worldVertices.Length; i++)
            {
                centroid += worldVertices[i];
            }
            centroid /= worldVertices.Length;

            panelObj.transform.localPosition = centroid;

            Mesh mesh = new Mesh();
            mesh.name = $"{panelName}_Mesh";

            int n = worldVertices.Length;
            Vector3[] localVerts = new Vector3[n + 1];
            localVerts[0] = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                localVerts[i + 1] = worldVertices[i] - centroid;
            }

            List<int> tris = new List<int>(n * 3);
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                tris.Add(0);
                tris.Add(i + 1);
                tris.Add(next + 1);
            }

            Vector2[] uvs = new Vector2[localVerts.Length];
            uvs[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < n; i++)
            {
                float angle = (Mathf.PI * 2.0f * i) / n;
                uvs[i + 1] = new Vector2(0.5f + 0.5f * Mathf.Cos(angle), 0.5f + 0.5f * Mathf.Sin(angle));
            }

            mesh.vertices = localVerts;
            mesh.triangles = tris.ToArray();
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
            return panelComp;
        }
    }
}
