using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ArmourType
{
    Helmet = 0,
    Chestpiece = 1,
    Arms = 2,
    Leggings = 3,
    Shoes = 4
}

public enum SkinType
{
    SkinnedMeshRenderer,
    MeshRenderer
}

[CreateAssetMenu(menuName = "Item/Armour")]
public class ArmorItem : Item
{
    public float ArmourValue;
    public ArmourType _ArmourType;
    public GameObject Skin;
    public SkinType _SkinType;

    public override bool Use(params object[] parameters)
    {
        throw new System.NotImplementedException();
    }

    public override string ToString()
    {
        return DefaultToString() + "Armour Type: " + _ArmourType.ToString() + "Armor Value: " + ArmourValue;
    }
}
