using ColoredSquares;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RNG = UnityEngine.Random;

public sealed class TombstoneMazeModule : ColoredSquaresModuleBase
{
    public override string Name { get { return "Tombstone Maze"; } }

    private bool[] _canGoDown, _canGoRight;
    private bool[] _canGoDownHidden, _canGoRightHidden;
    private int _pawnPosition, _opponentPosition, _pawnPositionHidden, _opponentPositionHidden;

    private int _opponentRememberedPosition;
    private int _opponentLastMoveDir;

    protected override void DoStart()
    {
        SetInitialState();
    }

    private void SetInitialState()
    {
        bool[][] maze1 = GenerateMaze();
        bool[][] maze2 = GenerateMaze();
        _canGoDown = maze1[1];
        _canGoRight = maze1[0];
        _canGoDownHidden = maze2[1];
        _canGoRightHidden = maze2[0];

        _pawnPosition = 12;
        _pawnPositionHidden = 12;
        _opponentPosition = 3;
        _opponentPositionHidden = 3;
        _opponentRememberedPosition = 3;
        _opponentLastMoveDir = 1;

        string box = " │\n─┼";
        Log("Visible Maze:");
        Log(
            "\n┼─┼─┼─┼─┼\n" +
            Enumerable.Range(0, 4).Select(row =>
                "│" + Enumerable.Range(0, 4).Select(col => box.Substring(0, 2).Replace('│', _canGoRight[row * 4 + col] ? ' ' : '│')).Join("") +
                "\n┼" + Enumerable.Range(0, 4).Select(col => box.Substring(3, 2).Replace('─', _canGoDown[row * 4 + col] ? ' ' : '─')).Join("")
            ).Join("\n")
        );
        Log("Hidden Maze:");
        Log(
            "\n┼─┼─┼─┼─┼\n" +
            Enumerable.Range(0, 4).Select(row =>
                "│" + Enumerable.Range(0, 4).Select(col => box.Substring(0, 2).Replace('│', _canGoRightHidden[row * 4 + col] ? ' ' : '│')).Join("") +
                "\n┼" + Enumerable.Range(0, 4).Select(col => box.Substring(3, 2).Replace('─', _canGoDownHidden[row * 4 + col] ? ' ' : '─')).Join("")
            ).Join("\n")
        );

        StartSquareColorsCoroutine(Enumerable.Range(0, 16).Select(i => MazeColor(i)).ToArray(), delay: true);
    }

    private SquareColor MazeColor(int i)
    {
        int x = MazeDirections(i);
        if(x >= 3)
            ++x; // Skip Blue
        return (SquareColor)x;
    }

    /// <summary>
    /// Ordered DURL
    /// </summary>
    private int MazeDirections(int i, bool hidden = false)
    {
        return (i < 12 && (hidden ? _canGoDownHidden : _canGoDown)[i] ? 1 : 0) |
            (i >= 4 && (hidden ? _canGoDownHidden : _canGoDown)[i - 4] ? 2 : 0) |
            (i % 4 != 3 && (hidden ? _canGoRightHidden : _canGoRight)[i] ? 4 : 0) |
            (i % 4 != 0 && (hidden ? _canGoRightHidden : _canGoRight)[i - 1] ? 8 : 0);
    }

