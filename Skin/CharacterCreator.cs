using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCreator : MonoBehaviour
{
    public GameObject FacialHairCarousel;
    public bool IsMale;
    public CharacterOrganizer BaseCharacter;

    public CharacterSkin WhichInfo;
    public CharacterObjectParents WhichGender;

    public CharacterSkin MaleInfo;
    public CharacterSkin FemaleInfo;

    public TMP_InputField CharacterName;
    public int SeedRange = 100000;

    private void Awake()
    {
        World.ShaderInteractors = new();

        if (Random.Range(0, 2) > 0)
        {
            IsMale = false;
        }
        else
        {
            IsMale = true;
        }

        MaleInfo = new(true);
        FemaleInfo = new(false);
    }

    private void Start()
    {
        ToggleGender();
    }

    public void NewGame()
    {
        if (CharacterName.text != "")
        {
            string saveName = SaveData.SaveMap(ExtraUtils.RemoveSpace(CharacterName.text), IsMale ? MaleInfo : FemaleInfo);

            if (saveName != "")
            {
                World.InitializeWorldData(SaveData.LoadData(saveName));
                SceneTransitioner.LoadScene("GameWorld", true);
            }
        }
    }

    public void ChangeHead(int direction)
    {
        WhichGender.HeadAllElements.GetChild(WhichInfo.HeadAllElements).gameObject.SetActive(false);
        WhichInfo.HeadAllElements = (int)Mathf.Repeat(WhichInfo.HeadAllElements + direction, WhichGender.HeadAllElements.childCount - 1);
        WhichGender.HeadAllElements.GetChild(WhichInfo.HeadAllElements).gameObject.SetActive(true);
    }

    public void ChangeEyebrow(int direction)
    {
        WhichGender.Eyebrow.GetChild(WhichInfo.Eyebrow).gameObject.SetActive(false);
        WhichInfo.Eyebrow = (int)Mathf.Repeat(WhichInfo.Eyebrow + direction, WhichGender.Eyebrow.childCount - 1);
        WhichGender.Eyebrow.GetChild(WhichInfo.Eyebrow).gameObject.SetActive(true);
    }

    public void ChangeHair(int direction)
    {
        BaseCharacter.AllGender.AllHair.GetChild(WhichInfo.Hair).gameObject.SetActive(false);
        WhichInfo.Hair = (int)Mathf.Repeat(WhichInfo.Hair + direction, BaseCharacter.AllGender.AllHair.childCount - 1);
        BaseCharacter.AllGender.AllHair.GetChild(WhichInfo.Hair).gameObject.SetActive(true);
    }

    public void ChangeFacialHair(int direction)
    {
        WhichGender.FacialHair.GetChild(WhichInfo.FacialHair).gameObject.SetActive(false);
        WhichInfo.FacialHair = (int)Mathf.Repeat(WhichInfo.FacialHair + direction, WhichGender.FacialHair.childCount - 1);
        WhichGender.FacialHair.GetChild(WhichInfo.FacialHair).gameObject.SetActive(true);
    }

    public void ToggleGender()
    {
        ToggleGender(IsMale, false);
        IsMale = !IsMale;
        ToggleGender(IsMale, true);
    }

    public void ToggleGender(bool isMale, bool isEnabled)
    {
        BaseCharacter.AllGender.AllHair.GetChild(WhichInfo.Hair).gameObject.SetActive(false);

        if (isMale)
        {
            FacialHairCarousel.SetActive(true);
            WhichGender = BaseCharacter.Male;
            WhichInfo = MaleInfo;
            WhichGender.FacialHair.GetChild(WhichInfo.FacialHair).gameObject.SetActive(isEnabled);
        }
        else
        {
            FacialHairCarousel.SetActive(false);
            WhichInfo = FemaleInfo;
            WhichGender = BaseCharacter.Female;
        }

        BaseCharacter.AllGender.AllHair.GetChild(WhichInfo.Hair).gameObject.SetActive(true);
        WhichGender.HeadAllElements.GetChild(WhichInfo.HeadAllElements).gameObject.SetActive(isEnabled);
        WhichGender.Eyebrow.GetChild(WhichInfo.Eyebrow).gameObject.SetActive(isEnabled);
        WhichGender.Torso.GetChild(0).gameObject.SetActive(isEnabled);
        WhichGender.ArmUpperRight.GetChild(0).gameObject.SetActive(isEnabled);
        WhichGender.ArmUpperLeft.GetChild(0).gameObject.SetActive(isEnabled);
        WhichGender.ArmLowerRight.GetChild(0).gameObject.SetActive(isEnabled);
        WhichGender.ArmlowerLeft.GetChild(0).gameObject.SetActive(isEnabled);
        WhichGender.HandRight.GetChild(0).gameObject.SetActive(isEnabled);
        WhichGender.HandLeft.GetChild(0).gameObject.SetActive(isEnabled);
        WhichGender.Hips.GetChild(0).gameObject.SetActive(isEnabled);
        WhichGender.LegRight.GetChild(0).gameObject.SetActive(isEnabled);
        WhichGender.LegLeft.GetChild(0).gameObject.SetActive(isEnabled);
    }
}

[System.Serializable]
public class CharacterSkin
{
    public bool IsMale = true;
    public Races Race;

    //Gender Specific
    public int HeadAllElements;
    public int Eyebrow;
    public int FacialHair;
    public int Hair;

    public CharacterSkin(bool isMale)
    {
        IsMale = isMale;
        Race = Races.Human;

        if (IsMale)
        {
            FacialHair = 0;
        }
        else
        {
            FacialHair = -1;
        }

        HeadAllElements = 0;
        Eyebrow = 0;
        Hair = 0;
    }
}