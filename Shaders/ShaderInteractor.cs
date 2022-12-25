using UnityEngine;

public class ShaderInteractor : MonoBehaviour
{
    private void Start()
    {
        bool found = false;
        if (World.ShaderInteractors != null)
        {
            World.ShaderInteractors.Add(this);
            found = true;
        }

        if (!found)
        {
            Destroy(this);
            enabled = false;
        }
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