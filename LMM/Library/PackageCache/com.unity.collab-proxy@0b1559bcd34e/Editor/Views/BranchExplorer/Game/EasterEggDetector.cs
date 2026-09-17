using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Game
{
    internal class EasterEggDetector
    {
        internal bool ProcessKeyCode(KeyCode keyCode)
        {
            if (keyCode == KeyCode.None)
                return false;

            bool konamiMatch = ProcessSequence(
                keyCode, KONAMI_CODE, ref mKonamiIndex);

            bool playMatch = ProcessSequence(
                keyCode, PLAY_CODE, ref mPlayIndex);

            bool gameMatch = ProcessSequence(
                keyCode, GAME_CODE, ref mGameIndex);

            return konamiMatch || playMatch || gameMatch;
        }

        internal void Reset()
        {
            mKonamiIndex = 0;
            mPlayIndex = 0;
            mGameIndex = 0;
        }

        static bool ProcessSequence(
            KeyCode keyCode, KeyCode[] sequence, ref int index)
        {
            if (keyCode == sequence[index])
            {
                index++;
                if (index >= sequence.Length)
                {
                    index = 0;
                    return true;
                }
            }
            else
            {
                index = keyCode == sequence[0] ? 1 : 0;
            }

            return false;
        }

        static readonly KeyCode[] KONAMI_CODE =
        {
            KeyCode.UpArrow, KeyCode.UpArrow,
            KeyCode.DownArrow, KeyCode.DownArrow,
            KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.B, KeyCode.A
        };

        static readonly KeyCode[] PLAY_CODE =
        {
            KeyCode.P, KeyCode.L, KeyCode.A, KeyCode.Y
        };

        static readonly KeyCode[] GAME_CODE =
        {
            KeyCode.G, KeyCode.A, KeyCode.M, KeyCode.E
        };

        int mKonamiIndex;
        int mPlayIndex;
        int mGameIndex;
    }
}
