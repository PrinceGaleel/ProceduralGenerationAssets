using System.Collections.Generic;
using UnityEngine;

public class CharacterOrganizer : MonoBehaviour
{
    [Header("Material")]
    public Material _Material;

    [Header("Gear Colors")]
    public Color[] Primary = { new(0.2862745f, 0.4f, 0.4941177f), new(0.4392157f, 0.1960784f, 0.172549f), new(0.3529412f, 0.3803922f, 0.2705882f), new(0.682353f, 0.4392157f, 0.2196079f), new(0.4313726f, 0.2313726f, 0.2705882f), new(0.5921569f, 0.4941177f, 0.2588235f), new(0.482353f, 0.4156863f, 0.3529412f), new(0.2352941f, 0.2352941f, 0.2352941f), new(0.2313726f, 0.4313726f, 0.4156863f) };
    public Color[] Secondary = { new(0.7019608f, 0.6235294f, 0.4666667f), new(0.7372549f, 0.7372549f, 0.7372549f), new(0.1647059f, 0.1647059f, 0.1647059f), new(0.2392157f, 0.2509804f, 0.1882353f) };

    [Header("Metal Colors")]
    public Color[] MetalPrimary = { new(0.6705883f, 0.6705883f, 0.6705883f), new(0.5568628f, 0.5960785f, 0.6392157f), new Color(0.5568628f, 0.6235294f, 0.6f), new Color(0.6313726f, 0.6196079f, 0.5568628f), new(0.6980392f, 0.6509804f, 0.6196079f) };
    public Color[] MetalSecondary = { new(0.3921569f, 0.4039216f, 0.4117647f), new(0.4784314f, 0.5176471f, 0.5450981f), new Color(0.3764706f, 0.3607843f, 0.3372549f), new Color(0.3254902f, 0.3764706f, 0.3372549f), new(0.4f, 0.4039216f, 0.3568628f) };

    [Header("Skin Colors")]
    public Color WhiteSkin = new(1f, 0.8000001f, 0.682353f);
    public Color BrownSkin = new(0.8196079f, 0.6352941f, 0.4588236f);
    public Color BlackSkin = new(0.5647059f, 0.4078432f, 0.3137255f);
    public Color ElfSkin = new(0.9607844f, 0.7843138f, 0.7294118f);

    [Header("Hair Colors")]
    public Color[] WhiteHair = { new(0.3098039f, 0.254902f, 0.1764706f), new(0.2196079f, 0.2196079f, 0.2196079f), new(0.8313726f, 0.6235294f, 0.3607843f), new(0.8901961f, 0.7803922f, 0.5490196f), new Color(0.8000001f, 0.8196079f, 0.8078432f), new(0.6862745f, 0.4f, 0.2352941f), new(0.5450981f, 0.427451f, 0.2156863f), new(0.8470589f, 0.4666667f, 0.2470588f) };
    public Color WhiteStubble = new(0.8039216f, 0.7019608f, 0.6313726f);
    public Color[] BrownHair = { new(0.3098039f, 0.254902f, 0.1764706f), new(0.1764706f, 0.1686275f, 0.1686275f), new(0.3843138f, 0.2352941f, 0.0509804f), new(0.6196079f, 0.6196079f, 0.6196079f), new(0.6196079f, 0.6196079f, 0.6196079f) };
    public Color BrownStubble = new(0.6588235f, 0.572549f, 0.4627451f);
    public Color[] BlackHair = { new(0.2431373f, 0.2039216f, 0.145098f), new(0.1764706f, 0.1686275f, 0.1686275f), new(0.1764706f, 0.1686275f, 0.1686275f) };
    public Color BlackStubble = new(0.3882353f, 0.2901961f, 0.2470588f);
    public Color[] ElfHair = { new(0.9764706f, 0.9686275f, 0.9568628f), new(0.1764706f, 0.1686275f, 0.1686275f), new(0.8980393f, 0.7764707f, 0.6196079f) };
    public Color ElfStubble = new(0.8627452f, 0.7294118f, 0.6862745f);

    [Header("Scar Colors")]
    public Color WhiteScar = new(0.9294118f, 0.6862745f, 0.5921569f);
    public Color BrownScar = new(0.6980392f, 0.5450981f, 0.4f);
    public Color BlackScar = new(0.4235294f, 0.3176471f, 0.282353f);
    public Color ElfScar = new(0.8745099f, 0.6588235f, 0.6313726f);

    [Header("Body Art Colors")]
    public Color[] BodyArt = { new Color(0.0509804f, 0.6745098f, 0.9843138f), new(0.7215686f, 0.2666667f, 0.2666667f), new(0.3058824f, 0.7215686f, 0.6862745f), new(0.9254903f, 0.882353f, 0.8509805f), new(0.3098039f, 0.7058824f, 0.3137255f), new(0.5294118f, 0.3098039f, 0.6470588f), new(0.8666667f, 0.7764707f, 0.254902f), new(0.2392157f, 0.4588236f, 0.8156863f) };

    public CharacterObjectParents Male;
    public CharacterObjectParents Female;
    public CharacterObjectListsAllGender AllGender;

