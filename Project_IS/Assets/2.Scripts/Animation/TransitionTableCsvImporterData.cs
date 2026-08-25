using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Animation/Transition Table CSV Importer Data", fileName = "Transition Table CSV Importer Data")]
public class TransitionTableCsvImporterData : ScriptableObject
{
    public TextAsset csvFile;
    public TransitionTable transitionTable;
}
