using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.TexturePainters
{
    public class HeightTexturePainter : BaseTexturePainter
    {
        [SerializeField] TextureConfig _texture;
        [SerializeField] float _startHeight;
        [SerializeField] float _endHeight;
        [SerializeField] AnimationCurve _intensity;
        [SerializeField] bool _suppressOtherTextures = false;
        [SerializeField] AnimationCurve _suppressionIntensity;

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            int textureLayer = generationData.manager.GetLayerForTexture(_texture);
            float heightMapStart = _startHeight / generationData.heightmapScale.y;
            float heightMapEnd = _endHeight / generationData.heightmapScale.y;
            float heightMapRangeInv = 1f / (heightMapEnd - heightMapStart);
            int numAlphaMaps = generationData.alphaMaps.GetLength(2);

            for (int y = 0; y < generationData.alphaMapResolution; y++)
            {
                int heightMapY = Mathf.FloorToInt((float)y * (float)generationData.mapResolution / (float)generationData.alphaMapResolution);

                for (int x = 0; x < generationData.alphaMapResolution; x++)
                {
                    int heightMapX = Mathf.FloorToInt((float)x * (float)generationData.mapResolution / (float)generationData.alphaMapResolution);

                    //Skip if incorrect biome
                    if (biomeIndex >= 0 && generationData.biomeMap[x, y] != biomeIndex)
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
                    generationData.alphaMaps[x, y, textureLayer] = strength * _intensity.Evaluate(heightPercentage);

                    //If suppression is enabled update the other layers
                    if (_suppressOtherTextures)
                    {
                        float suppression = _suppressionIntensity.Evaluate(heightPercentage);

                        //Apply suppresion to other layers
                        for (int layerIndex = 0; layerIndex < numAlphaMaps; layerIndex++)
                        {
                            if (layerIndex == textureLayer)
                            {
                                continue;
                            }

                            generationData.alphaMaps[x, y, layerIndex] *= suppression;
                        }
                    }
                }
            }
        }

        public override List<TextureConfig> RetrieveTextures()
        {
            List<TextureConfig> allTextures = new List<TextureConfig>();
            allTextures.Add(_texture);
            return allTextures;
        }
    }
}