    private void Awake()
    {
        BuildParentParts();
    }

    protected void BuildParentParts()
    {
        Transform[] rootTransforms = gameObject.GetComponentsInChildren<Transform>();
        SetGender(Male, rootTransforms, "Male");
        SetGender(Female, rootTransforms, "Female");
        SetAllGender(AllGender, rootTransforms);
    }

    private static Transform BuildList(string characterPart, Transform[] rootTransform)
    {
        // find character parts parent object in the scene
        foreach (Transform t in rootTransform)
        {
            if (t.gameObject.name == characterPart)
            {
                foreach (Transform child in t)
                {
                    child.gameObject.SetActive(false);
                }

                return (t);
            }
        }

        return null;
    }

    public void SetGender(CharacterObjectParents container, Transform[] rootTransforms, string prefix)
    {
        //build out male lists
        container.HeadAllElements = BuildList(prefix + "_Head_All_Elements", rootTransforms);
        container.HeadNoElements = BuildList(prefix + "_Head_No_Elements", rootTransforms);
        container.Eyebrow = BuildList(prefix + "_01_Eyebrows", rootTransforms);
        container.FacialHair = BuildList(prefix + "_02_FacialHair", rootTransforms);
        container.Torso = BuildList(prefix + "_03_Torso", rootTransforms);
        container.ArmUpperRight = BuildList(prefix + "_04_Arm_Upper_Right", rootTransforms);
        container.ArmUpperLeft = BuildList(prefix + "_05_Arm_Upper_Left", rootTransforms);
        container.ArmLowerRight = BuildList(prefix + "_06_Arm_Lower_Right", rootTransforms);
        container.ArmlowerLeft = BuildList(prefix + "_07_Arm_Lower_Left", rootTransforms);
        container.HandRight = BuildList(prefix + "_08_Hand_Right", rootTransforms);
        container.HandLeft = BuildList(prefix + "_09_Hand_Left", rootTransforms);
        container.Hips = BuildList(prefix + "_10_Hips", rootTransforms);
        container.LegRight = BuildList(prefix + "_11_Leg_Right", rootTransforms);
        container.LegLeft = BuildList(prefix + "_12_Leg_Left", rootTransforms);
    }

    public void SetAllGender(CharacterObjectListsAllGender container, Transform[] rootTransforms)
    {
        container. AllHair = BuildList("All_01_Hair", rootTransforms);
        container.AllHeadAttachment = BuildList("All_02_Head_Attachment", rootTransforms);
        container.HeadCoveringsBaseHair = BuildList("HeadCoverings_Base_Hair", rootTransforms);
        container.HeadCoveringsNoFacialHair = BuildList("HeadCoverings_No_FacialHair", rootTransforms);
        container.HeadCoveringsNoHair = BuildList("HeadCoverings_No_Hair", rootTransforms);
        container.ChestAttachment = BuildList("All_03_Chest_Attachment", rootTransforms);
        container.BackAttachment = BuildList("All_04_Back_Attachment", rootTransforms);
        container.ShoulderAttachmentRight = BuildList("All_05_Shoulder_Attachment_Right", rootTransforms);
        container.SHoulderAttachmentLeft = BuildList("All_06_Shoulder_Attachment_Left", rootTransforms);
        container.ElbowAttachmentRight = BuildList("All_07_Elbow_Attachment_Right", rootTransforms);
        container.ElbowAttachmentLeft = BuildList("All_08_Elbow_Attachment_Left", rootTransforms);
        container.HipsAttachment = BuildList("All_09_Hips_Attachment", rootTransforms);
        container.KneeAttachmentRight = BuildList("All_10_Knee_Attachement_Right", rootTransforms);
        container.KneeAttachmentLeft = BuildList("All_11_Knee_Attachement_Left", rootTransforms);
        container.ElfEars = BuildList("Elf_Ear", rootTransforms);
    }
}

[System.Serializable]
public class CharacterObjectParents
{
    public Transform HeadAllElements;
    public Transform Eyebrow;
    public Transform FacialHair;

    public Transform Torso;
    public Transform ArmUpperRight;
    public Transform ArmUpperLeft;
    public Transform ArmLowerRight;
    public Transform ArmlowerLeft;
    public Transform HandRight;
    public Transform HandLeft;
    public Transform Hips;
    public Transform LegRight;
    public Transform LegLeft;

    public Transform HeadNoElements;
}

[System.Serializable]
public class CharacterObjectListsAllGender
{
    public Transform AllHair;

    public Transform HeadCoveringsBaseHair;
    public Transform HeadCoveringsNoFacialHair;
    public Transform HeadCoveringsNoHair;
    public Transform AllHeadAttachment;
    public Transform ChestAttachment;
    public Transform BackAttachment;
    public Transform ShoulderAttachmentRight;
    public Transform SHoulderAttachmentLeft;
    public Transform ElbowAttachmentRight;
    public Transform ElbowAttachmentLeft;
    public Transform HipsAttachment;
    public Transform KneeAttachmentRight;
    public Transform KneeAttachmentLeft;
    public Transform Ears;
    public Transform ElfEars;
}