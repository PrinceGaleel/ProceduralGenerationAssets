using UnityEngine;

public class SkinnedMeshCopier : CharacterOrganizer
{
    public static SkinnedMeshCopier Instance;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("ERROR: Multiple skinned mesh copier instances detected");
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            Instance = this;
            BuildParentParts();
        }
    }

    private void Start()
    {
        LoadCharacter();
        gameObject.SetActive(false);
    }

    public void LoadCharacter()
    {
        CharacterSkin skin = World.CurrentSaveData.CharSkin;
        CharacterObjectParents genderParts;
        if (skin.IsMale)
        {
            genderParts = Male;
        }
        else
        {
            genderParts = Female;
        }

        PlayerStats.Instance.AllGenderParts.AllHair = CopySkinnedMeshRenderer(AllGender.AllHair.GetChild(skin.Hair), PlayerStats.Instance.SkinnedMeshParent);
        PlayerStats.Instance.GenderParts.Eyebrow = CopySkinnedMeshRenderer(genderParts.Eyebrow.GetChild(skin.Eyebrow), PlayerStats.Instance.SkinnedMeshParent);
        PlayerStats.Instance.GenderParts.HeadAllElements = CopySkinnedMeshRenderer(genderParts.HeadAllElements.GetChild(skin.HeadAllElements), PlayerStats.Instance.SkinnedMeshParent);

        if (World.CurrentSaveData.CharSkin.IsMale)
        {
            PlayerStats.Instance.GenderParts.FacialHair = CopySkinnedMeshRenderer(genderParts.FacialHair.GetChild(skin.FacialHair), PlayerStats.Instance.SkinnedMeshParent);
        }

        PlayerStats.Instance.GenderParts.Torso = CopySkinnedMeshRenderer(genderParts.Torso.GetChild(0), PlayerStats.Instance.SkinnedMeshParent);
        PlayerStats.Instance.GenderParts.ArmUpperRight = CopySkinnedMeshRenderer(genderParts.ArmUpperRight.GetChild(0), PlayerStats.Instance.SkinnedMeshParent);
        PlayerStats.Instance.GenderParts.ArmUpperLeft = CopySkinnedMeshRenderer(genderParts.ArmUpperLeft.GetChild(0), PlayerStats.Instance.SkinnedMeshParent);
        PlayerStats.Instance.GenderParts.ArmLowerRight = CopySkinnedMeshRenderer(genderParts.ArmLowerRight.GetChild(0), PlayerStats.Instance.SkinnedMeshParent);
        PlayerStats.Instance.GenderParts.ArmlowerLeft = CopySkinnedMeshRenderer(genderParts.ArmlowerLeft.GetChild(0), PlayerStats.Instance.SkinnedMeshParent);
        PlayerStats.Instance.GenderParts.HandRight = CopySkinnedMeshRenderer(genderParts.HandRight.GetChild(0), PlayerStats.Instance.SkinnedMeshParent);
        PlayerStats.Instance.GenderParts.HandLeft = CopySkinnedMeshRenderer(genderParts.HandLeft.GetChild(0), PlayerStats.Instance.SkinnedMeshParent);
        PlayerStats.Instance.GenderParts.Hips = CopySkinnedMeshRenderer(genderParts.Hips.GetChild(0), PlayerStats.Instance.SkinnedMeshParent);
        PlayerStats.Instance.GenderParts.LegRight = CopySkinnedMeshRenderer(genderParts.LegRight.GetChild(0), PlayerStats.Instance.SkinnedMeshParent);
        PlayerStats.Instance.GenderParts.LegLeft = CopySkinnedMeshRenderer(genderParts.LegLeft.GetChild(0), PlayerStats.Instance.SkinnedMeshParent);

        CameraController.Instance.FirstPersonDisable = new() { PlayerStats.Instance.AllGenderParts.AllHair, PlayerStats.Instance.GenderParts.Eyebrow,
                PlayerStats.Instance.GenderParts.HeadAllElements };
        
        if (World.CurrentSaveData.CharSkin.IsMale)
        {
            CameraController.Instance.FirstPersonDisable.Add(PlayerStats.Instance.GenderParts.FacialHair);
        }

        CameraController.Instance.FirstPersonArmsLayer = new() { PlayerStats.Instance.GenderParts.ArmUpperRight, PlayerStats.Instance.GenderParts.ArmUpperLeft,
             PlayerStats.Instance.GenderParts.ArmLowerRight, PlayerStats.Instance.GenderParts.ArmlowerLeft, PlayerStats.Instance.GenderParts.HandLeft,
             PlayerStats.Instance.GenderParts.HandRight, PlayerStats.Instance.GenderParts.Torso };

        CameraController.Instance.ChangeHeadAttachState(false);
    }

    public Transform CopySkinnedMeshRenderer(Transform rendererToCopy, Transform newParent)
    {
        SkinnedMeshRenderer renderer = Instantiate(rendererToCopy.gameObject, rendererToCopy.parent).GetComponent<SkinnedMeshRenderer>();
        TransferSkinnedMeshRenderer(renderer, newParent);

        return renderer.transform;
    }

    public void TransferSkinnedMeshRenderer(SkinnedMeshRenderer skinToMove, Transform newParent)
    {
        Transform[] newBones = new Transform[skinToMove.bones.Length];
        Transform[] newParentBones = newParent.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < skinToMove.bones.Length; i++)
        {
            foreach (Transform newBone in newParentBones)
            {
                if (newBone.name == skinToMove.bones[i].name)
                {
                    newBones[i] = newBone;
                }
            }
        }

        foreach (Transform newBone in newParentBones)
        {
            if (newBone.name == skinToMove.rootBone.name)
            {
                skinToMove.rootBone = newBone;
                break;
            }
        }

        skinToMove.bones = newBones;
        skinToMove.transform.SetParent(newParent);
        skinToMove.gameObject.SetActive(true);
        //skinToMove.ResetLocalBounds();
    }
}