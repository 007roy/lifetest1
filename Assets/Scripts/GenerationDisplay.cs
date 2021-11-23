using UnityEngine;
using System;
using UnityEngine.Events;
using TMPro;

[Serializable] public class UnityEventGenerationCounter : UnityEvent<int> { }
public class GenerationDisplay : MonoBehaviour
{
    public static GenerationDisplay singleton;
    public UnityEventGenerationCounter onCounterUpdate;
    TMP_Text text;
    float gameStartTime;

    public GenerationDisplay()
    {
        if (singleton == null) singleton = this;
    }

    void Start()
    {
        onCounterUpdate.AddListener(UpdateCounter);
        text = GetComponent<TMP_Text>();
        gameStartTime = Time.time;
    }

    private void UpdateCounter(int arg0)
    {
        var avgGeneTime = 60/((Time.time - gameStartTime)/arg0); //gens per min
        text.text = arg0 + "\n " + System.Math.Round(avgGeneTime, 0) + " g/m";

    }

    
}


