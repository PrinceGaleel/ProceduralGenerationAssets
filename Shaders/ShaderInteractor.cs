using UnityEngine;

public class ShaderInteractor : MonoBehaviour
{
    private void Start()
    {
        GrassManager.ShaderInteractors.Add(this);
    }

    private void Update()
    {
        Shader.SetGlobalVector("_PositionMoving", transform.position);
    }

    private void OnDestroy()
    {
        if (GrassManager.ShaderInteractors != null)
        {
            if (GrassManager.ShaderInteractors.Contains(this))
            {
                GrassManager.ShaderInteractors.Remove(this);
            }
        }
    }
}