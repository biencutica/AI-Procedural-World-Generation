using UnityEngine;


public static class ProceduralHelper
{
    //dispatch compute shader
    //write noise values into a RenderTexture
    //read data into a float[,]
    //return data
    // the height map size we are using 241x241
    // the scale - number of details
    // octaves - number of layers to combine for complexity
    // persistance - amplitude reduction factor for each octave
    // lacunarity - frequency increase factor for each octave
    // offset - to offset the noise map coordinates

    public static float[,] GenerateNoiseMapGPU(ComputeShader shader, int pixWidth = 241, int pixHeight = 241, float scale = 20f, int seed = 3, int octaves = 4, float persistence = 0.5f, float lacunarity = 2f, float xOffset = 0.0f, float yOffset = 0.0f, float heightMultiplier = 1f, AnimationCurve curve = null)
    {
        if (scale <= 0f) scale = 0.0001f;

        System.Random rand = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = rand.Next(-100000, 100000) + xOffset;
            float offsetY = rand.Next(-100000, 100000) + yOffset;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        //buffer between GPU and CPU
        int count = pixWidth * pixHeight;
        ComputeBuffer resultBuffer = new ComputeBuffer(count, sizeof(float));
        ComputeBuffer offsetBuffer = new ComputeBuffer(octaves, sizeof(float) * 2);
        offsetBuffer.SetData(octaveOffsets);

        int kernel = shader.FindKernel("CSMain");

        shader.SetInt("width", pixWidth);
        shader.SetInt("height", pixHeight);
        shader.SetFloat("scale", scale);
        shader.SetInt("seed", seed);
        shader.SetInt("octaves", octaves);
        shader.SetFloat("persistence", persistence);
        shader.SetFloat("lacunarity", lacunarity);
        shader.SetFloat("xOffset", xOffset);
        shader.SetFloat("yOffset", yOffset);
        shader.SetBuffer(kernel, "octaveOffsets", offsetBuffer);
        shader.SetBuffer(kernel, "ResultBuffer", resultBuffer);

        int threadGroupsX = Mathf.CeilToInt(pixWidth / 8f);
        int threadGroupsY = Mathf.CeilToInt(pixHeight / 8f);
        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

        //read back from GPU
        float[] rawData = new float[count];
        resultBuffer.GetData(rawData);

        float[,] noiseMap = new float[pixWidth, pixHeight];
        for (int y = 0; y < pixHeight; y++)
        {
            for (int x = 0; x < pixWidth; x++)
            {
                int index = y * pixWidth + x;
                float val = rawData[index];
                float evaluated = (curve != null ? curve.Evaluate(val) : val);
                noiseMap[x, y] = evaluated * heightMultiplier;
            }
        }

        offsetBuffer.Release();
        resultBuffer.Release();

        return noiseMap;
    }


    // generates a mesh based on the provided heightmap
    public static Mesh GenerateTerrainMesh(float[,] heightData)
    {
        //if (heightCurve == null) heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
        int width = heightData.GetLength(0);
        int height = heightData.GetLength(1);

        // create vertices and uvs
        var vertices = new Vector3[width * height];
        var uvs = new Vector2[width * height];

        for (var z = 0; z < height; z++)
        {
            for (var x = 0; x < width; x++)
            {
                vertices[x + z * width] = new Vector3(x, heightData[x, z], z);
                uvs[x + z * width] = new Vector2((float)x / (width - 1), (float)z / (height - 1));
            }
        }

        // create triangle indices for the mesh
        var indices = new int[(width - 1) * (height - 1) * 6]; //unity requires triangles, not quads
        var counter = 0;

        for (int z = 0; z < height - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                var lowerLeft = x + (z + 1) * width;
                var lowerRight = (x + 1) + (z + 1) * width;
                var topLeft = x + z * width;
                var topRight = (x + 1) + z * width;

                // create two triangles (upper and lower) for each square
                indices[counter++] = topLeft;
                indices[counter++] = lowerLeft;
                indices[counter++] = topRight;

                indices[counter++] = topRight;
                indices[counter++] = lowerLeft;
                indices[counter++] = lowerRight;
            }
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices,
            triangles = indices,
            uv = uvs
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static float[,] GenerateNoiseMap(int pixWidth = 241, int pixHeight = 241, float scale = 20f, int seed = 0, int octaves = 4, float persistence = 0.5f, float lacunarity = 2f, float xOffset = 0.0f, float yOffset = 0.0f, float heightMultiplier = 1f, AnimationCurve curve = null)
    {

        if (scale <= 0f) scale = 0.0001f;

        System.Random rand = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1f; // high values -> smoother terrain

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = rand.Next(-100000, 100000) + xOffset;
            float offsetY = rand.Next(-100000, 100000) + yOffset;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }

        float[,] noiseMap = new float[pixWidth, pixHeight];
        float halfWidth = pixWidth / 2f;
        float halfHeight = pixHeight / 2f;

        bool useCurve = curve != null;

        // generate Perlin noise
        for (var j = 0; j < pixHeight; j++) // j = z
        {
            for (var i = 0; i < pixWidth; i++) // i = x
            {
                amplitude = 1f; // high values -> smoother terrain
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float x = (i - halfWidth + octaveOffsets[o].x) / scale * frequency;
                    float y = (j - halfHeight + octaveOffsets[o].y) / scale * frequency;

                    float perlinVal = Mathf.PerlinNoise(x, y);
                    noiseHeight += perlinVal * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity; // high lacunarity -> more zoomed-in details
                }

                noiseMap[i, j] = noiseHeight;
                Debug.Log(noiseHeight);
            }
        }

        for (var j = 0; j < pixHeight; j++)
        {
            for (var i = 0; i < pixWidth; i++)
            {
                float normalizedHeight = Mathf.Clamp01(noiseMap[i, j] / maxPossibleHeight);
                noiseMap[i, j] = (useCurve ? curve.Evaluate(normalizedHeight) : normalizedHeight) * heightMultiplier;
            }
        }

        return noiseMap;
    }
}