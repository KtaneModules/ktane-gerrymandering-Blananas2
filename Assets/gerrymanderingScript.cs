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
    int seed;
    readonly int year = DateTime.Today.Year;
    bool[] shutUp = { false, false };
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

    // For TP
    List<List<GameObject>> TPGroupedBlocs;

    void Awake () {
        moduleId = moduleIdCounter++;
        inc = Application.isEditor ? 0 : 1;
		Test.OnHighlight += delegate { marker++; };
        foreach (KMSelectable Bloc in Blocs) {
            Bloc.OnInteract += delegate () { holding = true; marker += inc; BlocUpdate(Bloc); return false; };
			Bloc.OnHighlight += delegate { if (holding) { BlocUpdate(Bloc); } };
            Bloc.OnInteractEnded += delegate { holding = false; };
        }
        Bomb.OnBombExploded += delegate { LogFail("explosion"); };
    }

    void OnDestroy () { LogFail("leaving"); }

    // Use this for initialization
    void Start () {
        //TODO: Implement some system to more easily "rig" the module for testing purposes. Rigging the seed is fine, but everything else (preffered winner, area, size) is cumbersome as you have to look for it in the code.

        foreach (var j in BlocObjs) {
            j.SetActive(false);
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
        minSize = ((height/2) - 1) * width + (area == 4 ? 1 : 0);
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
                
                if (area >= 2 & (xPos == 0 | yPos == 0))  { gridOffset = 1; }
                if (area == 4 & (xPos == 1 | yPos == 1))  { gridOffset = 2; }
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

        Debug.LogFormat("[Gerrymandering #{0}] There will be {1} districts with {2} blocs each, which is {3} blocs total.", moduleId, districts, blocSize, chosenSize);

        var Answer = new List<FSharpList<Tuple<int, int>>>();
        var Matrix = new Hue[height, width];
        puzzle = new Puzzle(Answer, Matrix, bluePreffered ? Hue.Blue : Hue.Orange);

        StartCoroutine(GeneratePuzzle());
        Debug.LogFormat("[Gerrymandering #{0}] Our constitution declares we use this random value: {1}", moduleId, seed);
        
        PrettyMatrix = Cell.ShowMatrix(puzzle.Cells);
        Debug.LogFormat("[Gerrymandering #{0}] Given map:\n{1}", moduleId, PrettyMatrix);
        ProcessMatrix();
    }

    IEnumerator GeneratePuzzle() {
        while (true) {
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            if (puzzle.Run(new System.Random(seed), blocSize, districts, TimeSpan.FromMilliseconds(100))) {
                yield break;
            }

            Debug.LogFormat("<Gerrymandering #{0}> Seed didn't manage to generate in 100ms: {1}", moduleId, seed);
            yield return null;
        }  
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
        TPGroupedBlocs = Enumerable.Range(0, BlocObjs.Length / 13)
            .Select(a => BlocObjs.Skip(13 * a).Take(13).ToList()).ToList(); // Grab basically an entire grid of selectables.
        while (TPGroupedBlocs.Any() && !TPGroupedBlocs.First().Any(b => b.activeSelf)) // Trim the top-most rows that do not have any selectables.
            TPGroupedBlocs.RemoveAt(0);
        while (TPGroupedBlocs.Any() && !TPGroupedBlocs.Last().Any(b => b.activeSelf)) // Trim the bottom-most rows that do not have any selectables.
            TPGroupedBlocs.RemoveAt(TPGroupedBlocs.Count - 1);
        while (TPGroupedBlocs.Any(a => a.Any()) && !TPGroupedBlocs.Select(a => a.First()).Any(a => a.activeSelf)) // Trim the left-most columns that do not have any selectables.
            for (var x = 0; x < TPGroupedBlocs.Count; x++)
                TPGroupedBlocs[x].RemoveAt(0);
        while (TPGroupedBlocs.Any(a => a.Any()) && !TPGroupedBlocs.Select(a => a.Last()).Any(a => a.activeSelf)) // Trim the right-most columns that do not have any selectables.
            for (var x = 0; x < TPGroupedBlocs.Count; x++)
                TPGroupedBlocs[x].RemoveAt(TPGroupedBlocs[x].Count - 1);
    }

    void BlocUpdate(KMSelectable Bloc) {
        if (moduleSolved) { return; }
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
                Debug.LogFormat("[Gerrymandering #{0}] Submitted districts:\n{1}", moduleId, LogSubmission());
                moduleSolved = true;
                Debug.LogFormat("[Gerrymandering #{0}] Valid districts given, module solved.", moduleId);
                GetComponent<KMBombModule>().HandlePass();
                Audio.PlaySoundAtTransform("done", transform);
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
                if (!shutUp[0]) {
                    Debug.LogFormat("<Gerrymandering #{0}> Found district with invalid number of blocs, incorrect.", moduleId); 
                    shutUp[0] = true;
                }
                return false;
            }
            if (!Orth(gridIx, atIx)) {
                if (!shutUp[1]) {
                    Debug.LogFormat("<Gerrymandering #{0}> Found district with blocs not orthogonally adjacent, incorrect.", moduleId);
                    shutUp[1] = true;
                }
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
                    q = -1;
                    continue;
                }
            }
            if (q % 13 != 0) {
                if (lineGrid[q-1] == v && gridTWO[q-1] == -1) {
                    gridTWO[q-1] = 1;
                    count++;
                    q = -1;
                    continue;
                }
            }
            if (q % 13 != 12) {
                if (lineGrid[q+1] == v && gridTWO[q+1] == -1) {
                    gridTWO[q+1] = 1;
                    count++;
                    q = -1;
                    continue;
                }
            }
            if (q / 13 != 8) {
                if (lineGrid[q+13] == v && gridTWO[q+13] == -1) {
                    gridTWO[q+13] = 1;
                    count++;
                    q = -1;
                    continue;
                }
            }
        }
        return count == blocSize;
    }

    bool Pref(int v) {
        int[] subsides = { 0, 0 };

        for (int h = 0; h < height; h++) {
            for (int w = 0; w < width; w++) {
                if (lineGrid[(h + gridOffset)*13 + w + gridOffset] == v) {
                    var _ = puzzle.Matrix[h, w].IsBlue ? subsides[0]++ : subsides[1]++;
                }
            }
        }
        return subsides[0] > subsides[1];
    }

    string LogSubmission() {
        string str = "";
        int[] values = new int[chosenSize];
        int discovered = 0;
        const string bet = "abcdefghijklmnopqrstuvwxyzàáâãäåāăąǻȁȃạảấầẩẫậắằẳẵặçćĉċčďđèéêëēĕėęěȅȇẹẻẽếềểễệĝğġģǧĥħĩīĭįìíîïȉȋỉịĵķĺļľŀłñńņňŋòóôõöøōŏőơǫǿȍȏȫȭȱọỏốồổỗộớờởỡợŕŗřȑȓśŝşšșţťŧùúûüțũūŭůűųưȕȗụủứừửữựŵẁẃẅýÿŷȳỳỵỷỹźżžæǽǆĳǉǌœ"; //definitely overboard

        string r = "+";
        for (int w = 0; w < width; w++) { r += "---+"; }
        r += "\n";
        str += r;
        for (int h = 0; h < height; h++) {
            for (int w = 0; w < width; w++) {
                int relevant = lineGrid[(h + gridOffset)*13 + w + gridOffset];
                if (relevant == -1) {
                    str += "|   ";
                } else if (values.Contains(relevant)) {
                    str += "| " + bet[Array.IndexOf(values, relevant)] + " ";
                } else {
                    values[discovered] = relevant;
                    str += "| " + bet[discovered] + " ";
                    discovered++;
                }
            }
            str += "|\n" + r;
        }

        return discovered == 0 ? "" : str;
    }

    void LogFail(string r) {
        if (moduleSolved) { return; } 
        string sub = LogSubmission();
        if (sub != "") {
            Debug.LogFormat("[Gerrymandering #{0}] Upon {1}, this is what the districts looked like:\n{2}", moduleId, r, LogSubmission());
        }
    }

