﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextPulse : MonoBehaviour
{
    public Color ColorA;
    public Color ColorB;
    private Image TextToPulse;

    private void Awake()
    {
        TextToPulse = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        TextToPulse.color = Color.Lerp(ColorA, ColorB, Mathf.PingPong(Time.time, 1));
    }
}
