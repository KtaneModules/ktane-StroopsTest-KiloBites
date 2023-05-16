using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class StroopsTestScript : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;

    public KMSelectable[] buttons;
    public TextMesh screenText;
    public GameObject[] buttonObjs;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private bool isActivated;

    private bool[] pressed = new bool[2];
    private bool[] alreadyAnswered = new bool[6];
    private bool pause;

    private readonly Coroutine[] pressAnimations = new Coroutine[2];
    private int stage;
    private int? firstColor, firstWord;
    private int currentCond, questionCond;

    private static readonly Color[] colors = { Color.red, Color.yellow, Color.green, Color.blue, Color.magenta, Color.white };

    private static readonly string[] colorNames = { "Red", "Yellow", "Green", "Blue", "Magenta", "White" };

    private List<int> wordList = new List<int>();
    private List<int> colorList = new List<int>();

    private Coroutine _resetTimer;
    private Coroutine _generateSequence;

    void Awake()
    {

        moduleId = moduleIdCounter++;

        foreach (KMSelectable button in buttons)
        {
            button.OnInteract += delegate () { buttonPress(button); return false; };
        }

        Module.OnActivate += onActivate;

        buttons[1].OnHighlight = delegate ()
        {
            if (!moduleSolved && stage > 0 && isActivated)
                _resetTimer = StartCoroutine(standBy());
        };
        buttons[1].OnHighlightEnded = delegate ()
        {
            if (!moduleSolved && _resetTimer != null && isActivated)
                StopCoroutine(_resetTimer);
        };

    }


    void Start()
    {
        screenText.text = "";
        determineCondition();
    }

    void onActivate()
    {
        _generateSequence = StartCoroutine(generateSeq());
    }

    IEnumerator standBy()
    {
        var duration = 4.5f;
        var elapsed = 0f;
        while (elapsed <= duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        Audio.PlaySoundAtTransform("Reset", transform);
        if (_generateSequence != null)
            StopCoroutine(_generateSequence);
        wordList.Clear();
        colorList.Clear();
        screenText.text = "";
        isActivated = false;
        Debug.LogFormat("[Stroop's Test #{0}] Reset has been initiated! Resetting back to Stage 1.", moduleId);
        var arr = new[] { ".", "..", "..." };
        screenText.color = Color.white;
        for (int i = 0; i < 9; i++)
        {
            screenText.text = "Reset" + arr[i % 3];
            yield return new WaitForSeconds(0.42f);
        }
        screenText.text = "";
        yield return new WaitForSeconds(0.75f);
        reset();
    }

    void reset()
    {
        firstWord = null;
        firstColor = null;
        stage = 0;
        for (int i = 0; i < 6; i++)
        {
            alreadyAnswered[i] = false;
        }
        for (int i = 0; i < 2; i++)
        {
            pressed[i] = false;
        }
        if (_generateSequence != null)
            StopCoroutine(_generateSequence);
        _generateSequence = StartCoroutine(generateSeq());
    }

    IEnumerator generateSeq()
    {
        while (true)
        {
            var textIx = rnd.Range(0, colors.Length);
            var colorIx = rnd.Range(0, colors.Length);

            if (wordList.Count > 20 && colorList.Count > 20)
            {
                for (var i = 0; i < 10; i++)
                {
                    wordList.RemoveAt(i);
                    colorList.RemoveAt(i);
                }
            }

            if (wordList.Count >= 1 && colorList.Count >= 1)
            {
                isActivated = true;
                while (wordList.Last() == textIx && colorList.Last() == colorIx)
                {
                    textIx = rnd.Range(0, colors.Length);
                    colorIx = rnd.Range(0, colors.Length);
                }
            }

            wordList.Add(textIx);
            colorList.Add(colorIx);

            screenText.text = colorNames[textIx];
            screenText.color = colors[colorIx];
            yield return new WaitForSeconds(1.55f);
        }
    }

    IEnumerator submittedDisplay()
    {
        yield return null;
        pause = true;
        screenText.text = colorNames[wordList.Last()];
        screenText.color = colors[colorList.Last()];
        yield return new WaitForSeconds(2);
        pause = false;
        if (_generateSequence != null)
            StopCoroutine(_generateSequence);
        _generateSequence = StartCoroutine(generateSeq());
    }

    bool validCond(int question, int color)
    {
        switch (question)
        {
            case 0:
                switch (color)
                {
                    case 0:
                        return firstWord != null && firstWord == wordList.Last();
                    case 1:
                        return wordList.Last() != 4 && colorList.Last() != 4 && wordList.Last() != 5 && colorList.Last() != 5;
                    case 2:
                        return colorList[colorList.Count - 2] == colorList.Last();
                    case 3:
                        return wordList[wordList.Count - 2] == wordList.Last();
                    case 4:
                        return firstColor != null && firstColor == wordList.Last();
                    case 5:
                        var values = new int[4]
                        {
                            wordList.Last(),
                            wordList[wordList.Count - 2],
                            colorList.Last(),
                            colorList[colorList.Count - 2]
                        };
                        return values.Distinct().Count() == 4;
                }
                break;
            case 1:

                switch (color)
                {
                    case 0:
                        return colorList[colorList.Count - 2] == wordList.Last();
                    case 1:
                        return firstColor != null && firstColor == colorList.Last();
                    case 2:
                        return wordList.Last() != 0 && colorList.Last() != 0 && wordList.Last() != 3 && colorList.Last() != 3;
                    case 3:
                        return wordList.Last() != 2 && colorList.Last() != 2 && wordList.Last() != 1 && colorList.Last() != 1;
                    case 4:
                        return wordList[wordList.Count - 2] == wordList.Last();
                    case 5:
                        return firstWord != null && firstWord == colorList.Last();
                }
                break;
        }
        return false;
    }

    string validLogging(int question, int color, int pos, bool valid)
    {
        var button = pos == 0 ? "YES" : "NO";

        switch (question)
        {
            case 0:
                switch (color)
                {
                    case 0:
                        return string.Format("You pressed {0} while the first submitted word ({1}) {2} the current slide's word ({3}).", button, firstWord == null ? "doesn't exist" : colorNames[firstWord.Value], valid ? "matches" : "doesn't match", colorNames[wordList.Last()]);
                    case 1:
                        return string.Format("You pressed {0} while the current slide's word ({1}) and the current slide's text color ({2}) {3}.", button, colorNames[wordList.Last()], colorNames[colorList.Last()], valid ? "isn't Magenta nor White" : "is Magenta or White");
                    case 2:
                        return string.Format("You pressed {0} while the color on the previous slide ({1}) {2} the current slide's text color ({3}).", button, colorNames[colorList[colorList.Count - 2]], valid ? "matches" : "doesn't match", colorNames[colorList.Last()]);
                    case 3:
                        return string.Format("You pressed {0} while the word on the previous slide ({1}) {2} the current slide's text color ({3}).", button, colorNames[wordList[wordList.Count - 2]], valid ? "matches" : "doesn't match", colorNames[colorList.Last()]);
                    case 4:
                        return string.Format("You pressed {0} while the first submitted text color ({1}) {2} the current slide's word ({3}).", button, firstColor == null ? "doesn't exist" : colorNames[firstColor.Value], valid ? "matches" : "doesn't match", colorNames[wordList.Last()]);
                    case 5:
                        return string.Format("You pressed {0} while the current word ({1}), current text color ({2}), previous word ({3}), and previous text color ({4}) {5} distinct.", button, colorNames[wordList.Last()], colorNames[colorList.Last()], colorNames[wordList[wordList.Count - 2]], colorNames[colorList[colorList.Count - 2]], valid ? "are" : "aren't");
                }
                break;
            case 1:
                switch (color)
                {
                    case 0:
                        return string.Format("You pressed {0} while text color on the previous slide ({1}) {2} the current slide's word ({3}).", button, colorNames[colorList[colorList.Count - 2]], valid ? "matches" : "doesn't match", colorNames[wordList.Last()]);
                    case 1:
                        return string.Format("You pressed {0} while the first submitted word ({1}) {2} the current slide's text color ({3}).", button, firstWord == null ? "doesn't exist" : colorNames[firstWord.Value], valid ? "matches" : "doesn't match", colorNames[colorList.Last()]);
                    case 2:
                        return string.Format("You pressed {0} while the current slide's word ({1}) and the current slide's text color ({2}) {3}.", button, colorNames[wordList.Last()], colorNames[colorList.Last()], valid ? "isn't Red nor Blue" : "is Red or Blue");
                    case 3:
                        return string.Format("You pressed {0} while the current slide's word ({1}) and the current slide's text color ({2}) {3}.", button, colorNames[wordList.Last()], colorNames[colorList.Last()], valid ? "isn't Green nor Yellow" : "is Green or Yellow");
                    case 4:
                        return string.Format("You pressed {0} while the word on the previous slide ({1}) {2} the current slide's word ({3}).", button, colorNames[wordList[wordList.Count - 2]], valid ? "matches" : "doesn't match", colorNames[wordList.Last()]);
                    case 5:
                        return string.Format("You pressed {0} while the first submitted word ({1}) {2} the current slide's text color ({3}).", button, firstWord == null ? "doesn't exist" : colorNames[firstWord.Value], valid ? "matches" : "doesn't match", colorNames[colorList.Last()]);
                }
                break;
        }

        return null;
    }

    void buttonPress(KMSelectable button)
    {
        Audio.PlaySoundAtTransform("ButtonClick", transform);

        for (int i = 0; i < 2; i++)
        {
            if (button == buttons[i])
            {
                pressAnimations[i] = StartCoroutine(pressAnimation(i));
            }
        }

        if (moduleSolved || !isActivated || stage == 3 || pause)
        {
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            if (button == buttons[i])
            {
                if (i == 0)
                {
                    yesPress();
                }
                else
                {
                    noPress();
                }
            }
        }

        if (stage == 3)
        {
            StopAllCoroutines();
            StartCoroutine(solveAnimation());
        }
    }

    void yesPress()
    {
        var validation = validCond(questionCond, currentCond == 0 ? colorList.Last() : wordList.Last());

        var ix = currentCond == 0 ? colorList.Last() : wordList.Last();

        if (validation && !pressed[0] && !alreadyAnswered[ix])
        {
            if (stage != 0)
            {
                pressed[0] = true;
                alreadyAnswered[ix] = true;
            }
            if (stage == 0)
            {
                firstColor = colorList.Last();
                firstWord = wordList.Last();
                alreadyAnswered[ix] = true;
                Debug.LogFormat("[Stroop's Test #{0}] First submitted word and color is {1} in {2}.", moduleId, colorNames[firstWord.Value], colorNames[firstColor.Value]);
            }
            stage++;
            if (stage != 3)
            {
                StopAllCoroutines();
                StartCoroutine(submittedDisplay());
            }
            Debug.LogFormat("[Stroop's Test #{0}] {1} {2}", moduleId, validLogging(questionCond, currentCond == 0 ? colorList.Last() : wordList.Last(), 0, validation), stage != 3 ? string.Format("Advancing to stage {0}.", (stage + 1).ToString()) : "Solved!");

        }
        else
        {
            Module.HandleStrike();

            if (pressed[0])
            {
                Debug.LogFormat("[Stroop's Test #{0}] You pressed YES when you already have a valid YES pressed previous stage. {1}", moduleId, stage != 0 ? "Resetting to stage 1." : null);
            }
            else if (alreadyAnswered[ix])
            {
                Debug.LogFormat("[Stroop's Test #{0}] You pressed YES when the previous question submitted is already answered! Strike! {1}", moduleId, stage != 0 ? "Resetting to stage 1." : null);
            }
            else
            {
                Debug.LogFormat("[Stroop's Test #{0}] {1} Strike! {2}", moduleId, validLogging(questionCond, currentCond == 0 ? colorList.Last() : wordList.Last(), 0, validation), stage != 0 ? "Resetting to stage 1" : null);
            }

            pressed[0] = false;

            for (int i = 0; i < 6; i++)
            {
                alreadyAnswered[i] = false;
            }

            firstWord = null;
            firstColor = null;
            stage = 0;
        }
    }

    void noPress()
    {
        var validation = validCond(questionCond, currentCond == 0 ? colorList.Last() : wordList.Last());

        var ix = currentCond == 0 ? colorList.Last() : wordList.Last();


        if (!validation && !pressed[1] && !alreadyAnswered[ix])
        {
            if (stage != 0)
            {
                pressed[1] = true;
                alreadyAnswered[ix] = true;
            }
            if (stage == 0)
            {
                firstColor = colorList.Last();
                firstWord = wordList.Last();
                alreadyAnswered[ix] = true;
                Debug.LogFormat("[Stroop's Test #{0}] First submitted word and color is {1} in {2}.", moduleId, colorNames[firstWord.Value], colorNames[firstColor.Value]);
            }
            stage++;
            if (stage != 3)
            {
                StopAllCoroutines();
                StartCoroutine(submittedDisplay());
            }
            Debug.LogFormat("[Stroop's Test #{0}] {1} {2}", moduleId, validLogging(questionCond, currentCond == 0 ? colorList.Last() : wordList.Last(), 1, validation), stage != 3 ? string.Format("Advancing to stage {0}.", (stage + 1).ToString()) : "Solved!");
        }
        else
        {
            Module.HandleStrike();

            if (pressed[1])
            {
                Debug.LogFormat("[Stroop's Test #{0}] You pressed NO when you already have a valid NO pressed previous stage. {1}", moduleId, stage != 0 ? "Resetting to stage 1." : null);
            }
            else if (alreadyAnswered[ix])
            {
                Debug.LogFormat("[Stroop's Test #{0}] You pressed NO when the previous question submitted is already answered! Strike! {1}", moduleId, stage != 0 ? "Resetting to stage 1." : null);
            }
            else
            {
                Debug.LogFormat("[Stroop's Test #{0}] {1} Strike! {2}", moduleId, validLogging(questionCond, currentCond == 0 ? colorList.Last() : wordList.Last(), 1, validation), stage != 0 ? "Resetting to stage 1." : null); ;
            }

            pressed[1] = false;

            for (int i = 0; i < 6; i++)
            {
                alreadyAnswered[i] = false;
            }

            firstWord = null;
            firstColor = null;
            stage = 0;
        }
    }

    void determineCondition()
    {
        currentCond = Bomb.GetSerialNumberNumbers().Last() % 2 == 0 ? 0 : 1;
        questionCond = Bomb.GetSerialNumberNumbers().First() % 2 == 0 ? 0 : 1;

        Debug.LogFormat("[Stroop's Test #{0}] You are looking up the current slide's {1} since the last digit of the serial number is {2}.", moduleId, currentCond == 0 ? "color" : "word", Bomb.GetSerialNumberNumbers().Last() % 2 == 0 ? "even" : "odd");
        Debug.LogFormat("[Stroop's Test #{0}] The first digit of the serial number is {1}. Use the {2} questions for validation.", moduleId, Bomb.GetSerialNumberNumbers().First() % 2 == 0 ? "even" : "odd", questionCond == 0 ? "top" : "bottom");
    }

    IEnumerator solveAnimation()
    {
        yield return null;

        Audio.PlaySoundAtTransform("Solve", transform);

        var color = 5;
        var word = 0;

        while (color != 0 && word != 5)
        {
            screenText.text = colorNames[word];
            screenText.color = colors[color];
            word++;
            color--;
            yield return new WaitForSeconds(0.147f);
            screenText.text = "";
            yield return new WaitForSeconds(0.05f);
        }

        Bomb.GetComponent<KMBombModule>().HandlePass();
        moduleSolved = true;

        string[] solveText = { "YOU", "DID", "IT", "YEAH", "MADE", "BY", "KILO", "BITES", "AND", "BY", "ITZ", "SHAUN", "THANKS", "FOR", "PLAYING" };

        for (int i = 0; i < 15; i++)
        {
            screenText.text = solveText[i].ToUpperInvariant();
            screenText.color = colors[rnd.Range(0, colors.Length)];
            var duration = 0.15f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                screenText.gameObject.transform.localScale = new Vector3(
                    Easing.InOutQuad(elapsed, 0.006f, 0.005f, duration),
                    Easing.InOutQuad(elapsed, 0.006f, 0.005f, duration),
                    0.02f);
                yield return null;
                elapsed += Time.deltaTime;
            }
            yield return new WaitForSeconds(0.322f);
        }

        screenText.text = "";
    }

    IEnumerator pressAnimation(int btn)
    {
        var timer = 0f;
        var curPos = buttonObjs[btn].transform.localPosition;
        var duration = 0.1f;

        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            buttonObjs[btn].transform.localPosition = new Vector3(curPos.x, Easing.InOutQuad(timer, curPos.y, 0.01f, duration), curPos.z);
        }
        timer = 0f;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            buttonObjs[btn].transform.localPosition = new Vector3(curPos.x, Easing.InOutQuad(timer, curPos.y, 0.0146f, duration), curPos.z);
        }
    }

    // TP and autosolver implemented by Quinn Wuest
