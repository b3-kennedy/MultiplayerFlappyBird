using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeManager : MonoBehaviour
{
    public static VolumeManager Instance;
    Bloom bloom;

    Volume volume;

    private void Awake()
    {
        Instance = this;
        volume = GetComponent<Volume>();
        volume.profile.TryGet(out bloom);

    }

    public void ChangeBloomIntensity(float value)
    {
        bloom.intensity.value = value;
    }

}
