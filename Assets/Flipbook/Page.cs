using UnityEngine;

namespace Flipbook {

readonly struct Page
{
    #region Allocation/deallocation

    public static Page Allocate
      (GameObject parent, Mesh mesh, Material material, (int x, int y) resolution)
    {
        var go = new GameObject("Page");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var rt = new RenderTexture(resolution.x, resolution.y, 0);

        go.transform.parent = parent.transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.hideFlags = HideFlags.HideInHierarchy;
        go.layer = parent.layer;
        mf.sharedMesh = mesh;
        mr.sharedMaterial = material;

        _block.SetFloat("StartTime", 1e+3f);
        mr.SetPropertyBlock(_block);

        return new Page(go, mr, rt);
    }

    public static void Deallocate(Page page)
    {
        Object.Destroy(page.Texture);
        Object.Destroy(page._gameObject);
    }


    #endregion

    #region Public method

    public RenderTexture Texture { get; }

    public Page StartFlipping(float speed)
    {
        _block.SetFloat("Speed", speed);
        _block.SetFloat("StartTime", Time.time);
        _block.SetTexture("ColorMap", Texture);
        _renderer.SetPropertyBlock(_block);
        return this;
    }

    #endregion

    #region Private members

    GameObject _gameObject { get; }
    MeshRenderer _renderer { get; }

    static MaterialPropertyBlock _block = new MaterialPropertyBlock();

    Page(GameObject go, MeshRenderer mr, RenderTexture rt)
      => (_gameObject, _renderer, Texture) = (go, mr, rt);

    #endregion
}

} // namespace Flipbook