    private bool[][] GenerateMaze()
    {
        List<int> active = new List<int>();
        List<int> todo = Enumerable.Range(0, 16).ToList();
        int start = RNG.Range(0, todo.Count);
        active.Add(todo[start]);
        todo.RemoveAt(start);

        List<int> vwalls = Enumerable.Range(0, 16).ToList();
        List<int> hwalls = Enumerable.Range(0, 16).ToList();

        while(todo.Count > 0)
        {
            int activeIx = RNG.Range(0, active.Count);
            int sq = active[activeIx];

            List<int> adjs = new List<int>();
            if((sq % 4) > 0 && todo.Contains(sq - 1))
                adjs.Add(sq - 1);
            if((sq % 4) < 3 && todo.Contains(sq + 1))
                adjs.Add(sq + 1);
            if((sq / 4) > 0 && todo.Contains(sq - 4))
                adjs.Add(sq - 4);
            if((sq / 4) < 3 && todo.Contains(sq + 4))
                adjs.Add(sq + 4);

            if(adjs.Count == 0)
            {
                active.RemoveAt(activeIx);
                continue;
            }

            int adj = adjs[RNG.Range(0, adjs.Count)];
            todo.Remove(adj);
            active.Add(adj);

            if(adj == sq - 1)
                vwalls.Remove(adj);
            else if(adj == sq + 1)
                vwalls.Remove(sq);
            else if(adj == sq - 4)
                hwalls.Remove(adj);
            else if(adj == sq + 4)
                hwalls.Remove(sq);
        }

        return new bool[][] { Enumerable.Range(0, 16).Select(i => !vwalls.Contains(i)).ToArray(), Enumerable.Range(0, 16).Select(i => !hwalls.Contains(i)).ToArray() };
    }

    protected override void ButtonPressed(int index)
    {
        if(_isSolved)
            return;

        Log("Pressed button {0}.", index);
        PlaySound(index);

        int scale = (index % 4) + 1;
        int dir = index / 4;

        if(scale == 4)
        {
            Dig(dir); // Contains EnemyDig()
            return;
        }

        for(int i = 0; i < scale; ++i)
            Move(dir);
        EnemyMove();
    }

    private void Move(int dir)
    {
        if(dir == 0 && (MazeDirections(_pawnPosition) & 2) != 0 && _opponentPosition != _pawnPosition - 4)
            _pawnPosition -= 4;
        if(dir == 0 && (MazeDirections(_pawnPositionHidden, true) & 2) != 0 && _opponentPositionHidden != _pawnPositionHidden - 4)
            _pawnPositionHidden -= 4;

        if(dir == 1 && (MazeDirections(_pawnPosition) & 4) != 0 && _opponentPosition != _pawnPosition + 1)
            _pawnPosition += 1;
        if(dir == 1 && (MazeDirections(_pawnPositionHidden, true) & 4) != 0 && _opponentPositionHidden != _pawnPositionHidden + 1)
            _pawnPositionHidden += 1;

        if(dir == 2 && (MazeDirections(_pawnPosition) & 1) != 0 && _opponentPosition != _pawnPosition + 4)
            _pawnPosition += 4;
        if(dir == 2 && (MazeDirections(_pawnPositionHidden, true) & 1) != 0 && _opponentPositionHidden != _pawnPositionHidden + 4)
            _pawnPositionHidden += 4;

        if(dir == 3 && (MazeDirections(_pawnPosition) & 8) != 0 && _opponentPosition != _pawnPosition - 1)
            _pawnPosition -= 1;
        if(dir == 3 && (MazeDirections(_pawnPositionHidden, true) & 8) != 0 && _opponentPositionHidden != _pawnPositionHidden - 1)
            _pawnPositionHidden -= 1;

        Log("This means you were at {0} (visible) or {1} (hidden).", _pawnPosition, _pawnPositionHidden);
    }

