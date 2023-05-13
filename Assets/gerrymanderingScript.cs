using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class gerrymanderingScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Blocs;
    public GameObject[] BlocObjs;
    public GameObject BlocPivot;
    public Material[] PartyColors;
    public SpriteRenderer Envelope;
    public Sprite[] EnvColors;
    public TextMesh Text;

    bool bluePreffered;
    int area = -1;
    int height = -1; int width = -1;
    int minSize = -1; int maxSize = -1;
    int chosenSize = -1;
    int blocSize = 9;
    int districts = -1;
    int year = DateTime.Today.Year;
    char[] arrangement = {
        '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', //maybe ints are better for this? actually nah
        '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.',
        '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.',
        '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.',
        '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.',
        '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.',
        '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.',
        '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.',
        '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.'
    };
    char[] deranged = { '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.', '.' };
    string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;
        /*
        foreach (KMSelectable object in keypad) {
            object.OnInteract += delegate () { keypadPress(object); return false; };
        }
        */

        //button.OnInteract += delegate () { buttonPress(); return false; };

    }

    // Use this for initialization
    void Start () {
        bluePreffered = UnityEngine.Random.Range(0, 2) == 0;
        Debug.LogFormat("[Gerrymandering #{0}] {1} is your party's color.", moduleId, bluePreffered ? "Blue" : "Orange");
        Envelope.sprite = EnvColors[bluePreffered ? 0 : 1];

        area = UnityEngine.Random.Range(0, 100);

        if      (area <  5) { area = 0; }
        else if (area < 20) { area = 1; }
        else if (area < 60) { area = 2; }
        else if (area < 85) { area = 3; }
        else                { area = 4; }

        height = 9 - area;
        width = height + 4;
        Debug.LogFormat("<Gerrymandering #{0}> {1}x{2} is the width and height of the map.", moduleId, width, height);
        minSize = ((height/2) + 1) * width;
        maxSize = (height - 1) * width;

        float scaleFactor = 1f;
        if (area % 2 == 1) {
            BlocPivot.transform.localPosition = new Vector3(0.005f, 0f, -0.0325f);
        }
        if (area != 0) {
            scaleFactor = 0.1f * (area - 1) + 1.075f;

            for (int blok = 0; blok < 117; blok++) {
                int xPos = blok % 13;
                int yPos = blok / 13;
                bool valid = true;
                
                if (xPos == 12 | yPos == 8)               { valid = false; }
                if (area >= 2 & (xPos == 0 | yPos == 0))  { valid = false; }
                if (area >= 3 & (xPos == 11 | yPos == 7)) { valid = false; }
                if (area == 4 & (xPos == 1 | yPos == 1))  { valid = false; }
                
                BlocObjs[blok].SetActive(valid);
                if (!valid) {
                    arrangement[blok] = 'X';
                    deranged[blok] = 'X';
                }
            }
        }
        BlocPivot.transform.localScale = new Vector3(scaleFactor, 1f, scaleFactor);

        chosenSize = UnityEngine.Random.Range(minSize, maxSize + 1);
        while ((chosenSize % 9) * (chosenSize % 7) * (chosenSize % 5) * (chosenSize % 3) != 0) {
            chosenSize = UnityEngine.Random.Range(minSize, maxSize + 1);
        }
        Text.text = String.Format("Fantasia {0} District\n({1} Voters)", year, chosenSize);
        while (chosenSize % blocSize != 0) {
            blocSize -= 2;
        }
        districts = chosenSize / blocSize;
        Debug.LogFormat("<Gerrymandering #{0}> There will be {1} {2}-minos, which is {3} blocs total.", moduleId, districts, blocSize, chosenSize);

        //NewArrangement:
        int narwhal = 0;
        districts = 3; /// ZAMN

        for (int dis = 0; dis < districts; dis++) {
            NewMino:
            narwhal += 1;
            int[] xOff = { 0, -999, -999, -999, -999, -999, -999, -999, -999 };
            int[] yOff = { 0, -999, -999, -999, -999, -999, -999, -999, -999 };
            int[] cOff = { -999, -999 };

            for (int bloq = 1; bloq < blocSize; bloq++) {
                int rPos = UnityEngine.Random.Range(0, bloq-1);
                NewLocation:
                cOff[0] = xOff[rPos]; 
                cOff[1] = yOff[rPos];
                int rDir = UnityEngine.Random.Range(0, 4);
                switch (rDir) {
                    case 0: cOff[1] -= 1; break;
                    case 1: cOff[0] += 1; break;
                    case 2: cOff[1] += 1; break;
                    case 3: cOff[0] -= 1; break;
                }
                for (int rix = 0; rix < bloq; rix++) {
                    if (xOff[rix] == cOff[0] && yOff[rix] == cOff[1]) {
                        goto NewLocation;
                    }
                }
                xOff[bloq] = cOff[0]; yOff[bloq] = cOff[1];
            }

            int[] gridPlace = {6, 4};

            int foundAFuckinLocation = 0;
            int placementAttempts = 0;

            while (foundAFuckinLocation != blocSize | placementAttempts < 100) {
                gridPlace[0] = UnityEngine.Random.Range(0, 13);
                gridPlace[1] = UnityEngine.Random.Range(0, 9);
                placementAttempts += 1;
                foundAFuckinLocation = 0;
                for (int blc = 0; blc < blocSize; blc++) {
                    if (gridPlace[0]+xOff[blc] < 0 | gridPlace[0]+xOff[blc] > 12 | gridPlace[1]+yOff[blc] < 0 | gridPlace[1]+yOff[blc] > 8) {
                        //thog don't caare
                    }
                    else if (arrangement[(gridPlace[1]+yOff[blc])*13 + (gridPlace[0]+xOff[blc])] == '.') {
                        foundAFuckinLocation += 1;
                    }
                }
            }
            Debug.Log("Placement attempts: " + placementAttempts);
            Debug.Log("Found a fucking location: " + foundAFuckinLocation);
            Debug.Log("Narwhal: " + narwhal);
            Debug.Log("allison ecknhart");
            placementAttempts = 0;
            if (foundAFuckinLocation != blocSize) {
                Debug.Log("dipshit! fuck you!");
                //goto NewMino;
            }
            if (narwhal > 100) {
                Debug.Log("FAILURE");
                /*
                for (int gex = 0; gex < arrangement.Count(); gex++) {
                    arrangement[gex] = deranged[gex];
                }
                goto NewArrangement;
                */
            }
            for (int blow = 0; blow < blocSize; blow++) {
                arrangement[(gridPlace[0] + xOff[blow]) + (gridPlace[1] + yOff[blow]) * 13] = base36[dis];
            }
        }

        Debug.Log(new string(arrangement));
    }

    /*
    void keypadPress(KMSelectable object) {
        
    }
    */

}
