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
    public GameObject TestObj;
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
    string TheColorsOfMatrix = "";
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
    Puzzle puzzle;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;
        inc = Application.isEditor ? 0 : 1;
		Test.OnHighlight += delegate { marker += 1; };
        foreach (KMSelectable Bloc in Blocs) {
            Bloc.OnInteract += delegate () { holding = true; marker += inc; BlocUpdate(Bloc); return false; };
			Bloc.OnHighlight += delegate { BlocUpdate(Bloc); };
            Bloc.OnInteractEnded += delegate { holding = false; };
        }
    }

    // Use this for initialization
    void Start () {
        foreach (var sel in BlocObjs) {
            sel.SetActive(false);
        }

        TestObj.SetActive(Application.isEditor);

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
        maxSize = (height - 2) * (width - 2);

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
                
                //BlocObjs[blok].SetActive(valid);
            }
        }
        BlocPivot.transform.localScale = new Vector3(scaleFactor, 1f, scaleFactor);

        chosenSize = UnityEngine.Random.Range(minSize, maxSize + 1);
        while (((chosenSize % 9) * (chosenSize % 7) * (chosenSize % 5) * (chosenSize % 3) != 0) || ((chosenSize % 2) == 0)) {
            chosenSize = UnityEngine.Random.Range(minSize, maxSize + 1);
        }
        Text.text = String.Format("Fantasia {0} District\n({1} Voters)", year, chosenSize);
        while (chosenSize % blocSize != 0) {
            blocSize -= 1;
        }
        districts = chosenSize / blocSize;

        Debug.LogFormat("<Gerrymandering #{0}> There will be {1} {2}-minos, which is {3} blocs total.", moduleId, districts, blocSize, chosenSize);

        var seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        var Answer = new List<FSharpList<Tuple<int, int>>>();
        var Matrix = new Hue[height, width];
        puzzle = new Puzzle(Answer, Matrix, bluePreffered ? Hue.Blue : Hue.Orange);

        Debug.Assert(puzzle.Run(new System.Random(seed), blocSize, districts, TimeSpan.FromMilliseconds(100)));
        Debug.LogFormat("[Gerrymandering #{0}] Seed used: {1}", moduleId, seed);
        
        PrettyMatrix = Cell.ShowMatrix(puzzle.Cells);
        Debug.LogFormat("[Gerrymandering #{0}] Given Matrix:\n{1}", moduleId, PrettyMatrix);
        ProcessMatrix();
    }

    void ProcessMatrix() {
        foreach (var answer in  puzzle.Answer.SelectMany(x => x))
        {
            var obj = BlocObjs[(answer.Item1 + gridOffset) * 13 + answer.Item2 + gridOffset];
            var renderer = obj.GetComponent<MeshRenderer>();
            var hue = puzzle.Cells[answer.Item1, answer.Item2].Hue;
            renderer.material = puzzle.Cells[answer.Item1, answer.Item2].Hue.IsBlue ? PartyColors[0] : PartyColors[1];
            obj.SetActive(true);
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
        int lineCount = 0;
		for (int b = 0; b < 117; b++) {
			if (lineGrid[b] == -1) { continue; }
            lineCount++;
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
        if (lineCount == chosenSize) {
            if (CheckLineValidity()) {
                Debug.LogFormat("[Gerrymandering #{0}] {1}", moduleId, lineGrid.Join(",")); //this can be done WAYYYYYYYYY better
                Debug.LogFormat("[Gerrymandering #{0}] Valid districts given, module solved.", moduleId);
                GetComponent<KMBombModule>().HandlePass();
            }
        }
	}

    bool CheckLineValidity() {
        List<int> ids = new List<int> { };
        int[] sides = { 0, 0 };

        for (int gridIx = 0; gridIx < 117; gridIx++) {
            int atIx = lineGrid[gridIx];
            if (atIx == -1 || ids.Contains(atIx)) { continue; }
            if (lineGrid.Where(e => e == atIx).Count() != blocSize) {
                Debug.Log("<gery> found district with invalid number of blocs");
                return false;
            }
            if (!Orth(gridIx, atIx)) {
                Debug.Log("<gery> found district with blocs not orthogonally adjacent");
                return false;
            }
            sides[Pref(atIx) ? 0 : 1]++;

            ids.Add(atIx);
        }

        return bluePreffered ? (sides[0] > sides[1]) : (sides[0] < sides[1]);
    }

    bool Orth(int g, int v) {
        int count = 1;
        int[] gridTWO = { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                          -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                          -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                          -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                          -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                          -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                          -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                          -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
                          -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        gridTWO[g] = 1;
        for (int q = 0; q < 117; q++) {
            if (gridTWO[q] == -1) { continue; }
            if (q / 13 != 0) {
                if (lineGrid[q-13] == v && gridTWO[q-13] == -1) {
                    gridTWO[q-13] = 1;
                    count++;
                    q = 0;
                    continue;
                }
            }
            if (q % 13 != 0) {
                if (lineGrid[q-1] == v && gridTWO[q-1] == -1) {
                    gridTWO[q-1] = 1;
                    count++;
                    q = 0;
                    continue;
                }
            }
            if (q % 13 != 12) {
                if (lineGrid[q+1] == v && gridTWO[q+1] == -1) {
                    gridTWO[q+1] = 1;
                    count++;
                    q = 0;
                    continue;
                }
            }
            if (q / 13 != 8) {
                if (lineGrid[q+13] == v && gridTWO[q+13] == -1) {
                    gridTWO[q+13] = 1;
                    count++;
                    q = 0;
                    continue;
                }
            }
        }
        return count == blocSize;
    }

    bool Pref(int v) {
        //TODO: rewrite to take into account that Matrix uses [height, width] and that gridOffset exists
        
        /*
        int[] subsides = { 0, 0 };
        
        for (int q = 0; q < puzzle.Matrix; q++) {
            var _ = puzzle.Matrix[q / 13, q % 13].IsBlue ? subsides[0]++ : subsides[1]++;
        }
        return subsides[0] > subsides[1];
        */
        return false;
    }
}
