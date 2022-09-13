using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColoredSquares;
using UnityEngine;

using Rnd = UnityEngine.Random;

public abstract class ColoredSquaresModuleBase : MonoBehaviour
{
    public ColoredSquaresScaffold ScaffoldPrefab;
    public KMColorblindMode ColorblindMode;

    public abstract string Name { get; }

    protected KMRuleSeedable RuleSeedable { get { return _scaffold.RuleSeedable; } }
    protected KMSelectable[] Buttons { get { return _scaffold.Buttons; } }
    protected KMAudio Audio { get { return _scaffold.Audio; } }

    private ColoredSquaresScaffold _scaffold;
    private KMBombModule _module;
    private MeshRenderer[] _buttonRenderers;
    protected SquareColor[] _colors = new SquareColor[16];
    private static readonly Dictionary<string, int> _moduleIdCounters = new Dictionary<string, int>();
    private int _moduleId;
    protected bool _isSolved = false;
    private bool _colorblind;

    protected Coroutine ActiveCoroutine;
    protected bool IsCoroutineActive { get { return ActiveCoroutine != null; } }

    private static T[] newArray<T>(params T[] array) { return array; }
    private static readonly Color[] _lightColors = newArray<Color>(
        Color.black, Color.white, Color.red, new Color32(0x83, 0x83, 0xff, 0xff), Color.green, Color.yellow, Color.magenta,
        new Color32(0x13, 0x13, 0xd4, 0xff), new Color32(0xfe, 0x97, 0x00, 0xff), new Color32(0x00, 0xfe, 0xff, 0xff),
        new Color32(0x85, 0x16, 0xca, 0xff), new Color32(0x93, 0x04, 0x00, 0xff), new Color32(0xb1, 0x61, 0x10, 0xff),
        new Color32(0xe0, 0xa9, 0xfe, 0xff), new Color32(0x28, 0x75, 0xfe, 0xff), new Color32(0x87, 0xed, 0x8d, 0xff),
        new Color32(0x00, 0x2b, 0x14, 0xff), new Color32(0xb4, 0xb4, 0xb4, 0xff));

    private void Awake()
    {
        if (!_moduleIdCounters.ContainsKey(Name))
            _moduleIdCounters[Name] = 1;

        _moduleId = _moduleIdCounters[Name]++;
        _module = GetComponent<KMBombModule>();

        _scaffold = Instantiate(ScaffoldPrefab, transform);
        _buttonRenderers = _scaffold.Buttons.Select(b => b.GetComponent<MeshRenderer>()).ToArray();
        _colorblind = ColorblindMode.ColorblindModeActive;

        var moduleSelectable = GetComponent<KMSelectable>();
        foreach (var btn in _scaffold.Buttons)
            btn.Parent = moduleSelectable;
        moduleSelectable.Children = _scaffold.Buttons;
        moduleSelectable.UpdateChildren();
        for (int i = 0; i < 16; i++)
            _scaffold.Buttons[i].OnInteract = MakeButtonHandler(i);
        SetAllButtonsBlack();
    }
    
    private void Start()
    {
        for (int i = 0; i < 16; i++)
            _scaffold.Lights[i].range = .1f * _module.transform.lossyScale.x;
        DoStart();
    }

    protected abstract void DoStart();

