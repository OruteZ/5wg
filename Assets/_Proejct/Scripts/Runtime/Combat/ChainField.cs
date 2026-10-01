using System;
using System.Collections.Generic;
using FiveWG.Core;
using UnityEngine;

namespace FiveWG.Combat
{
    /// <summary>
    /// 연결된 대상 전원에게 일정 간격으로 틱 데미지를 주는 사슬 하나의 수치. 신스(체인 빔)가 쓴다.
    /// </summary>
    public struct ChainFieldData
    {
        public float TickDamage;
        public float TickInterval;
        public float Duration;
        public Faction OwnerFaction;

        /// <summary>사슬이 시작되는 지점(발사 주체). 비주얼 라인의 첫 점으로만 쓴다.</summary>
        public Transform Origin;
    }

    /// <summary>
    /// 사거리 쿼리가 아니라 무기가 골라준 대상 목록을 그대로 물고 있다가 틱마다 때리는 영역.
    /// <see cref="DamageField"/>와 갈라 만든 이유는 판정 방식 자체가 다르기 때문이다 —
    /// 저건 "지금 반경 안에 누가 있나"를 매 틱 다시 묻고, 이건 "발동 순간 골라둔 대상들"을 고정해서 문다.
    /// 링크된 대상이 죽어 비활성화되면(풀에 반납되면) 그 이후로만 조용히 빠진다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ChainField : MonoBehaviour, IPooledObject<ChainField>
    {
        private readonly List<Transform> _links = new();
        private Vector3[] _linePoints = Array.Empty<Vector3>();

        private LineRenderer _line;
        private Action<ChainField> _release;
        private ChainFieldData _data;
        private float _elapsed;
        private float _nextTick;
        private bool _isSpent;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();

            // 프리팹에 비주얼을 미리 안 만들어두고 코드에서 최소한으로 꾸민다 —
            // 전용 머티리얼 에셋을 새로 두지 않으려고 URP에서도 그리는 Sprites/Default를 쓴다.
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.startWidth = 0.08f;
            _line.endWidth = 0.08f;
            _line.textureMode = LineTextureMode.Tile;
            _line.numCapVertices = 4;
            _line.sortingOrder = 5;
        }

        public void SetReleaseCallback(Action<ChainField> release) => _release = release;

        /// <summary>수명이 끝나기 전에 강제로 끊는다. 같은 무기가 다시 발사해 새 사슬을 걸 때 쓴다 —
        /// 안 그러면 옛 사슬이 남은 시간만큼 옛 대상을 계속 때려서 "타겟이 안 바뀌는" 것처럼 보인다.</summary>
        public void Stop() => Despawn();

        /// <summary>
        /// 수명·틱 주기는 그대로 두고 링크 대상만 갈아끼운다. 원래 재계산은 트리거 칸에서만
        /// 일어나는데(기획서), 그 간격(반 마디~한 마디)이 실제로 움직이는 적을 쫓기엔 느려서
        /// 매 프레임(<see cref="FiveWG.Weapons.ChainWeapon.Tick"/>) 이걸로 갱신한다.
        /// </summary>
        public void Retarget(IReadOnlyList<Transform> targets)
        {
            if (_isSpent) return;

            _links.Clear();
            for (int i = 0; i < targets.Count; i++) _links.Add(targets[i]);
        }

        /// <summary>풀에서 꺼낼 때마다 호출한다. 링크 목록은 무기가 발동 순간에 한 번 골라 넘긴다.</summary>
        public void Begin(IReadOnlyList<Transform> targets, in ChainFieldData data)
        {
            _data = data;
            _links.Clear();
            for (int i = 0; i < targets.Count; i++) _links.Add(targets[i]);

            _elapsed = 0f;
            _nextTick = 0f;
            _isSpent = false;

            TickDamage();
            UpdateLine();
        }

        private void Update()
        {
            if (_isSpent) return;

            _elapsed += Time.deltaTime;

            if (_elapsed >= _nextTick && _data.TickInterval > 0f) TickDamage();
            UpdateLine();
            if (_elapsed >= _data.Duration) Despawn();
        }

        /// <summary>발사 주체에서 시작해 살아 있는 링크를 순서대로 잇는다. 죽은 링크는 그 자리에서 끊긴다.</summary>
        private void UpdateLine()
        {
            // ponytail: 매 프레임 새로 세는 대신, 최대 길이(오리진 + 전체 링크)로 한 번만 키워두고
            // 실제 채운 만큼만 그린다. 동시에 떠 있는 사슬이 한 자릿수라 재할당이 문제 되지 않는다.
            int maxLen = _links.Count + 1;
            if (_linePoints.Length < maxLen) _linePoints = new Vector3[maxLen];

            int count = 0;
            if (_data.Origin != null) _linePoints[count++] = _data.Origin.position;

            for (int i = 0; i < _links.Count; i++)
            {
                Transform link = _links[i];
                if (link == null || !link.gameObject.activeInHierarchy) break;

                _linePoints[count++] = link.position;
            }

            _line.positionCount = count;
            _line.SetPositions(_linePoints);
        }

        private void TickDamage()
        {
            _nextTick += _data.TickInterval > 0f ? _data.TickInterval : float.MaxValue;

            for (int i = 0; i < _links.Count; i++)
            {
                Transform link = _links[i];

                // 죽어서 풀에 반납된 대상은 비활성화만 되고 파괴되지는 않는다 — 활성 여부로 거른다.
                if (link == null || !link.gameObject.activeInHierarchy) continue;
                if (!link.TryGetComponent(out IDamageable damageable)) continue;
                if (damageable.Faction == _data.OwnerFaction) continue;

                damageable.TakeDamage(_data.TickDamage);
            }
        }

        private void Despawn()
        {
            if (_isSpent) return;
            _isSpent = true;

            _links.Clear();
            _release?.Invoke(this);
        }
    }
}
