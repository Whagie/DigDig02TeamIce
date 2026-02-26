using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Room : MonoBehaviour
{
    public List<GameObject> RoomShrouders;
    private List<GameObject> shrouderDepthThings = new();
    private List<MeshRenderer> shrouderRenderers = new();

    public List<GameObject> RoomObjects;

    [SerializeField] private Material origShroudMaterial;
    private Material roomShroudMaterial;

    [Range(0f, 10f), SerializeField] private float _fadeOutSpeed = 5f;
    [Range(0f, 10f), SerializeField] private float _fadeInSpeed = 5f;

    [Range(0f, 10f), SerializeField] private float _lightFadeOutSpeed = 15f;
    [Range(0f, 10f), SerializeField] private float _lightFadeInSpeed = 5f;

    [SerializeField] private Color _fadeOutStartColor;

    private List<Light> roomLights = new();
    private List<float> originalLightIntensities = new();

    public bool StartingRoom = false;
    public bool RoomActive { get; set; } = false;
    public bool IsFadingOut { get; private set; }
    public bool FadedOut { get; private set; } = true;
    public bool IsFadingIn { get; private set; }
    public bool FadedIn { get; private set; } = false;

    private void Awake()
    {
        if (origShroudMaterial == null)
        {
            origShroudMaterial = Resources.Load<Material>("Materials/ShroudMaterial");
        }

        foreach (var shrouder in RoomShrouders)
        {
            for (int i = 0; i < shrouder.transform.childCount; i++)
            {
                if (shrouder.transform.GetChild(i).CompareTag("Shrouder"))
                {
                    shrouderDepthThings.Add(shrouder.transform.GetChild(i).gameObject);
                }
            }
        }
    }

    private void Start()
    {
        roomShroudMaterial = new Material(origShroudMaterial);
        foreach (var room in RoomShrouders)
        {
            if (room.TryGetComponent(out MeshRenderer renderer))
            {
                if (renderer.sharedMaterial == origShroudMaterial)
                    renderer.material = roomShroudMaterial;

                shrouderRenderers.Add(renderer);
            }
        }

        roomLights.AddRange(GetComponentsInChildren<Light>());
        foreach (var light in roomLights)
            originalLightIntensities.Add(light.intensity);

        _fadeOutStartColor.a = StartingRoom ? 0f : 1f;
        foreach (var renderer in shrouderRenderers)
        {
            renderer.material.SetColor("_Color", _fadeOutStartColor);
            renderer.material.SetFloat("_Alpha", _fadeOutStartColor.a);
        }

        if (StartingRoom)
        {
            RoomActive = true;
            foreach (var depthThing in shrouderDepthThings)
            {
                depthThing.SetActive(false);
            }
        }
        else
        {
            RoomActive = false;
            foreach (var depthThing in shrouderDepthThings)
            {
                depthThing.SetActive(true);
            }
        }
    }

    private void Update()
    {
        if (IsFadingOut)
        {
            bool allOpaque = true;
            _fadeOutStartColor.a = Mathf.MoveTowards(_fadeOutStartColor.a, 1f, Time.deltaTime * _fadeOutSpeed);
            foreach (var renderer in shrouderRenderers)
            {
                renderer.material.SetColor("_Color", _fadeOutStartColor);
                renderer.material.SetFloat("_Alpha", _fadeOutStartColor.a);
            }
            foreach (var light in roomLights)
                light.intensity = Mathf.MoveTowards(light.intensity, 0f, Time.deltaTime * _lightFadeOutSpeed);

            if (_fadeOutStartColor.a < 1f)
                allOpaque = false;

            if (allOpaque)
            {
                IsFadingOut = false;
                FadedOut = true;
                FadedIn = false;
                foreach (var depthThing in shrouderDepthThings)
                {
                    depthThing.SetActive(true);
                }
            }
        }

        if (IsFadingIn)
        {
            bool allTransparent = true;
            _fadeOutStartColor.a = Mathf.MoveTowards(_fadeOutStartColor.a, 0f, Time.deltaTime * _fadeInSpeed);
            foreach (var renderer in shrouderRenderers)
            {
                renderer.material.SetColor("_Color", _fadeOutStartColor);
                renderer.material.SetFloat("_Alpha", _fadeOutStartColor.a);
            }
            for (int i = 0; i < roomLights.Count; i++)
            {
                var target = originalLightIntensities[i];
                roomLights[i].intensity = Mathf.MoveTowards(roomLights[i].intensity, target, Time.deltaTime * _lightFadeInSpeed);
            }

            if (_fadeOutStartColor.a > 0f)
                allTransparent = false;

            if (allTransparent)
            {
                IsFadingIn = false;
                FadedIn = true;
                FadedOut = false;
            }
        }
    }

    public void StartFadeOut()
    {
        IsFadingOut = true;
        IsFadingIn = false;
        FadedIn = false;
    }

    public void StartFadeIn()
    {
        IsFadingIn = true;
        IsFadingOut = false;
        FadedOut = false;
        foreach (var depthThing in shrouderDepthThings)
        {
            depthThing.SetActive(false);
        }
    }
}