    private KMSelectable.OnInteractHandler MakeButtonHandler(int index)
    {
        return delegate
        {
            _scaffold.Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _scaffold.Buttons[index].transform);
            _scaffold.Buttons[index].AddInteractionPunch();
            if (!_isSolved)
                ButtonPressed(index);
            return false;
        };
    }

    protected void Log(string format, params object[] args)
    {
        Debug.LogFormat(@"[{0} #{1}] {2}", Name, _moduleId, string.Format(format, args));
    }

    protected void LogDebug(string format, params object[] args)
    {
        Debug.LogFormat(@"<{0} #{1}> {2}", Name, _moduleId, string.Format(format, args));
    }

    protected abstract void ButtonPressed(int index);

    protected void ModulePassed()
    {
        if (ActiveCoroutine != null)
        {
            StopCoroutine(ActiveCoroutine);
            ActiveCoroutine = null;
        }
        SetAllButtonsBlack();
        Log("Module solved.");
        _module.HandlePass();
        _isSolved = true;
    }

    protected void Strike()
    {
        _module.HandleStrike();
        SetAllButtonsBlack();
    }

    protected void PlaySound(int index)
    {
        switch (_colors[index])
        {
            case SquareColor.Red:
            case SquareColor.Black:
            case SquareColor.White:
            case SquareColor.Forest:
                _scaffold.Audio.PlaySoundAtTransform("redlight", _scaffold.Buttons[index].transform);
                break;
            case SquareColor.Blue:
            case SquareColor.DarkBlue:
            case SquareColor.Orange:
            case SquareColor.Brown:
            case SquareColor.Gray:
                _scaffold.Audio.PlaySoundAtTransform("bluelight", _scaffold.Buttons[index].transform);
                break;
            case SquareColor.Green:
            case SquareColor.Cyan:
            case SquareColor.Mauve:
                _scaffold.Audio.PlaySoundAtTransform("greenlight", _scaffold.Buttons[index].transform);
                break;
            case SquareColor.Yellow:
            case SquareColor.Purple:
            case SquareColor.Azure:
                _scaffold.Audio.PlaySoundAtTransform("yellowlight", _scaffold.Buttons[index].transform);
                break;
            case SquareColor.Magenta:
            case SquareColor.Chestnut:
            case SquareColor.Jade:
                _scaffold.Audio.PlaySoundAtTransform("magentalight", _scaffold.Buttons[index].transform);
                break;
        }
    }

    public void SetButtonBlack(int index)
    {
        _colors[index] = (int)SquareColor.Black;
        _buttonRenderers[index].sharedMaterial = _scaffold.Materials[(int) SquareColor.Black];
        _scaffold.Lights[index].gameObject.SetActive(false);
    }

    public void SetAllButtonsBlack()
    {
        for (int i = 0; i < 16; i++)
            SetButtonBlack(i);
    }

    public void SetButtonColor(int ix, SquareColor color)
    {
        if (color == SquareColor.Black)
            SetButtonBlack(ix);
        else
        {
            _colors[ix] = color;
            _buttonRenderers[ix].sharedMaterial = _colorblind ? _scaffold.MaterialsCB[(int) color] ?? _scaffold.Materials[(int) color] : _scaffold.Materials[(int) color];
            _scaffold.Lights[ix].color = _lightColors[(int) color];
            _scaffold.Lights[ix].gameObject.SetActive(true);
        }
    }

    public void StartSquareColorsCoroutine(SquareColor[] colors, SquaresToRecolor behaviour = SquaresToRecolor.All, bool delay = false, bool unshuffled = false)
    {
        var indexes = new List<int?>((behaviour == SquaresToRecolor.NonwhiteOnly
            ? Enumerable.Range(0, 16).Where(ix => colors[ix] != SquareColor.White)
            : behaviour == SquaresToRecolor.NonblackOnly
            ? Enumerable.Range(0, 16).Where(ix => colors[ix] != SquareColor.Black)
            : Enumerable.Range(0, 16)).Select(i => (int?) i));
        if (!unshuffled)
            indexes.Shuffle();
        if (delay)
            indexes.Insert(0, null);
        StartSquareColorsCoroutine(colors, indexes.ToArray());
    }

    /// <summary>
    /// Starts a coroutine that re-colors some of the squares.
    /// </summary>
    /// <param name="colors">The colors to set the relevant squares to.</param>
    /// <param name="indexes">Specifies which squares to recolor and in which order. Insert a <c>null</c> value to add a delay.</param>
    public void StartSquareColorsCoroutine(SquareColor[] colors, int?[] indexes)
    {
        if (ActiveCoroutine != null)
            StopCoroutine(ActiveCoroutine);
        ActiveCoroutine = StartCoroutine(SetSquareColorsCoroutine(colors, indexes));
    }

    private IEnumerator SetSquareColorsCoroutine(SquareColor[] colors, int?[] indexes)
    {
        foreach (var i in indexes)
        {
            if (i == null)
                yield return new WaitForSeconds(Rnd.Range(1.5f, 2f));
            else
            {
                SetButtonColor(i.Value, colors[i.Value]);
                yield return new WaitForSeconds(.03f);
            }
        }
        ActiveCoroutine = null;
    }

#pragma warning disable 414
    protected readonly string TwitchHelpMessage = @"!{0} A1 A2 A3 B3 [specify column as letter, then row as number] | !{0} colorblind";
#pragma warning restore 414

    protected IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.Trim().Equals("colorblind", StringComparison.InvariantCultureIgnoreCase))
        {
            _colorblind = !_colorblind;
            StartSquareColorsCoroutine(_colors);
            yield return null;
            yield break;
        }

        var buttons = new List<KMSelectable>();
        foreach (var piece in command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (piece.Length != 2 || piece[0] < 'a' || piece[0] > 'd' || piece[1] < '1' || piece[1] > '4')
                yield break;
            buttons.Add(_scaffold.Buttons[(piece[0] - 'a') + 4 * (piece[1] - '1')]);
        }

        yield return null;
        yield return buttons;
    }
}