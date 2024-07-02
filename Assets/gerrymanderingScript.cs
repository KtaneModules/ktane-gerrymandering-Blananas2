using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Emik.Ktane.Gerrymandering;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

public class gerrymanderingScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Blocs;
    public KMSelectable Test;
	public SpriteRenderer[] Slots;
	public Sprite[] Lines;
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
    readonly int year = DateTime.Today.Year;
    int gridOffset = 0;
    string PrettyMatrix;
    bool holding = false;
	int marker = 0;
	int inc = 1;
    int[] lineGrid = { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                       -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    class Rng : OptimizedClosures.FSharpFunc<int, int, int> {
        public Rng(Func<int, int, int> func) {
            _func = func;
        }

        Func<int, int, int> _func;

        public override int Invoke(int low, int high) {
            return _func(low, high);
        }
    }

    void Awake () {
        moduleId = moduleIdCounter++;
        inc = (Application.isEditor) ? 0 : 1;
		Test.OnHighlight += delegate { marker += 1; };
        foreach (KMSelectable Bloc in Blocs) {
            Bloc.OnInteract += delegate () { holding = true; marker += inc; BlocUpdate(Bloc); return false; };
			Bloc.OnHighlight += delegate { BlocUpdate(Bloc); };
            Bloc.OnInteractEnded += delegate { holding = false; /*TODO: check for a solve here*/ };
        }
    }

    // Use this for initialization
    void Start () {
        //TODO: if application isnt editor remove test button automatically

        bluePreffered = UnityEngine.Random.Range(0, 2) == 0;
        Debug.LogFormat("[Gerrymandering #{0}] {1} is your party's color.", moduleId, bluePreffered ? "Blue" : "Orange");
        Envelope.sprite = EnvColors[bluePreffered ? 0 : 1];

        area = UnityEngine.Random.Range(0, 100);

        if      (area <  5) { area = 0; }
        else if (area < 20) { area = 1; }
        else if (area < 60) { area = 2; }
        else if (area < 85) { area = 3; }
        else                { area = 4; }
        //area = 4; //ZAMN

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
                if (area >= 2 & (xPos == 0 | yPos == 0))  { valid = false; gridOffset = 1; }
                if (area >= 3 & (xPos == 11 | yPos == 7)) { valid = false; }
                if (area == 4 & (xPos == 1 | yPos == 1))  { valid = false; gridOffset = 2; }
                
                BlocObjs[blok].SetActive(valid);
            }
        }
        BlocPivot.transform.localScale = new Vector3(scaleFactor, 1f, scaleFactor);

        chosenSize = UnityEngine.Random.Range(minSize, maxSize + 1);
        //chosenSize = 12; //ZAMN
        while ((chosenSize % 9) * (chosenSize % 7) * (chosenSize % 5) * (chosenSize % 3) != 0) {
            chosenSize = UnityEngine.Random.Range(minSize, maxSize + 1);
        }
        Text.text = String.Format("Fantasia {0} District\n({1} Voters)", year, chosenSize);
        while (chosenSize % blocSize != 0) {
            blocSize -= 1;
        }
        districts = chosenSize / blocSize;
        Debug.LogFormat("<Gerrymandering #{0}> There will be {1} {2}-minos, which is {3} blocs total.", moduleId, districts, blocSize, chosenSize);

        var Rand = new Rng((x, y) => UnityEngine.Random.Range(x, y));
        var Answer = new List<FSharpList<Tuple<int, int>>>();
        var Matrix = new Hue[height, width];
        var Puzzle = new Puzzle(Answer, Matrix, bluePreffered ? Hue.Blue : Hue.Orange);

        Debug.Assert(Puzzle.Run(Rand, blocSize, districts, TimeSpan.FromSeconds(1)));
        
        PrettyMatrix = Cell.ShowMatrix(Puzzle.Cells);
        Debug.LogFormat("[Gerrymandering #{0}] Given Matrix:\n{1}", moduleId, PrettyMatrix);
        ProcessMatrix();
    }

    void ProcessMatrix() {
        string AwfulMatrix = PrettyMatrix.Replace("+", " ").Replace("-", " ").Replace("|", " ").Replace("\n", ";");
        Debug.LogFormat("<Gerrymandering #{0}> Awfuled: \'{1}\'", moduleId, AwfulMatrix);
        string[] SplitAwful = AwfulMatrix.Split(';');
        Debug.LogFormat("<Gerrymandering #{0}> Splitted: \'{1}\'", moduleId, SplitAwful.Join("', '"));
        int zr = -1;
        int zc = -1;
        for (int ar = 1; ar < SplitAwful.Length; ar += 2) {
            zr++;
            for (int ac = 2; ac < SplitAwful[ar].Length; ac += 4) {
                zc++;
                switch (SplitAwful[ar][ac]) {
                    case ' ': BlocObjs[(zr + gridOffset)*13 + zc + gridOffset].SetActive(false); break;
                    case 'X': BlocObjs[(zr + gridOffset)*13 + zc + gridOffset].GetComponent<MeshRenderer>().material = PartyColors[0]; break;
                    case 'O': BlocObjs[(zr + gridOffset)*13 + zc + gridOffset].GetComponent<MeshRenderer>().material = PartyColors[1]; break;
                }
            }
            zc = -1;
        }
    }

    void BlocUpdate(KMSelectable Bloc) {
        for (int b = 0; b < Blocs.Length; b++) {
			if (Bloc == Blocs[b]) {
				UpdateLines(b);
			}
		}
    }

	void UpdateLines(int x) {
		if (!holding) { return; }
		lineGrid[x] = marker;
		for (int b = 0; b < 117; b++) {
			if (lineGrid[b] == -1) { continue; }
			int total = 0;
			if (b / 13 != 0) {
				if (lineGrid[b-13] == lineGrid[b]) {
					total += 1;
				}
			}
			if (b % 13 != 0) {
				if (lineGrid[b-1] == lineGrid[b]) {
					total += 2;
				}
			}
			if (b / 13 != 8) {
				if (lineGrid[b+13] == lineGrid[b]) {
					total += 4;
				}
			}
			if (b % 13 != 12) {
				if (lineGrid[b+1] == lineGrid[b]) {
					total += 8;
				}
			}
			Slots[b].sprite = Lines[total];
		}
	}
}
