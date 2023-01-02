using UnityEngine;

public class ShaderInteractor : MonoBehaviour
{
    private void Start()
    {
        World.ShaderInteractors.Add(this);
    }

    private void Update()
    {
        Shader.SetGlobalVector("_PositionMoving", transform.position);
    }

    private void OnDestroy()
    {
        if (World.ShaderInteractors != null)
        {
            if (World.ShaderInteractors.Contains(this))
            {
                World.ShaderInteractors.Remove(this);
            }
        }
    }
}