    private void EnemyMove()
    {
        SquareColor[] squares = Enumerable.Repeat(SquareColor.Black, 16).ToArray();
        List<int> dirs = new int[] { 0, 1, 2, 3 }.ToList();
        if(_opponentRememberedPosition % 4 == 0)
            dirs.Remove(3);
        if(_opponentRememberedPosition % 4 == 3)
            dirs.Remove(1);
        if(_opponentRememberedPosition / 4 == 0)
            dirs.Remove(0);
        if(_opponentRememberedPosition / 4 == 3)
            dirs.Remove(2);

        int dir = RNG.Range(0, 3) != 0 ? SolveMaze() : dirs.PickRandom();
        int scale = RNG.Range(1, 4);

        for(int i = 0; i < scale; ++i)
        {
            if(dir == 0 && (MazeDirections(_opponentPosition) & 2) != 0 && _opponentPosition != _pawnPosition + 4)
                _opponentPosition -= 4;
            if(dir == 0 && (MazeDirections(_opponentPositionHidden, true) & 2) != 0 && _opponentPositionHidden != _pawnPositionHidden + 4)
                _opponentPositionHidden -= 4;

            if(dir == 1 && (MazeDirections(_opponentPosition) & 4) != 0 && _opponentPosition != _pawnPosition - 1)
                _opponentPosition += 1;
            if(dir == 1 && (MazeDirections(_opponentPositionHidden, true) & 4) != 0 && _opponentPositionHidden != _pawnPositionHidden - 1)
                _opponentPositionHidden += 1;

            if(dir == 2 && (MazeDirections(_opponentPosition) & 1) != 0 && _opponentPosition != _pawnPosition - 4)
                _opponentPosition += 4;
            if(dir == 2 && (MazeDirections(_opponentPositionHidden, true) & 1) != 0 && _opponentPositionHidden != _pawnPositionHidden - 4)
                _opponentPositionHidden += 4;

            if(dir == 3 && (MazeDirections(_opponentPosition) & 8) != 0 && _opponentPosition != _pawnPosition + 1)
                _opponentPosition -= 1;
            if(dir == 3 && (MazeDirections(_opponentPositionHidden, true) & 8) != 0 && _opponentPositionHidden != _pawnPositionHidden + 1)
                _opponentPositionHidden -= 1;
        }

        StartSquareColorsCoroutine(squares);
        StartCoroutine(Flash((dir * 4) + (scale - 1), squares[(dir * 4) + (scale - 1)], SquareColor.White));

        Log("This means the pawn was at {0} (visible) or {1} (hidden).", _opponentPosition, _opponentPositionHidden);
    }

    private int SolveMaze()
    {
        List<int> todo = Enumerable.Range(0, 16).ToList();
        int start = RNG.Range(0, 2) != 0 ? _opponentRememberedPosition : _opponentPosition;
        todo.Remove(start);
        Dictionary<int, int> dirToAdj = new Dictionary<int, int>();
        if((MazeDirections(start) & 8) != 0)
            dirToAdj.Add(3, start - 1);
        if((MazeDirections(start) & 4) != 0)
            dirToAdj.Add(1, start + 1);
        if((MazeDirections(start) & 2) != 0)
            dirToAdj.Add(0, start - 4);
        if((MazeDirections(start) & 1) != 0)
            dirToAdj.Add(2, start + 4);
        tryagain:
        KeyValuePair<int, int> kvp = dirToAdj.PickRandom();
        dirToAdj.Remove(kvp.Key);
        if(dirToAdj.Count == 0)
            goto finished;

        List<int> active = new List<int>() { kvp.Value };
        List<int> goal = new List<int>();
        if((MazeDirections(8) & 1) != 0)
            goal.Add(8);
        if((MazeDirections(13) & 8) != 0)
            goal.Add(13);

        while(active.Count != 0)
        {
            int picked = active.PickRandom();
            active.Remove(picked);

            if((MazeDirections(picked) & 8) != 0)
                active.Add(picked - 1);
            if((MazeDirections(picked) & 4) != 0)
                active.Add(picked + 1);
            if((MazeDirections(picked) & 2) != 0)
                active.Add(picked - 4);
            if((MazeDirections(picked) & 1) != 0)
                active.Add(picked + 4);

            if(goal.Any(g => active.Contains(g)))
                goto finished;
        }
        goto tryagain;

        finished:
        return kvp.Key;
    }

