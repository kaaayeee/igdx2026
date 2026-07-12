using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class CutoffMaskUI : Image
{
    private Material _customMaterial;

    public override Material materialForRendering
    {
        get
        {
            // Melakukan caching agar tidak membuat 'new Material' setiap frame
            if (_customMaterial == null)
            {
                _customMaterial = new Material(base.materialForRendering);
                // Mengubah fungsi komparasi Stencil menjadi NotEqual untuk efek Inverse
                _customMaterial.SetInt("_StencilComp", (int)CompareFunction.NotEqual);
            }
            return _customMaterial;
        }
    }

    // Penting: Bersihkan material saat object dihancurkan
    protected override void OnDestroy()
    {
        if (_customMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(_customMaterial);
            else
                DestroyImmediate(_customMaterial);
        }
        base.OnDestroy();
    }
}