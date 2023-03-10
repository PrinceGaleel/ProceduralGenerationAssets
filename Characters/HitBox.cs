using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageTypes
{
    Physical,
    Magical
}

public class HitBox : MonoBehaviour
{
    public float CurrentArmorRating;
    public CharacterStats Character;
    public float DamageMultiplier = 1;

    private void Awake()
    {
        if(!Character)
        {
            if(GetComponent<Collider>())
            {
                GetComponent<Collider>().enabled = false;
            }
            else
            {
                Debug.Log("Alert: Hitbox has no collider " + gameObject.name);
            }

            Debug.Log("Alert: Hitbox has no character " + gameObject.name);
            enabled = false;
        }
        else if (!GetComponent<Collider>())
        {
            Debug.Log("Alert: Hitbox has no collider " + Character.name + ", " + gameObject.name);
        }
    }

    public void IncreaseHealth(HitData hitData)
    {
        if(TeamsManager.IsAlly(Character, hitData.From))
        {
            Character.IncreaseHealth(hitData.Amount);
        }
    }

    public bool DecreaseHealth(HitData hitData)
    {
        if (TeamsManager.IsEnemy(Character, hitData.From))
        {
            Character.DecreaseHealth(hitData.Amount * DamageMultiplier);
            return true;
        }

        return false;
    }

    public void IncreaseMana(HitData hitData)
    {
        if (TeamsManager.IsAlly(Character, hitData.From))
        {
            Character.IncreaseMana(hitData.Amount);
        }
    }

    public void DecreaseMana(HitData hitData)
    {
        if (TeamsManager.IsEnemy(Character, hitData.From))
        {
            Character.DecreaseMana(-hitData.Amount);
        }
    }

    public void IncreaseStamina(HitData hitData)
    {
        if (TeamsManager.IsAlly(Character, hitData.From))
        {
            Character.IncreaseStamina(hitData.Amount);
        }
    }

    public void DecreaseStamina(HitData hitData)
    {
        if (TeamsManager.IsEnemy(Character, hitData.From))
        {
            Character.DecreaseStamina(-hitData.Amount);
        }
    }
}

public struct HitData
{
    public CharacterStats From;
    public float Amount;
    public DamageTypes DamageType;

    public HitData(float amount, CharacterStats character, DamageTypes damageType)
    {
        Amount = Mathf.Abs(amount);
        From = character;
        DamageType = damageType;
    }
}