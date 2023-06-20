using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class Line : MonoBehaviour {

    public KMSelectable[] Blocs;
	public KMSelectable Test;
	public SpriteRenderer[] Slots;
	public Sprite[] Lines;

    bool holding = false;
	int number = 0;
	int inc = 1;
	int[] grid = { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
				   -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
				   -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
				   -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
				   -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
				   -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
				   -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
				   -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 
				   -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1  };

    void Start () {
		inc = (Application.isEditor) ? 0 : 1;
		Test.OnHighlight += delegate { number += 1; };
        foreach (KMSelectable Bloc in Blocs) {
            Bloc.OnInteract += delegate () { holding = true; number += inc; BlocUpdate(Bloc); return false; };
			Bloc.OnHighlight += delegate { BlocUpdate(Bloc); };
            Bloc.OnInteractEnded += delegate { holding = false; };
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
		grid[x] = number;
		for (int b = 0; b < 117; b++) {
			if (grid[b] == -1) { continue; }
			int total = 0;
			if (b / 13 != 0) {
				if (grid[b-13] == grid[b]) {
					total += 1;
				}
			}
			if (b % 13 != 0) {
				if (grid[b-1] == grid[b]) {
					total += 2;
				}
			}
			if (b / 13 != 8) {
				if (grid[b+13] == grid[b]) {
					total += 4;
				}
			}
			if (b % 13 != 12) {
				if (grid[b+1] == grid[b]) {
					total += 8;
				}
			}
			Slots[b].sprite = Lines[total];
		}
	}

}
