using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TerrainGeneration : MonoBehaviour
{
    [SerializeField] private Terrain terrain;
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
        List<Vector2Int> mountain_peaks = new List<Vector2Int>();
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
    
}
