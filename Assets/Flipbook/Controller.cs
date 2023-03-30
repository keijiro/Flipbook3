using UnityEngine;
using System.Collections.Generic;
using ComputeUnits = MLStableDiffusion.ComputeUnits;
using ImageSource = Klak.TestTools.ImageSource;
using OperationCanceledException = System.OperationCanceledException;

namespace Flipbook {

public sealed class Controller : MonoBehaviour
{
    #region Editable attributes

    [Space]
    [SerializeField] string _prompt = "vincent van gogh";
    [SerializeField, Range(0, 1)] float _strength = 0.5f;
    [SerializeField, Range(0, 20)] int _stepCount = 5;
    [SerializeField] int _seed = 1;
    [SerializeField] float _guidance = 6;
    [Space]
    [SerializeField] ImageSource _source = null;
    [SerializeField, Range(0.1f, 2.0f)] float _speed = 1;
    [Space]
    [SerializeField] Mesh _mesh = null;
    [SerializeField] Material _material = null;
    [SerializeField] ComputeShader _preprocess = null;
    [Space]
    [SerializeField] string _resourceDir = "StableDiffusion";
    [SerializeField] ComputeUnits _computeUnits = ComputeUnits.All;
    [Space]
    [SerializeField] UnityEngine.UI.RawImage _uiPreview = null;
    [SerializeField] bool _dryRun = false;

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
        // 24 FPS (Film look! Isn't it?)
        Application.targetFrameRate = 24;

        try
        {
            if (!_dryRun)
            {
                // Stable Diffusion pipeline initialization
                _pipeline = new MLStableDiffusion.Pipeline(_preprocess);
                await _pipeline.InitializeAsync(ResourcePath, _computeUnits);
            }

            for (var cancel = destroyCancellationToken;;)
            {
                if (!_dryRun)
                {
                    // Stable Diffusion parameters
                    _pipeline.Prompt = _prompt;
                    _pipeline.Strength = _strength;
                    _pipeline.StepCount = _stepCount;
                    _pipeline.Seed = _seed;
                    _pipeline.GuidanceScale = _guidance;
                }

                // Page allocation
                var page = _pages.Count >= PageCount ? _pages.Dequeue() :
                    Page.Allocate(gameObject, _mesh, _material, (512, 512));

                _pages.Enqueue(page);

                // New image generation
                if (!_dryRun)
                {
                    await _pipeline.RunAsync(_source.Texture, page.Texture, cancel);
                }
                else
                {
                    await Awaitable.WaitForSecondsAsync(1.5f, cancel);
                    Graphics.Blit(_source.Texture, page.Texture);
                }

                // Page animation start
                page.StartFlipping(_speed);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            // Cleaning unmanaged objects up
            _pipeline?.Dispose();
            _pipeline = null;
        }
    }

    void Update()
      => _uiPreview.texture = _source.Texture;

    void OnDestroy()
    {
        while (_pages.Count > 0) Page.Deallocate(_pages.Dequeue());
    }

    #endregion
}

} // namespace Flipbook
