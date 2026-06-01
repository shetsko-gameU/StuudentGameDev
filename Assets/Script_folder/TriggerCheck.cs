using System.Collections;
using UnityEngine;

public interface TriggerCheck
{
    bool isAggroed{get; set;}
    bool isWithinRange{get; set;}
    void setAggroStatus (bool isAggroed);
    void setRangeBool (bool isWithinRange);
    
}
