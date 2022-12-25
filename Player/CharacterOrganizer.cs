// character randomizer version 1.30
using System.Collections.Generic;
using UnityEngine;

public class CharacterOrganizer : MonoBehaviour
{
    public static CharacterOrganizer Instance;

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

    private bool IsMale;

    private CharacterObjectParents Male;
    private CharacterObjectParents Female;
    private CharacterObjectListsAllGender AllGender;

    public int HeadAllElements = -1;
    public int HeadNoElements = -1;
    public int Eyebrow = -1;
    public int FacialHair = -1;
    public int Torso = -1;
    public int ArmUpperRight = -1;
    public int ArmUpperLeft = -1;
    public int ArmLowerRight = -1;
    public int ArmlowerLeft = -1;
    public int HandRight = -1;
    public int HandLeft = -1;
    public int Hips = -1;
    public int LegRight = -1;
    public int LegLeft = -1;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("Error: Multiple character organizer instances detected");
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
        DefaultCharacter();
    }

    private void DefaultCharacter()
    {
        IsMale = true;

        Male.HeadAllElements.GetChild(0).gameObject.SetActive(true);
        Male.Eyebrow.GetChild(0).gameObject.SetActive(true);
        Male.FacialHair.GetChild(0).gameObject.SetActive(true);
        Male.Torso.GetChild(0).gameObject.SetActive(true);
        Male.ArmUpperRight.GetChild(0).gameObject.SetActive(true);
        Male.ArmUpperLeft.GetChild(0).gameObject.SetActive(true);
        Male.ArmLowerRight.GetChild(0).gameObject.SetActive(true);
        Male.ArmlowerLeft.GetChild(0).gameObject.SetActive(true);
        Male.HandRight.GetChild(0).gameObject.SetActive(true);
        Male.HandLeft.GetChild(0).gameObject.SetActive(true);
        Male.Hips.GetChild(0).gameObject.SetActive(true);
        Male.LegRight.GetChild(0).gameObject.SetActive(true);
        Male.LegLeft.GetChild(0).gameObject.SetActive(true);
    }

    private void BuildParentParts()
    {
        Transform[] rootTransforms = gameObject.GetComponentsInChildren<Transform>();
        Male = new(rootTransforms, "Male");
        Female = new(rootTransforms, "Female");
        AllGender = new(rootTransforms);
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

    public void ChangeGender()
    {
        DisableGenderParts();
        IsMale = !IsMale;
        EnableGenderParts();
    }

    private void DisableGenderParts()
    {
        CharacterObjectParents whichGender;

        if (IsMale)
        {
            whichGender = Male;
        }
        else
        {
            whichGender = Female;
        }

        whichGender.HeadAllElements.GetChild(HeadAllElements).gameObject.SetActive(false);
        whichGender.HeadNoElements.GetChild(HeadNoElements).gameObject.SetActive(false);
        whichGender.Eyebrow.GetChild(Eyebrow).gameObject.SetActive(false);

        if (IsMale)
        {
            whichGender.FacialHair.GetChild(FacialHair).gameObject.SetActive(false);
        }

        whichGender.Torso.GetChild(Torso).gameObject.SetActive(false);
        whichGender.ArmUpperRight.GetChild(ArmUpperRight).gameObject.SetActive(false);
        whichGender.ArmUpperLeft.GetChild(ArmUpperLeft).gameObject.SetActive(false);
        whichGender.ArmLowerRight.GetChild(ArmLowerRight).gameObject.SetActive(false);
        whichGender.ArmlowerLeft.GetChild(ArmlowerLeft).gameObject.SetActive(false);
        whichGender.HandRight.GetChild(HandRight).gameObject.SetActive(false);
        whichGender.HandLeft.GetChild(HandLeft).gameObject.SetActive(false);
        whichGender.Hips.GetChild(Hips).gameObject.SetActive(false);
        whichGender.LegRight.GetChild(LegRight).gameObject.SetActive(false);
        whichGender.LegLeft.GetChild(LegLeft).gameObject.SetActive(false);
    }

    private void EnableGenderParts()
    {
        CharacterObjectParents whichGender;

        if (IsMale)
        {
            whichGender = Male;
        }
        else
        {
            whichGender = Female;
        }

        whichGender.HeadAllElements.GetChild(HeadAllElements).gameObject.SetActive(true);
        whichGender.HeadNoElements.GetChild(HeadNoElements).gameObject.SetActive(true);
        whichGender.Eyebrow.GetChild(Eyebrow).gameObject.SetActive(true);

        if (IsMale)
        {
            whichGender.FacialHair.GetChild(FacialHair).gameObject.SetActive(true);
        }

        whichGender.Torso.GetChild(Torso).gameObject.SetActive(true);
        whichGender.ArmUpperRight.GetChild(ArmUpperRight).gameObject.SetActive(true);
        whichGender.ArmUpperLeft.GetChild(ArmUpperLeft).gameObject.SetActive(true);
        whichGender.ArmLowerRight.GetChild(ArmLowerRight).gameObject.SetActive(true);
        whichGender.ArmlowerLeft.GetChild(ArmlowerLeft).gameObject.SetActive(true);
        whichGender.HandRight.GetChild(HandRight).gameObject.SetActive(true);
        whichGender.HandLeft.GetChild(HandLeft).gameObject.SetActive(true);
        whichGender.Hips.GetChild(Hips).gameObject.SetActive(true);
        whichGender.LegRight.GetChild(LegRight).gameObject.SetActive(true);
        whichGender.LegLeft.GetChild(LegLeft).gameObject.SetActive(true);
    }

    [System.Serializable]
    private class CharacterObjectParents
    {
        public Transform HeadAllElements;
        public Transform HeadNoElements;
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

        public CharacterObjectParents(Transform[] rootTransforms, string prefix)
        {
            //build out male lists
            HeadAllElements = BuildList(prefix + "_Head_All_Elements", rootTransforms);
            HeadNoElements = BuildList(prefix + "_Head_No_Elements", rootTransforms);
            Eyebrow = BuildList(prefix + "_01_Eyebrows", rootTransforms);
            FacialHair = BuildList(prefix + "_02_FacialHair", rootTransforms);
            Torso = BuildList(prefix + "_03_Torso", rootTransforms);
            ArmUpperRight = BuildList(prefix + "_04_Arm_Upper_Right", rootTransforms);
            ArmUpperLeft = BuildList(prefix + "_05_Arm_Upper_Left", rootTransforms);
            ArmLowerRight = BuildList(prefix + "_06_Arm_Lower_Right", rootTransforms);
            ArmlowerLeft = BuildList(prefix + "_07_Arm_Lower_Left", rootTransforms);
            HandRight = BuildList(prefix + "_08_Hand_Right", rootTransforms);
            HandLeft = BuildList(prefix + "_09_Hand_Left", rootTransforms);
            Hips = BuildList(prefix + "_10_Hips", rootTransforms);
            LegRight = BuildList(prefix + "_11_Leg_Right", rootTransforms);
            LegLeft = BuildList(prefix + "_12_Leg_Left", rootTransforms);
        }
    }

    [System.Serializable]
    private class CharacterObjectListsAllGender
    {
        public Transform HeadCoveringsBaseHair;
        public Transform HeadCoveringsNoFacialHair;
        public Transform HeadCoveringsNoHair;
        public Transform AllHair;
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

        public CharacterObjectListsAllGender(Transform[] rootTransforms)
        {
            AllHair = BuildList("All_01_Hair", rootTransforms);
            AllHeadAttachment = BuildList("All_02_Head_Attachment", rootTransforms);
            HeadCoveringsBaseHair = BuildList("HeadCoverings_Base_Hair", rootTransforms);
            HeadCoveringsNoFacialHair = BuildList("HeadCoverings_No_FacialHair", rootTransforms);
            HeadCoveringsNoHair = BuildList("HeadCoverings_No_Hair", rootTransforms);
            ChestAttachment = BuildList("All_03_Chest_Attachment", rootTransforms);
            BackAttachment = BuildList("All_04_Back_Attachment", rootTransforms);
            ShoulderAttachmentRight = BuildList("All_05_Shoulder_Attachment_Right", rootTransforms);
            SHoulderAttachmentLeft = BuildList("All_06_Shoulder_Attachment_Left", rootTransforms);
            ElbowAttachmentRight = BuildList("All_07_Elbow_Attachment_Right", rootTransforms);
            ElbowAttachmentLeft = BuildList("All_08_Elbow_Attachment_Left", rootTransforms);
            HipsAttachment = BuildList("All_09_Hips_Attachment", rootTransforms);
            KneeAttachmentRight = BuildList("All_10_Knee_Attachement_Right", rootTransforms);
            KneeAttachmentLeft = BuildList("All_11_Knee_Attachement_Left", rootTransforms);
            ElfEars = BuildList("Elf_Ear", rootTransforms);
        }
    }
}