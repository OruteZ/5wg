using System;
using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 특정 BPM용 발사 샘플 한 장. 같은 악기라도 BPM마다 다른 파일로 녹음돼 있어서
    /// (예: 80/90/100/110bpm) 재생 시점의 곡 BPM에 가장 가까운 걸 골라 쓴다.
    /// </summary>
    [Serializable]
    public struct WeaponSoundClip
    {
        [LabelText("BPM")] public int Bpm;
        [LabelText("샘플")] public AudioClip Clip;
    }
}
