using System;
using UnityEngine;

[CreateAssetMenu(fileName = "UITextureData", menuName = "Scriptable Objects/UITextureData")]
public class UITextureData : ScriptableObject
{
    public Font DefaultFont;

    [Serializable]
    public struct UITexture
    {
        public string Name;
        public Texture2D Texture;
    }

    public UITexture[] Textures;

    public Texture2D GetTexture(string name)
    {
        foreach (var texture in Textures)
        {
            if (texture.Name == name)
            {
                return texture.Texture;
            }
        }

        Debug.LogError($"Texture with name {name} not found.");
        return null;
    }
}
