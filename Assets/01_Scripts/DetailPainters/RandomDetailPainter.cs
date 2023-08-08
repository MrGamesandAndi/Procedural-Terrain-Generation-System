using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.DetailPainters
{
    [System.Serializable]
    public class RandomDetailPainterConfig
    {
        public TerrainDetailConfig detailToPaint;
        [Range(0f, 1f)] public float intensityModifier = 1f;
        public float noiseScale;
        [Range(0f, 1f)] public float noiseThreshold;
    }

    public class RandomDetailPainter : BaseDetailPainter
    {
        [SerializeField]List<RandomDetailPainterConfig> _paintingConfigs = new List<RandomDetailPainterConfig>()
        {
            new RandomDetailPainterConfig()
        };

        [System.NonSerialized] List<TerrainDetailConfig> _cachedTerrainDetails = null;

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            for (int y = 0; y < generationData.detailMapResolution; y++)
            {
                int heightMapY = Mathf.FloorToInt((float)y * (float)generationData.mapResolution / (float)generationData.detailMapResolution);

                for (int x = 0; x < generationData.detailMapResolution; x++)
                {
                    int heightMapX = Mathf.FloorToInt((float)x * (float)generationData.mapResolution / (float)generationData.detailMapResolution);

                    //Skip if incorrect biome
                    if (biomeIndex >= 0 && generationData.biomeMap[heightMapX, heightMapY] != biomeIndex)
                    {
                        continue;
                    }

                    //Perform painting
                    foreach (var config in _paintingConfigs)
                    {
                        //Check if noise test passed
                        float noiseValue = Mathf.PerlinNoise(x * config.noiseScale, y * config.noiseScale);

                        if (generationData.Random(0f, 1f) >= noiseValue)
                        {
                            int layer = generationData.manager.GetDetailLayerForTerrainDetail(config.detailToPaint);
                            generationData.detailLayerMaps[layer][x, y] = Mathf.FloorToInt(strength * config.intensityModifier * generationData.maxDetailsPerPatch);
                        }
                    }
                }
            }
        }

        public override List<TerrainDetailConfig> RetrieveTerrainDetails()
        {
            if (_cachedTerrainDetails == null)
            {
                _cachedTerrainDetails = new List<TerrainDetailConfig>();

                foreach (var config in _paintingConfigs)
                {
                    _cachedTerrainDetails.Add(config.detailToPaint);
                }
            }

            return _cachedTerrainDetails;
        }
    }
}