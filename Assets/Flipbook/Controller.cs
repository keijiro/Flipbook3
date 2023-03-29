using UnityEngine;
using System.Collections.Generic;
using ComputeUnits = MLStableDiffusion.ComputeUnits;
using ImageSource = Klak.TestTools.ImageSource;
using OperationCanceledException = System.OperationCanceledException;

namespace Flipbook {

public sealed class Controller : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] bool _debug = false;
    [Space]
    [SerializeField] ImageSource _source = null;
    [SerializeField, Range(0.1f, 8.0f)] float _speed = 1;
    [Space]
    [SerializeField] string _resourceDir = "StableDiffusion";
    [SerializeField] ComputeUnits _computeUnits = ComputeUnits.All;
    [Space]
    [SerializeField] string _prompt = "vincent van gogh";
    [SerializeField, Range(0, 1)] float _strength = 0.5f;
    [SerializeField, Range(0, 20)] int _stepCount = 5;
    [SerializeField] int _seed = 1;
    [SerializeField] float _guidance = 6;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] Mesh _mesh = null;
    [SerializeField] Material _material = null;
    [SerializeField, HideInInspector] ComputeShader _preprocess = null;

    #endregion

    #region Public accessors

    public float Speed { get => _speed; set => _speed = value; }

    #endregion

    #region Private members

    string ResourcePath
      => Application.streamingAssetsPath + "/" + _resourceDir;

    const int PageCount = 8;

    MLStableDiffusion.Pipeline _pipeline;
    Queue<Page> _pages = new Queue<Page>();

    #endregion

    #region MonoBehaviour implementation

    async void Start()
    {
        Application.targetFrameRate = 24;

        for (var i = 0; i < PageCount; i++)
            _pages.Enqueue(Page.Allocate(gameObject, _mesh, _material, (512, 512)));

        try
        {
            Debug.Log("Loading SD model...");

            if (!_debug)
            {
                _pipeline = new MLStableDiffusion.Pipeline(_preprocess);
                await _pipeline.InitializeAsync(ResourcePath, _computeUnits);
            }

            Debug.Log("Loaded.");

            while (true)
            {
                if (!_debug)
                {
                    _pipeline.Prompt = _prompt;
                    _pipeline.Strength = _strength;
                    _pipeline.StepCount = _stepCount;
                    _pipeline.Seed = _seed;
                    _pipeline.GuidanceScale = _guidance;
                }

                Debug.Log("Generating...");

                var page = _pages.Dequeue();
                if (!_debug)
                    await _pipeline.RunAsync(_source.Texture, page.Texture);
                else
                    await Awaitable.WaitForSecondsAsync(1.5f, destroyCancellationToken);
                page.StartFlipping(_speed);
                _pages.Enqueue(page);

                Debug.Log("Done.");
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    void OnDestroy()
    {
        while (_pages.Count > 0) Page.Deallocate(_pages.Dequeue());
        _pipeline?.Dispose();
        _pipeline = null;
    }

    #endregion
}

} // namespace Flipbook
