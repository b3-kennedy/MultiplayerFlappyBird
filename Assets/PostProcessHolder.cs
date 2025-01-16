using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

[CreateAssetMenu(fileName = "PostProcessHolder", menuName = "Scriptable Objects/PostProcessHolder")]
public class PostProcessHolder : ScriptableObject
{
    public VolumeProfile volume;
}
