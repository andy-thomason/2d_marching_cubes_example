using UnityEngine;
using System.Collections;
using System.Collections.Generic;

class MarchingSquares {
    //   2 - 3 - 4
    //   |       |
    //   1       5
    //   |       |
    //   0 - 7 - 6
    //
    // The "middle" vertices 1, 3, 5 and 7 are generated only if the isosurface goes through the edge.
    //
    // We only need to store vertices 0, 1 and 7 per square.
    //
    static readonly ulong[] triangles = {
                            // 6420
        0x0000000000000000, // 0000
        0x0170000000000000, // 0001 (eg. vertex 0, 1 and 7)
        0x1230000000000000, // 0010
        0x0230370000000000, // 0011
        0x3450000000000000, // 0100
        0x0173450000000000, // 0101
        0x2452510000000000, // 0110
        0x2452572700000000, // 0111
        0x5670000000000000, // 1000
        0x0161560000000000, // 1001
        0x1235670000000000, // 1010
        0x0230350560000000, // 1011
        0x7347460000000000, // 1100
        0x6016136340000000, // 1101
        0x4674711240000000, // 1110
        0x0240460000000000, // 1111
    };

    public delegate float del(float x, float y);

    public MarchingSquares(int xdim, int ydim, float xscale, float yscale, float xoffset, float yoffset, del func) {
        //   2 - 3 - 4
        //   |       |
        //   1       5
        //   |       |
        //   0 - 7 - 6
        //
        // Vertices are stored in order 0, 7, 1. ie. 3 vertices per square.
        // Even empty squares have vertices to simplify the algorithm.
        Vector3[] vertices = new Vector3[(xdim+1)*(ydim+1)*3];

        float[] values = new float[(xdim+1)*(ydim+1)];
        List<int> indices = new List<int>();

        int[] index_offsets = {
            (0 + 0*(xdim+1))*3 + 0,  // v0
            (0 + 0*(xdim+1))*3 + 2,  // v1
            (0 + 1*(xdim+1))*3 + 0,  // v2
            (0 + 1*(xdim+1))*3 + 1,  // v3
            (1 + 1*(xdim+1))*3 + 0,  // v4
            (1 + 0*(xdim+1))*3 + 2,  // v5
            (1 + 0*(xdim+1))*3 + 0,  // v6
            (0 + 0*(xdim+1))*3 + 1,  // v7
        };

        for (int j = 0; j <= ydim; ++j) {
            for (int i = 0; i <= xdim; ++i) {
                float x0 = i * xscale + xoffset, y0 = j * yscale + yoffset;
                int k = i + (xdim+1) * j;
                values[k] = func(x0, y0);
                vertices[k*3] = new Vector3(x0, y0, 0);
            }
        }

        for (int j = 0; j < ydim; ++j) {
            for (int i = 0; i < xdim; ++i) {
                float x0 = i * xscale + xoffset, y0 = j * yscale + yoffset;
                int k = i + (xdim+1) * j;
                float v0 = values[k];
                float v6 = values[k+1];
                float v2 = values[k+(xdim+1)];
                bool b0 = v0 > 0;
                bool b2 = v2 > 0;
                bool b6 = v6 > 0;
                if (b0 != b2) {
                    float y2 = (j+1) * yscale + yoffset;
                    float x1 = x0;
                    float y1 = y0 + v0 * (y2 - y0) / (v0 - v2);
                    vertices[k*3+2] = new Vector3(x1, y1, 0);
                }
                if (b0 != b6) {
                    float x6 = (i+1) * xscale + xoffset;
                    float x7 = x0 + v0 * (x6 - x0) / (v0 - v6);
                    float y7 = y0;
                    vertices[k*3+1] = new Vector3(x7, y7, 0);
                }
            }
        }

        for (int j = 0; j < ydim; ++j) {
            for (int i = 0; i < xdim; ++i) {
                int k = i + (xdim+1) * j;
                float v0 = values[k];
                float v2 = values[k+(xdim+1)];
                float v4 = values[k+(xdim+1)+1];
                float v6 = values[k+1];
                bool b0 = v0 > 0;
                bool b2 = v2 > 0;
                bool b4 = v4 > 0;
                bool b6 = v6 > 0;

                int code = (b0 ? 1 : 0) + (b2 ? 2 : 0) + (b4 ? 4 : 0) + (b6 ? 8 : 0);
                ulong tris = triangles[code];
                while ((tris >> 52) != 0) {
                    indices.Add(k*3 + index_offsets[(int)(tris >> 60)]);
                    tris <<= 4;
                    indices.Add(k*3 + index_offsets[(int)(tris >> 60)]);
                    tris <<= 4;
                    indices.Add(k*3 + index_offsets[(int)(tris >> 60)]);
                    tris <<= 4;
                }
            }
        }
        this.vertices = vertices;
        this.indices = indices.ToArray();
    }

    public Vector3[] vertices;
    public int[] indices;
}

public class TheGame : MonoBehaviour {
    Mesh mesh_;
    MeshFilter mesh_filter_;

    // Use this for initialization
    void Start () {
        GameObject go = new GameObject ();
        go.name = "triangle";
        
        mesh_filter_ = go.AddComponent<MeshFilter> ();
        go.AddComponent<MeshRenderer> ();

        mesh_ = new Mesh();
    }

    static float func(float x, float y) {
        return 24 - (x*x + y*y);
    }
    
    // Update is called once per frame
    void Update () {
        MarchingSquares ms = new MarchingSquares(20, 20, 1.0f, 1.0f, -10.0f, -10.0f, func);
        mesh_.vertices = ms.vertices;
        mesh_.SetIndices(ms.indices, MeshTopology.Triangles, 0);
        mesh_filter_.mesh = mesh_;
    }
}