#pragma warning disable IDE0051 // Remove unused private members
    static readonly string TwitchHelpMessage = "\"!{0} A1 B1 B2 C1; A2 A3 B3 C3\" [Creates a district with blocs A1, B1, B2, C1; then another with A2, A3, B3, C3. Specify letter as column from left to right, number as row from top to bottom. Semicolons (\";\") separate districts when chaining. Coordinates skip over empty spaces, so long as its entire row or column is empty.]";
#pragma warning restore IDE0051 // Remove unused private members

    IEnumerator ProcessTwitchCommand(string cmd)
    {
        var intCmd = cmd.Trim();
        var rgxCoord = Regex.Match(intCmd, @"^[a-m][1-9](;?\s?[a-m][1-9])*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (rgxCoord.Success)
        {
            var obtainedCoordsSplit = rgxCoord.Value.Split(';').Select(a => a.Trim().Split().ToList()).ToList();
            var idxesToUse = new List<List<int>>();
            var BlocObjList = BlocObjs.ToList();
            foreach (var coordGroup in obtainedCoordsSplit)
            {
                var newGroup = new List<int>();
                foreach (var coord in coordGroup)
                {
                    var lowerCasedCoord = coord.ToLowerInvariant();
                    var rowIdx = "123456789".IndexOf(lowerCasedCoord[1]);
                    var colIdx = "abcdefghijklm".IndexOf(lowerCasedCoord[0]);


                    var oob = rowIdx >= TPGroupedBlocs.Count || rowIdx < 0;
                    outOfBounds:
                    if (oob)
                    {
                        yield return string.Format("sendtochaterror The specified coordinate {0} corresponds to a bloc outside our country!", coord);
                        yield break;
                    }
                    var obtainedRow = TPGroupedBlocs[rowIdx];
                    if (colIdx < 0 || colIdx >= obtainedRow.Count)
                    {
                        oob = true;
                        goto outOfBounds;
                    }
                    var obtainedObj = obtainedRow[colIdx];
                    if (!obtainedObj.activeSelf)
                    {
                        yield return string.Format("sendtochaterror The specified coordinate {0} corresponds to a bloc outside our country!", coord);
                        yield break;
                    }
                    newGroup.Add(BlocObjList.IndexOf(obtainedObj));
                }
                idxesToUse.Add(newGroup);
            }
            yield return null;
            for (var x = 0; x < idxesToUse.Count; x++)
            {
                var curGroup = idxesToUse[x];
                for (var y = 0; y < curGroup.Count; y++)
                {
                    var idxInGroup = idxesToUse[x][y];
                    if (y == 0)
                        Blocs[idxInGroup].OnInteract();
                    Blocs[idxInGroup].OnHighlight();
                    if (y + 1 >= curGroup.Count)
                    {
                        Blocs[curGroup.First()].OnInteractEnded();
                        if (Application.isEditor)
                            Test.OnHighlight();
                    }
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        var solution = puzzle.Answer;
        foreach (var group in solution) // A list of coordinates as a Tuple, in that group.
        {
            var allSelectableIdxes = group.Select(a => (a.Item1 + gridOffset) * 13 + a.Item2 + gridOffset).ToList();
            for (var x = 0; x < allSelectableIdxes.Count; x++)
            {
                if (x == 0)
                    Blocs[allSelectableIdxes[x]].OnInteract();
                Blocs[allSelectableIdxes[x]].OnHighlight();
                if (x + 1 >= allSelectableIdxes.Count)
                {
                    Blocs[allSelectableIdxes.First()].OnInteractEnded();
                    if (Application.isEditor)
                        Test.OnHighlight();
                }
                yield return new WaitForSeconds(0.05f);
            }
        }
        yield break;
    }
}
