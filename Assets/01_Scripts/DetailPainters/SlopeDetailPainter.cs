using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.DetailPainters
{
    public class SlopeDetailPainter : BaseDetailPainter
    {
        [SerializeField] TerrainDetailConfig _terrainDetail;
        [SerializeField] AnimationCurve _intensityVsSlope;

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            int detailLayer = generationData.manager.GetDetailLayerForTerrainDetail(_terrainDetail);

            for (int y = 0; y < generationData.detailMapResolution; ++y)
            {
                int heightMapY = Mathf.FloorToInt((float)y * (float)generationData.mapResolution / (float)generationData.detailMapResolution);

                for (int x = 0; x < generationData.detailMapResolution; ++x)
                {
                    int heightMapX = Mathf.FloorToInt((float)x * (float)generationData.mapResolution / (float)generationData.detailMapResolution);

                    //Skip if incorrect biome
                    if (biomeIndex >= 0 && generationData.biomeMap[heightMapX, heightMapY] != biomeIndex)
                    {
                        continue;
                    }

                    generationData.detailLayerMaps[detailLayer][x, y] = Mathf.FloorToInt(strength * _intensityVsSlope.Evaluate(1f - generationData.slopeMap[x, y]) * generationData.maxDetailsPerPatch);
                }
            }
        }

        public override List<TerrainDetailConfig> RetrieveTerrainDetails()
        {
            List<TerrainDetailConfig> allTerrainDetails = new List<TerrainDetailConfig>(1);
            allTerrainDetails.Add(_terrainDetail);
            return allTerrainDetails;
        }
    }
}