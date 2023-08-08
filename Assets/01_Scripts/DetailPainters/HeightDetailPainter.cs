using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.DetailPainters
{
    public class HeightDetailPainter : BaseDetailPainter
    {
        [SerializeField] TerrainDetailConfig _terrainDetail;
        [SerializeField] float _startHeight;
        [SerializeField] float _endHeight;
        [SerializeField] AnimationCurve _intensity;
        [SerializeField] bool _suppressOtherDetails = false;
        [SerializeField] AnimationCurve _suppressionIntensity;

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            int detailLayer = generationData.manager.GetDetailLayerForTerrainDetail(_terrainDetail);
            float heightMapStart = _startHeight / generationData.heightmapScale.y;
            float heightMapEnd = _endHeight / generationData.heightmapScale.y;
            float heightMapRangeInv = 1f / (heightMapEnd - heightMapStart);
            int numDetailLayers = generationData.detailLayerMaps.Count;

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

                    float height = generationData.heightMap[heightMapX, heightMapY];

                    //Skip if outside of height range
                    if (height < heightMapStart || height > heightMapEnd)
                    {
                        continue;
                    }

                    float heightPercentage = (height - heightMapStart) * heightMapRangeInv;
                    generationData.detailLayerMaps[detailLayer][x, y] = Mathf.FloorToInt(strength * _intensity.Evaluate(heightPercentage) * generationData.maxDetailsPerPatch);

                    //If suppression of other details is on then update the other layers
                    if (_suppressOtherDetails)
                    {
                        float suppression = _suppressionIntensity.Evaluate(heightPercentage);

                        //Apply suppression to other layers
                        for (int layerIndex = 0; layerIndex < numDetailLayers; ++layerIndex)
                        {
                            if (layerIndex == detailLayer)
                            {
                                continue;
                            }

                            generationData.detailLayerMaps[detailLayer][x, y] = Mathf.FloorToInt(generationData.detailLayerMaps[detailLayer][x, y] * suppression);
                        }
                    }
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
