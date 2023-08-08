using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.TexturePainters
{
    public class SlopeTexturePainter : BaseTexturePainter
    {
        [SerializeField] TextureConfig _texture;
        [SerializeField] AnimationCurve _intensityVsSlope;

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            int textureLayer = generationData.manager.GetLayerForTexture(_texture);

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

                    generationData.alphaMaps[heightMapX, heightMapY, textureLayer] = strength * _intensityVsSlope.Evaluate(generationData.slopeMap[x, y]);
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