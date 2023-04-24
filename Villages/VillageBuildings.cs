using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class VillageBuildings : ScriptableObject
{
    public List<CustomTuple<GameObject, Vector2>> CenterBuildings, EssentialBuildings, Houses, OptionalBuildings, Extras;
        
#if UNITY_EDITOR
    private void OnValidate()
    {
        CenterBuildings ??= new();
        EssentialBuildings ??= new();
        Houses ??= new();
        OptionalBuildings ??= new();
        Extras ??= new();

        for (int i = 0; i < CenterBuildings.Count; i++)
        {
            if(CenterBuildings[i].Item1)
            {
                if (CenterBuildings[i].Item1.TryGetComponent(out Building building))
                {
                    CenterBuildings[i] = new(CenterBuildings[i].Item1, building.GetHalfExtents);
                }
            }
        }

        for (int i = 0; i < EssentialBuildings.Count; i++)
        {
            if (EssentialBuildings[i].Item1)
            {
                if (EssentialBuildings[i].Item1.TryGetComponent(out Building building))
                {
                    EssentialBuildings[i] = new(EssentialBuildings[i].Item1, building.GetHalfExtents);
                }
            }
        }

        for (int i = 0; i < Houses.Count; i++)
        {
            if (Houses[i].Item1)
            {
                if (Houses[i].Item1.TryGetComponent(out Building building))
                {
                    Houses[i] = new(Houses[i].Item1, building.GetHalfExtents);
                }
            }
        }

        for (int i = 0; i < OptionalBuildings.Count; i++)
        {
            if (OptionalBuildings[i].Item1)
            {
                if (OptionalBuildings[i].Item1.TryGetComponent(out Building building))
                {
                    OptionalBuildings[i] = new(OptionalBuildings[i].Item1, building.GetHalfExtents);
                }
            }
        }

        for (int i = 0; i < Extras.Count; i++)
        {
            if (Extras[i].Item1)
            {
                if (Extras[i].Item1.TryGetComponent(out Building building))
                {
                    Extras[i] = new(Extras[i].Item1, building.GetHalfExtents);
                }
            }
        }
    }
#endif
}