#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "Command format: !{0} xx-x or !{0} xx-xx-x. (Word-Color-Button or PrevWord-PrevColor-Word-Color-Button)" +
        "For example, \"rg-y\" presses YES when the current slide has word RED is shown in GREEN color.\n" +
        "For example, \"bw-my-n\" presses NO when the previous slide has word BLUE shown in WHITE color, and current slide has word MANGENTA shown in YELLOW color.\n" +
        "Replace any color with \"x\" to represent any color. For example, \"mx-y\" presses YES when the current slide has word MAGENTA with ANY color.\n" +
        "Multiple options can be chained with spaces. For example: \"gr-y mb-n\" would press one of GREEN-RED-YES or MAGENTA-BLUE-NO.\n" +
        "Which ever one the module generates first is the one pressed. Only one press will be registered. The press will be sent to chat.";
#pragma warning restore 0414

    public struct TpPress
    {
        public int Word;
        public int Color;
        public int? PrevWord;
        public int? PrevColor;
        public int Button;
        public TpPress(int w, int c, int? pw, int? pc, int b)
        {
            Word = w;
            Color = c;
            PrevWord = pw;
            PrevColor = pc;
            Button = b;
        }
    }

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToLowerInvariant();
        if (Regex.IsMatch(command, @"^\s*reset\s*"))
        {
            if (stage == 0)
            {
                yield return "sendtochaterror You cannot reset on Stage 1!";
                yield break;
            }
            yield return null;
            buttons[1].OnHighlight();
            while (isActivated)
            {
                yield return null;
                yield return "trycancel";
            }
            buttons[1].OnHighlightEnded();
            yield break;
        }
        var parameters = command.Split(' ');
        var match = Regex.Match(command, @"^\s*(?:(?<w1>[RYGBMWX])(?<c1>[RYGBMWX])(?:-(?<w2>[RYGBMWX])(?<c2>[RYGBMWX]))?-(?<b>[YN])\s*)+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
            yield break;
        var list = new List<TpPress>();

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].Length == 4)
            {
                // No previous
                var word = "rygbmwx".IndexOf(parameters[i][0]);
                var color = "rygbmwx".IndexOf(parameters[i][1]);
                var button = "yn".IndexOf(parameters[i][3]);
                if (word == -1 || color == -1 || button == -1)
                    yield break;
                list.Add(new TpPress(word, color, null, null, button));
            }
            else if (parameters[i].Length == 7)
            {
                var prevWord = "rygbmwx".IndexOf(parameters[i][0]);
                var prevColor = "rygbmwx".IndexOf(parameters[i][1]);
                var word = "rygbmwx".IndexOf(parameters[i][3]);
                var color = "rygbmwx".IndexOf(parameters[i][4]);
                var button = "yn".IndexOf(parameters[i][6]);
                if (word == -1 || color == -1 || button == -1 || prevWord == -1 || prevColor == -1)
                    yield break;
                list.Add(new TpPress(word, color, prevWord, prevColor, button));
            }
        }

        yield return null;
        int btn;
        int w;
        int c;
        int? pw;
        int? pc;
        while (true)
        {
            for (int i = 0; i < list.Count; i++)
                if (
                    (list[i].Word == wordList.Last() || list[i].Word == 6) &&
                    (list[i].Color == colorList.Last() || list[i].Color == 6) &&
                    (list[i].PrevWord == null || list[i].PrevWord == 6 || (wordList.Count > 1 && list[i].PrevWord == wordList[wordList.Count - 2])) &&
                    (list[i].PrevColor == null || list[i].PrevColor == 6 || (colorList.Count > 1 && list[i].PrevColor == colorList[colorList.Count - 2]))
                    )
                {
                    w = wordList.Last();
                    c = colorList.Last();
                    if (wordList.Count < 2) pw = null;
                    else pw = wordList[wordList.Count - 2];
                    if (colorList.Count < 2) pc = null;
                    else pc = colorList[colorList.Count - 2];
                    btn = list[i].Button;
                    goto foundPress;
                }
            yield return "trycancel";
        }
        foundPress:
        var ws = new[] { "RED", "YELLOW", "GREEN", "BLUE", "MAGENTA", "WHITE" };
        yield return string.Format("sendtochat Module {0} (Stroop’s Test) Pressed {1} on Word {2} Color {3}. {4}",
            GetModuleCode(),
            new[] { "YES", "NO" }[btn],
            ws[w],
            ws[c],
            pw == null ? "" : "Previous slide: Word " + ws[pw.Value] + " Color " + ws[pc.Value] + "."
            );
        buttons[btn].OnInteract();
    }

    private string GetModuleCode()
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;
        foreach (Transform children in transform.parent)
        {
            var distance = (transform.position - children.position).magnitude;
            if (children.gameObject.name == "TwitchModule(Clone)" && (closest == null || distance < closestDistance))
            {
                closest = children;
                closestDistance = distance;
            }
        }
        return closest != null ? closest.Find("MultiDeckerUI").Find("IDText").GetComponent<UnityEngine.UI.Text>().text : null;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (stage != 3)
        {
            while (!isActivated || pause)
                yield return true;
            var validation = validCond(questionCond, currentCond == 0 ? colorList.Last() : wordList.Last());
            var ix = currentCond == 0 ? colorList.Last() : wordList.Last();
            int press = 420;
            if (validation && !pressed[0] && !alreadyAnswered[ix])
                press = 0;
            if (!validation && !pressed[1] && !alreadyAnswered[ix])
                press = 1;
            if (press == 420)
            {
                yield return true;
                continue;
            }
            buttons[press].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
