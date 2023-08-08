using UnityEngine;

namespace ProceduralGeneration.HeightModifiers
{
    public class OffsetHeightModifier : BaseHeightMapModifier
    {
        [SerializeField] float _offsetAmount;

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            for (int y = 0; y < generationData.mapResolution; y++)
            {
                for (int x = 0; x < generationData.mapResolution; x++)
                {
                    //Skip if incorrect biome
                    if (biomeIndex >= 0 && generationData.biomeMap[x, y] != biomeIndex)
                    {
                        continue;
                    }

                    //Calculate new height
                    float newHeight = generationData.heightMap[x, y] + (_offsetAmount / generationData.heightmapScale.y);

                    //Blend based on strength
                    generationData.heightMap[x, y] = Mathf.Lerp(generationData.heightMap[x, y], newHeight, strength);
                }
            }
        }
    }
}
