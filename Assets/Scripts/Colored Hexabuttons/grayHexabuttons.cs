using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class grayHexabuttons : MonoBehaviour
{
    public KMBombModule module;
    public KMAudio Audio;
    private int moduleId;
    private static int moduleIdCounter = 1;
    public KMSelectable[] hexButtons;
    public MeshRenderer[] buttonMesh;
    public TextMesh[] buttonText;
    public Material[] ledColors;
    public MeshRenderer[] ledMesh;
    public Light centerLight;

    private int numButtonPresses;
    private bool submitMode;
    private int[] values;
    private int[] results;
    private static readonly string[] positions = { "TL", "TR", "ML", "MR", "BL", "BR" };
    private static readonly int[] buttonIndex = { 0, 1, 2, 3, 4, 5 };

    private static int calculateFunction(int function, int value, int constant)
    {
        switch (function)
        {
            case 0: return value + constant;
            case 1: return (2 * value) + constant;
            case 2: return value + (2 * constant);
            case 3: return value - constant;
            case 4: return (2 * value) - constant;
            case 5: return value - (2 * constant);
            default: throw new InvalidOperationException();
        }
    }

    private static IEnumerable<int[]> solve(int?[] values, int?[] functions, int[] results, int constant)
    {
        int ix;
        for (ix = 0; ix < 6; ix++)
        {
            if (values[ix] == null)
                goto tryValue;
            if (functions[ix] == null)
                goto tryFunction;
        }

        yield return functions.Select(f => f.Value).ToArray();
        yield break;

        tryValue:
        for (var val = 1; val <= 6; val++)
            if (!values.Contains(val))
            {
                if (functions[ix] != null && calculateFunction(functions[ix].Value, val, constant) != results[ix])
                    continue;
                var valuesCopy = values.ToArray();
                valuesCopy[ix] = val;
                foreach (var result in solve(valuesCopy, functions, results, constant))
                    yield return result;
            }
        yield break;

        tryFunction:
        for (var fn = 0; fn < 6; fn++)
            if (!functions.Contains(fn))
            {
                if (calculateFunction(fn, values[ix].Value, constant) != results[ix])
                    continue;
                var functionsCopy = functions.ToArray();
                functionsCopy[ix] = fn;
                foreach (var result in solve(values, functionsCopy, results, constant))
                    yield return result;
            }
    }

    private int? checkUnique(int?[] givenFunctions, int revealedValueIx, int constant)
    {
        var givenValues = new int?[6];
        givenValues[revealedValueIx] = values[revealedValueIx];
        var tentativeSolutions = solve(givenValues, givenFunctions, results, constant).Take(2).ToArray();
        return tentativeSolutions.Length == 1 ? (int?) null : Enumerable.Range(0, 6).Where(ix => revealedValueIx != ix && givenFunctions[ix] == null && tentativeSolutions[0][ix] != tentativeSolutions[1][ix]).PickRandom();
    }

    void Awake()
    {
        moduleId = moduleIdCounter++;

        foreach (int aa in buttonIndex)
            hexButtons[aa].OnInteract = delegate { pressedGray(aa); return false; };
        hexButtons[6].OnInteract = delegate { pressedGrayCenter(); return false; };
        hexButtons[6].OnInteractEnded = delegate { pressedGrayCenterRelease(); };

        // Decide on the constant, the values, the functions, and calculate their results
        var constant = Rnd.Range(1, 7);
        values = Enumerable.Range(1, 6).ToArray().Shuffle();
        var functions = Enumerable.Range(0, 6).ToArray().Shuffle();
        results = Enumerable.Range(0, 6).Select(ix => calculateFunction(functions[ix], values[ix], constant)).ToArray();
        var reveals = Enumerable.Range(0, 6).ToArray().Shuffle();

        // We will reveal exactly one value and at least one function
        var revealedValueIx = reveals[0];
        var revealedFunctionIx = reveals[1];

        // Algorithm to determine if we need to reveal any more functions in order to make the solution unique
        var revealedFunctions = new int?[6];
        revealedFunctions[revealedFunctionIx] = functions[revealedFunctionIx];
        while (true)
        {
            var uniquenessResult = checkUnique(revealedFunctions, revealedValueIx, constant);
            if (uniquenessResult == null)
                break;
            revealedFunctions[uniquenessResult.Value] = functions[uniquenessResult.Value];
        }

        // Put the revealed value and function(s) on the buttons
        for (var btn = 0; btn < 6; btn++)
        {
            buttonText[btn].text = "";
            if (revealedFunctions[btn] != null)
            {
                Debug.LogFormat("[Gray Hexabuttons #{0}] Hint: Button {1} has function {2}.", moduleId, positions[btn], (char) ('A' + revealedFunctions[btn].Value));
                buttonText[btn].text = ((char) ('A' + revealedFunctions[btn].Value)).ToString();
            }
            else if (btn == revealedValueIx)
            {
                Debug.LogFormat("[Gray Hexabuttons #{0}] Hint: Button {1} has value {2}.", moduleId, positions[btn], values[revealedValueIx]);
                buttonText[btn].text = values[revealedValueIx].ToString();
            }
        }

        Debug.LogFormat("[Gray Hexabuttons #{0}] Constant: {1}", moduleId, constant);
        Debug.LogFormat("[Gray Hexabuttons #{0}] Values: {1}", moduleId, values.Join());
        Debug.LogFormat("[Gray Hexabuttons #{0}] Functions: {1}", moduleId, functions.Select(fn => (char) ('A' + fn)).Join());
        Debug.LogFormat("[Gray Hexabuttons #{0}] Results: {1}", moduleId, results.Join(", "));

        submitMode = false;
        centerLight.color = Color.white;
        centerLight.enabled = false;
    }
    int[] getResults(int[] x, int con, string funct)
    {
        int[] r = new int[6];
        for (int aa = 0; aa < x.Length; aa++)
        {
            switch (funct[aa])
            {
                case 'A':
                    r[aa] = (x[aa] + con);
                    break;
                case 'B':
                    r[aa] = ((2 * x[aa]) + con);
                    break;
                case 'C':
                    r[aa] = (x[aa] + (2 * con));
                    break;
                case 'D':
                    r[aa] = (x[aa] - con);
                    break;
                case 'E':
                    r[aa] = ((2 * x[aa]) - con);
                    break;
                case 'F':
                    r[aa] = (x[aa] - (2 * con));
                    break;
            }
        }
        return r;
    }
    string getRepeats(int[] r, int cur)
    {
        string repeats = cur + "";
        for (int aa = cur + 1; aa < 6; aa++)
        {
            if (r[cur] == r[aa])
                repeats = repeats + "" + aa;
        }
        return repeats;
    }
    void Start()
    {
        float scalar = transform.lossyScale.x;
        centerLight.range *= scalar;
    }
    void pressedGray(int n)
    {
        foreach (int aa in buttonIndex)
        {
            Vector3 pos = buttonMesh[aa].transform.localPosition;
            pos.y = 0.0169f;
            buttonMesh[aa].transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
            ledMesh[aa].material = ledColors[0];
            hexButtons[aa].OnInteract = delegate { pressedGray(buttonIndex[aa]); return false; };
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Vector3 pos2 = buttonMesh[n].transform.localPosition;
        pos2.y = 0.0126f;
        buttonMesh[n].transform.localPosition = new Vector3(pos2.x, pos2.y, pos2.z);
        hexButtons[n].OnInteract = null;
        ledMesh[n].material = ledColors[1];
        buttonText[6].text = (results[n] + "");
    }
    void pressedGraySubmit(int n)
    {
        Debug.LogFormat("[Gray Hexabuttons #{0}] User submitted {1} which has the value of {2}.", moduleId, positions[n], values[n]);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (values[n] == (numButtonPresses + 1))
        {
            Vector3 pos = buttonMesh[n].transform.localPosition;
            pos.y = 0.0126f;
            buttonMesh[n].transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
            numButtonPresses++;
            hexButtons[n].OnInteract = null;
            ledMesh[n].material = ledColors[1];
            if (numButtonPresses == 6)
            {
                submitMode = false;
                hexButtons[6].OnInteract = null;
                hexButtons[6].OnInteractEnded = null;
                module.HandlePass();
            }
        }
        else
        {
            Debug.LogFormat("[Gray Hexabuttons #{0}] Strike! I was expecting a value of {1}!", moduleId, (numButtonPresses + 1));
            module.HandleStrike();
            foreach (int i in buttonIndex)
            {
                Vector3 pos = buttonMesh[i].transform.localPosition;
                pos.y = 0.0169f;
                buttonMesh[i].transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
                hexButtons[i].OnInteract = delegate { pressedGray(i); return false; };
                ledMesh[i].material = ledColors[0];
            }
            submitMode = false;
        }
    }
    void pressedGrayCenter()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Vector3 pos = buttonMesh[6].transform.localPosition;
        pos.y = 0.0126f;
        buttonMesh[6].transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
        buttonText[6].text = "";
        if (submitMode)
        {
            submitMode = false;
            foreach (int i in buttonIndex)
            {
                Vector3 pos2 = buttonMesh[i].transform.localPosition;
                pos2.y = 0.0169f;
                buttonMesh[i].transform.localPosition = new Vector3(pos2.x, pos2.y, pos2.z);
                hexButtons[i].OnInteract = delegate { pressedGray(i); return false; };
                ledMesh[i].material = ledColors[0];
            }
        }
        else
        {
            submitMode = true;
            numButtonPresses = 0;
            foreach (int i in buttonIndex)
            {
                Vector3 pos2 = buttonMesh[i].transform.localPosition;
                pos2.y = 0.0169f;
                buttonMesh[i].transform.localPosition = new Vector3(pos2.x, pos2.y, pos2.z);
                ledMesh[i].material = ledColors[0];
                hexButtons[i].OnInteract = delegate { pressedGraySubmit(i); return false; };
                hexButtons[i].OnInteractEnded = null;
            }
            StartCoroutine(grayFlasher());
        }
    }
    void pressedGrayCenterRelease()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
        Vector3 pos = buttonMesh[6].transform.localPosition;
        pos.y = 0.0169f;
        buttonMesh[6].transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
    }
    IEnumerator grayFlasher()
    {
        yield return new WaitForSeconds(1.0f);
        while (submitMode)
        {
            centerLight.enabled = true;
            yield return new WaitForSeconds(1.0f);
            centerLight.enabled = false;
            yield return new WaitForSeconds(1.0f);
        }
        centerLight.enabled = false;
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press|p tl/1 tr/2 ml/3 mr/4 bl/5 br/6 c/7 presses the top-left, top-right, middle-left, middle-right, bottom-left, bottom-right, and center buttons in that order.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] param = command.ToUpper().Split(' ');
        if ((Regex.IsMatch(param[0], @"^\s*PRESS\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(param[0], @"^\s*P\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) && param.Length > 1)
        {
            if (isPos(param))
            {
                yield return null;
                for (int i = 1; i < param.Length; i++)
                {
                    int cursor = -1;
                    switch (param[i])
                    {
                        case "TL":
                        case "1":
                            cursor = 0;
                            break;
                        case "TR":
                        case "2":
                            cursor = 1;
                            break;
                        case "ML":
                        case "3":
                            cursor = 2;
                            break;
                        case "MR":
                        case "4":
                            cursor = 3;
                            break;
                        case "BL":
                        case "5":
                            cursor = 4;
                            break;
                        case "BR":
                        case "6":
                            cursor = 5;
                            break;
                        case "C":
                        case "7":
                            cursor = 6;
                            break;
                    }
                    if (hexButtons[cursor].OnInteract != null)
                    {
                        hexButtons[cursor].OnInteract();
                        yield return new WaitForSeconds(0.2f);
                        if (hexButtons[cursor].OnInteractEnded != null)
                        {
                            hexButtons[cursor].OnInteractEnded();
                            yield return new WaitForSeconds(0.2f);
                        }
                    }
                }
            }
        }
    }
    private bool isPos(string[] param)
    {
        for (int aa = 1; aa < param.Length; aa++)
        {
            switch (param[aa])
            {
                case "TL":
                case "TR":
                case "ML":
                case "MR":
                case "BL":
                case "BR":
                case "C":
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                    break;
                default:
                    return false;
            }
        }
        return true;
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        if (!submitMode)
        {
            hexButtons[6].OnInteract();
            yield return new WaitForSeconds(0.2f);
            hexButtons[6].OnInteractEnded();
            yield return new WaitForSeconds(0.2f);
        }
        while (submitMode)
        {
            hexButtons[Array.IndexOf(values, numButtonPresses + 1)].OnInteract();
            yield return new WaitForSeconds(0.2f);
        }
    }
}
