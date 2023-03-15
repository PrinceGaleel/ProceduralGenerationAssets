using UnityEngine;

public class GrassShaderInteractor : MonoBehaviour
{
    private void Start()
    {
        GrassChunk.ShaderInteractors.Add(this);
    }

    private void Update()
    {
        Shader.SetGlobalVector("_PositionMoving", transform.position);
    }

    private void OnDestroy()
    {
        if (GrassChunk.ShaderInteractors != null)
        {
            if (GrassChunk.ShaderInteractors.Contains(this))
            {
                GrassChunk.ShaderInteractors.Remove(this);
            }
        }
    }
}