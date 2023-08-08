using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.ObjectPlacers
{
    public class PerlinObjectPlacer : BaseObjectPlacer
    {
        [SerializeField] Vector2 _noiseScale = new Vector2(1f / 128f, 1f / 128f);
        [SerializeField] float _noiseThreshold = 0.5f;

        private List<Vector3> GetFilteredLocationsForBiome(ProcGenManager.GenerationData generationData, int biomeIndex)
        {
            List<Vector3> locations = new List<Vector3>(generationData.mapResolution * generationData.mapResolution / 10);

            for (int y = 0; y < generationData.mapResolution; y++)
            {
                for (int x = 0; x < generationData.mapResolution; x++)
                {
                    if (generationData.biomeMap[x, y] != biomeIndex)
                    {
                        continue;
                    }

                    //Calculate noise value
                    float noiseValue = Mathf.PerlinNoise(x * _noiseScale.x, y * _noiseScale.y);

                    //Noise must be above threshold to be considered a candidate point
                    if (noiseValue < _noiseThreshold)
                    {
                        continue;
                    }

                    float height = generationData.heightMap[x, y] * generationData.heightmapScale.y;
                    locations.Add(new Vector3(y * generationData.heightmapScale.z, height, x * generationData.heightmapScale.x));
                }
            }

            return locations;
        }

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            base.Execute(generationData, biomeIndex, biome);

            //Get potential spawn location
            List<Vector3> candidateLocations = GetFilteredLocationsForBiome(generationData, biomeIndex);
            ExecuteSimpleSpawning(generationData, candidateLocations);
        }
    }
}