using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class TerrainGeneration : MonoBehaviour
{
    [SerializeField] private Terrain terrain;
    [SerializeField] private List<GameObject> tree_prefabs = new List<GameObject>();
    private List<Vector2Int> mountain_peaks = new List<Vector2Int>();
    private int terrain_resolution;
    public float base_terrain_height = 0.05f;
    public float frequency = 4f;
    public float amplitude = 0.01f;
    public float seed = 0;
    public int mountain_resolution = 100;
    public float mountain_frequency = 0.5f;
    public float mountain_amplitude = 0.5f;
    public int base_terrain_octaves = 8;
    public int mountain_octaves = 16;
    

    private void Awake()
    {
        terrain = GetComponent<Terrain>();
        GenerateTerrain();
        
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            IncreaseSeed();
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            DecreaseSeed();
        }
    }
    public void GenerateTerrain() 
    {
        terrain_resolution = terrain.terrainData.heightmapResolution;
        float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain_resolution, terrain_resolution);
        //float[,] noise_heights = new float[terrain_resolution, terrain_resolution];
        for (int y = 0; y < terrain_resolution; y++)
        {
            for (int x = 0; x < terrain_resolution; x++)
            {
                heights[x, y] = base_terrain_height;
                float f = frequency;
                float a = amplitude;
                for (int i = 0; i < base_terrain_octaves; i++)
                {
                    float x_coordinate = (x + seed) / (float)terrain_resolution * f;
                    float y_coordinate = (y + seed) / (float)terrain_resolution * f;  
                    heights[x, y] += Mathf.PerlinNoise(x_coordinate, y_coordinate) * a;
                    f *= 1.5f;
                    a *= 0.5f;
                }   
            }
        }
        terrain.terrainData.SetHeights(0, 0, heights);
        GenerateMountains(heights);
        PlaceTrees();
        PaintTerrain();
    }

    
    public void IncreaseSeed()
    {
        seed+=10;
        GenerateTerrain();
    }

    public void DecreaseSeed()
    {
        seed-=10;
        GenerateTerrain();
    }


    private void GenerateMountains(float[,] heights)
    {
        mountain_peaks.Clear();
        while(mountain_peaks.Count < 3)
        {
            float highest_point = 0;
            Vector2Int point_pos = new Vector2Int();
            for (int y = 0; y < terrain_resolution; y++)
            {
                for (int x = 0; x < terrain_resolution; x++)
                {
                    if (heights[x,y] > highest_point)
                    {
                        bool valid = true;
                        foreach(var p in mountain_peaks) 
                        { if (Vector2Int.Distance(p, new Vector2Int(x,y)) <= mountain_resolution)
                            {
                                valid = false;
                            }

                        }
                        if (valid)
                        {
                            highest_point = heights[x, y];
                            point_pos = new Vector2Int(x, y);
                        }
                    }

                }
            }
            mountain_peaks.Add(point_pos);
        }
        foreach (var p in mountain_peaks)
        {
            float[,] mountain_heights = new float[mountain_resolution, mountain_resolution];
            try { mountain_heights = terrain.terrainData.GetHeights(p.x - mountain_resolution / 2, p.y - mountain_resolution / 2, mountain_resolution, mountain_resolution); } catch { return; }
            
            for (int y = 0; y < mountain_resolution; y++)
            {
                for (int x = 0; x < mountain_resolution; x++)
                {
                    float distance_from_peak = Vector2.Distance(new Vector2(x,y), new Vector2(mountain_resolution/2, mountain_resolution/2));
                    float height_scale = 1 - distance_from_peak / (mountain_resolution / 2);
                    if (height_scale < 0)
                    {

                        height_scale = 0;
                    }
                    float f = mountain_frequency;
                    float a = mountain_amplitude;
                    for (int i = 0; i < mountain_octaves; i++)
                    {
                        float x_coordinate = (x + seed) / (float)mountain_resolution * f;
                        float y_coordinate = (y + seed) / (float)mountain_resolution * f;
                        mountain_heights[x, y] += Mathf.PerlinNoise(x_coordinate, y_coordinate) * a * height_scale;
                        f *= 1.3f;
                        a *= 0.3f;
                    }
                    
                }
            }
            terrain.terrainData.SetHeights(p.x - mountain_resolution / 2, p.y - mountain_resolution / 2, mountain_heights);
        }

    }
    
    private void PlaceTrees()
    {
        GameObject tree_parent = new GameObject("trees");
        List<Vector2Int> positions = new List<Vector2Int>();
        //for (int y = 0; y < terrain.terrainData.heightmapResolution; y += 20)
        //{
        //    for (int x = 0; x < terrain.terrainData.heightmapResolution; x += 20)
        //    {
        //        positions.Add(new Vector2Int(x, y) * 1000/513);
        //    }
        //}

        for (int y = 0; y < terrain.terrainData.heightmapResolution; y += 50)
        {
            for (int x = 0; x < terrain.terrainData.heightmapResolution; x += 50)
            {
                positions.Add(new Vector2Int(x, y));
            }
        }
        foreach (var p in positions) 
        {
            float height = terrain.terrainData.GetHeight(p.x, p.y) + terrain.transform.position.y;
            
            Debug.DrawLine(new Vector3(p.x,height, p.y), new Vector3(p.x, 0, p.y), Color.red, 1000);
            foreach (var peak in mountain_peaks)
            {
                if (Vector2.Distance(p, peak) > mountain_resolution)
                {
                    GameObject tree = Instantiate(tree_prefabs[Random.Range(0, tree_prefabs.Count)], new Vector3(p.x, height, p.y), Quaternion.identity, tree_parent.transform);
                }

            }

                
        }

    }
    private void PaintTerrain() 
    {
        TerrainData terrain = GetComponent<Terrain>().terrainData;
        float[,,] splat_map_data = new float[terrain.alphamapWidth, terrain.alphamapHeight, terrain.alphamapLayers];
        for (int y = 0; y < terrain.alphamapHeight; y++) { 
            for(int x = 0; x < terrain.alphamapWidth; x++)
            {
                float x_normalized = (float)x / (float)terrain.alphamapWidth;
                float y_normalized = (float)y / (float)terrain.alphamapHeight;
                float height = terrain.GetHeight(Mathf.RoundToInt(x_normalized * terrain.heightmapResolution),Mathf.RoundToInt(y_normalized * terrain.heightmapResolution));
                float steepness = terrain.GetSteepness(x_normalized, y_normalized);
                height /= terrain.heightmapResolution;
                float[] splat_weights = new float[terrain.alphamapLayers];
                splat_weights[0] = 0.2f;
                splat_weights[2] = height;
                if (height > 1)
                {
                    Debug.Log("error height: " + height);
                }
                if (steepness < 45)
                {
                    splat_weights[1] = 1 - steepness / 90;
                }
                else
                {
                    splat_weights[1] = 0.5f - steepness / 90;
                }
                float z = splat_weights.Sum();
                for (int i = 0; i < terrain.alphamapLayers; i++)
                {
                    splat_weights[i] /= z;
                    splat_map_data[x, y, i] = splat_weights[i];
                }
            }
        
        }
        terrain.SetAlphamaps(0, 0, splat_map_data);

    
    
    }
}
