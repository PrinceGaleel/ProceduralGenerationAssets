using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using TMPro;

using UnityEngine;
using UnityEngine.UI;
using Unity.Jobs;

using static SaveHandler;

public class CharacterCreator : MonoBehaviour
{
    [SerializeField] private GameObject FacialHairCarousel;
    [SerializeField] private bool IsMale;
    [SerializeField] private CharacterOrganizer BaseCharacter;

    [SerializeField] private CharacterSkin WhichInfo;
    [SerializeField] private CharacterObjectParents WhichGender;

    [SerializeField] private CharacterSkin MaleInfo;
    [SerializeField] private CharacterSkin FemaleInfo;

    [SerializeField] private TMP_InputField CharacterName;

    public static HashSet<Vector2Int> Positions;
    private static BinaryFormatter MySerializer;
    private static List<Vector2Int> ToSmooth;

    private System.Random Rnd;

    private void Awake()
    {
        Rnd = new();
        
        MaleInfo = new(true);
        FemaleInfo = new(false);

        MySerializer = new()
        {
            AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
        };

        ToSmooth = new();
    }

    private void Start()
    {
        IsMale = false;
        ToggleGender();
        Randomize();
        ToggleGender();
        Randomize();

        if (Rnd.Next(0, 2) > 0)
        {
            ToggleGender();
        }
    }

    public void NewGame()
    {
        if (CharacterName.text != "")
        {
            SaveData saveData = SaveMap(ExtraUtils.RemoveSpace(CharacterName.text), IsMale ? MaleInfo : FemaleInfo);

            if (saveData != null)
            {
                SceneTransitioner.LoadScene("GameWorld", true, true);
                StartCoroutine(GenerateChunks());
            }
        }
    }

    public IEnumerator GenerateChunks()
    {
        ToSmooth = new();
        Positions = new();

        int mapSize = GameManager.CurrentSaveData.MapSize;
        int expectedAmount = ((mapSize * 2) + 1) * ((mapSize * 2) + 1);

        for (int y = -mapSize; y <= mapSize; y++)
        {
            for (int x = -mapSize; x <= mapSize; x++)
            {
                new PerlinData.GenerateChunkData(GameManager.CurrentSaveData.MyPerlinData.ChunkSize, new(x, y)).Schedule();
            }
            yield return new WaitForEndOfFrame();
        }

        while(Positions.Count != expectedAmount)
        {
            yield return null;
        }

        Positions = null;
        SceneTransitioner.AdvancePreLoad();
    }

    public class WriteChunkData : MainThreadJob
    {
        private readonly ChunkData MyChunkData;

        public WriteChunkData(ChunkData chunkData)
        {
            MyChunkData = chunkData;
        }

        public override void Execute()
        {
            string savePath = PersistentDataPath + "/" + GameManager.CurrentSaveData.SaveNum + "/Chunks";
            Vector2Int chunkPos = MyChunkData.ChunkPosition;

            using FileStream fileStream = new(savePath + "/" + chunkPos.x + "," + chunkPos.y + ".chunk", FileMode.Create);
            MySerializer.Serialize(fileStream, MyChunkData);

            Positions.Add(MyChunkData.ChunkPosition);
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

    private void Randomize()
    {
        WhichGender.HeadAllElements.GetChild(WhichInfo.HeadAllElements).gameObject.SetActive(false);
        WhichInfo.HeadAllElements = Rnd.Next(0, WhichGender.HeadAllElements.childCount - 1);
        WhichGender.HeadAllElements.GetChild(WhichInfo.HeadAllElements).gameObject.SetActive(true);

        WhichGender.Eyebrow.GetChild(WhichInfo.Eyebrow).gameObject.SetActive(false);
        WhichInfo.Eyebrow = Rnd.Next(0, WhichGender.Eyebrow.childCount - 1);
        WhichGender.Eyebrow.GetChild(WhichInfo.Eyebrow).gameObject.SetActive(true);

        BaseCharacter.AllGender.AllHair.GetChild(WhichInfo.Hair).gameObject.SetActive(false);
        WhichInfo.Hair = Rnd.Next(0, BaseCharacter.AllGender.AllHair.childCount - 1);
        BaseCharacter.AllGender.AllHair.GetChild(WhichInfo.Hair).gameObject.SetActive(true);

        if (IsMale)
        {
            WhichGender.FacialHair.GetChild(WhichInfo.FacialHair).gameObject.SetActive(false);
            WhichInfo.FacialHair = Rnd.Next(0, WhichGender.FacialHair.childCount - 1);
            WhichGender.FacialHair.GetChild(WhichInfo.FacialHair).gameObject.SetActive(true);
        }
    }

    private void ToggleGender(bool isMale, bool isEnabled)
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

    public int HeadAllElements;
    public int Eyebrow;
    public int FacialHair;
    public int Hair;

    public CharacterSkin()
    {
        IsMale = false;
        Race = Races.Human;
        FacialHair = -1;
        HeadAllElements = 0;
        Eyebrow = 0;
        Hair = 0;
    }

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