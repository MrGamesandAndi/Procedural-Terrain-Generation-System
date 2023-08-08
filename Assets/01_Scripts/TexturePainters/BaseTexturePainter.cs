using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.TexturePainters
{
    [Serializable]
    public class TextureConfig : IEquatable<TextureConfig>
    {
        public Texture2D diffuse;
        public Texture2D normalMap;

        public override bool Equals(object obj)
        {
            return Equals(obj as TextureConfig);
        }

        public bool Equals(TextureConfig other)
        {
            return other != null && other.diffuse == diffuse && other.normalMap == normalMap;
        }

        public override int GetHashCode()
        {
            int hash = 17;

            if (diffuse != null)
            {
                hash = hash * 23 + diffuse.GetHashCode();
            }

            if (normalMap != null)
            {
                hash = hash * 23 + normalMap.GetHashCode();
            }

            return hash;
        }
    }

    public class BaseTexturePainter : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)] protected float strength = 1f;

        public virtual void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            Debug.LogError($"No implementation of Execute function for {gameObject.name}.");
        }

        public virtual List<TextureConfig> RetrieveTextures()
        {
            return null;
        }
    }
}
