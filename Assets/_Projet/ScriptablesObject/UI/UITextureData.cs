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
}