    private void Dig(int dir)
    {
        bool wall = false;
        if(dir == 0)
            wall = (MazeDirections(_pawnPositionHidden, true) & 2) == 0 || _opponentPositionHidden == _pawnPositionHidden - 4;
        if(dir == 1)
            wall = (MazeDirections(_pawnPositionHidden, true) & 4) == 0 || _opponentPositionHidden == _pawnPositionHidden + 1;
        if(dir == 2)
            wall = (MazeDirections(_pawnPositionHidden, true) & 1) == 0 || _opponentPositionHidden == _pawnPositionHidden + 4;
        if(dir == 3)
            wall = (MazeDirections(_pawnPositionHidden, true) & 8) == 0 || _opponentPositionHidden == _pawnPositionHidden - 1;

        int dugix = dir == 0 ? _pawnPositionHidden - 4 : dir == 1 ? _pawnPositionHidden + 1 : dir == 2 ? _pawnPositionHidden + 4 : _pawnPositionHidden - 1;
        if(wall)
        {
            SquareColor[] squares = Enumerable.Repeat(SquareColor.Red, 16).ToArray();
            StartSquareColorsCoroutine(squares);
            EnemyDig(squares);

            Log("You could not go to position {0}.", dugix);
        }
        else
        {
            SquareColor[] squares = Enumerable.Repeat(SquareColor.Green, 16).ToArray();
            squares[dugix] = SquareColor.White;
            StartSquareColorsCoroutine(squares);
            if(dugix == 3)
            {
                StopAllCoroutines();
                Log("You dug position 3.");
                ModulePassed();
                return;
            }
            Log("You could go to position {0}.", dugix);
            EnemyDig(squares);
        }
    }

    private void EnemyDig(SquareColor[] squares)
    {
        int dir = (_opponentLastMoveDir + 2) % 4;
        if(RNG.Range(0, 2) != 0)
            dir = RNG.Range(0, 4);
        if(_opponentRememberedPosition == 8)
            dir = 2;
        if(_opponentRememberedPosition == 13)
            dir = 3;

        bool wall = false;
        if(dir == 0)
            wall = (MazeDirections(_opponentPosition) & 2) == 0 || _opponentPosition == _pawnPosition - 4;
        if(dir == 1)
            wall = (MazeDirections(_opponentPosition) & 4) == 0 || _opponentPosition == _pawnPosition + 1;
        if(dir == 2)
            wall = (MazeDirections(_opponentPosition) & 1) == 0 || _opponentPosition == _pawnPosition + 4;
        if(dir == 3)
            wall = (MazeDirections(_opponentPosition) & 8) == 0 || _opponentPosition == _pawnPosition - 1;

        int dugix = dir == 0 ? _opponentPosition - 4 : dir == 1 ? _opponentPosition + 1 : dir == 2 ? _opponentPosition + 4 : _opponentPosition - 1;

        Log("The pawn dug in position {0}.", dugix);
        if(wall)
            Log("They did not gain any new information, as they hit something.");
        else
            Log("They have gained new information.");

        if(!wall)
        {
            _opponentRememberedPosition = _opponentPosition;
            if(dir == 2 && _opponentPosition == 8 || dir == 3 && _opponentPosition == 13)
            {
                StopAllCoroutines();
                Strike("They dug in position 12. You lose. Strike!");
                return;
            }
        }

        StartCoroutine(Flash(3 + 4 * dir, squares[3 + 4 * dir], SquareColor.White));

    }

    private IEnumerator Flash(int ix, SquareColor baseCol, SquareColor newCol)
    {
        yield return new WaitUntil(() => !IsCoroutineActive);
        ActiveCoroutine = StartCoroutine(FlashInternal(ix, baseCol, newCol));
    }

    private IEnumerator FlashInternal(int ix, SquareColor baseCol, SquareColor newCol)
    {
        while(true)
        {
            SetButtonColor(ix, baseCol);
            yield return new WaitForSeconds(0.5f);
            SetButtonColor(ix, newCol);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void Strike(string message = null, params object[] args)
    {
        if(message != null)
            Log(message, args);
        if(ActiveCoroutine != null)
            StopCoroutine(ActiveCoroutine);
        base.Strike();
        SetInitialState();
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while(!_isSolved)
        {
            // TODO: Make this functional
            ModulePassed();
        }
        yield break;
    }
}
