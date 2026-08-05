using System;
using Alchemy.Inspector;
using UnityEngine;

/// <summary>
/// 적이 떨어뜨리는 경험치 픽업. 플레이어가 자석 반경에 들어오면 끌려가고, 닿으면 수집된다.
///
/// 콜라이더를 쓰지 않고 거리 계산으로 판정한다. 오브는 수십 개가 동시에 떠 있을 수 있는데
/// 그만큼의 트리거를 물리 엔진에 얹을 이유가 없다.
/// </summary>
public sealed class ExperienceOrb : MonoBehaviour
{
    [Title("수집")]
    [SerializeField, LabelText("자석 반경")] private float _magnetRadius = 2.5f;
    [SerializeField, LabelText("수집 반경")] private float _collectRadius = 0.35f;

    [Title("이동")]
    [SerializeField, LabelText("끌리기 시작 속도 (units/sec)")] private float _initialSpeed = 2f;
    [SerializeField, LabelText("가속도 (units/sec²)")] private float _acceleration = 18f;
    [SerializeField, LabelText("최대 속도 (units/sec)")] private float _maxSpeed = 16f;

    [Title("스폰")]
    [SerializeField, LabelText("드랍 위치 산포")] private float _scatter = 0.35f;

    [ShowInInspector, ReadOnly, LabelText("경험치 값")]
    private float ValueDebug => _value;

    private Action<ExperienceOrb> _release;
    private Transform _target;
    private IExperienceReceiver _receiver;
    private float _value;
    private float _speed;
    private bool _isMagnetized;
    private bool _isCollected;

    /// <summary>풀 생성 시 1회만 호출한다.</summary>
    public void SetReleaseCallback(Action<ExperienceOrb> release) => _release = release;

    /// <summary>풀에서 꺼낼 때마다 호출한다. 재사용되므로 상태를 전부 되돌린다.</summary>
    public void Spawn(Vector2 position, float value, Transform target, IExperienceReceiver receiver)
    {
        // 같은 자리에서 여러 마리가 죽어도 오브가 한 점에 겹치지 않게 흩뿌린다.
        transform.position = position + UnityEngine.Random.insideUnitCircle * _scatter;

        _value = value;
        _target = target;
        _receiver = receiver;
        _speed = _initialSpeed;
        _isMagnetized = false;
        _isCollected = false;
    }

    private void Update()
    {
        if (_isCollected || _target == null) return;

        Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
        float distance = toTarget.magnitude;

        // 한 번 끌리기 시작하면 반경을 벗어나도 계속 따라간다. 경계에서 붙었다 떨어졌다 하지 않게.
        if (!_isMagnetized)
        {
            if (distance > _magnetRadius) return;
            _isMagnetized = true;
        }

        if (distance <= _collectRadius)
        {
            Collect();
            return;
        }

        _speed = Mathf.Min(_speed + _acceleration * Time.deltaTime, _maxSpeed);
        transform.position += (Vector3)(toTarget / distance * _speed * Time.deltaTime);
    }

    private void Collect()
    {
        if (_isCollected) return;
        _isCollected = true;

        _receiver?.AddExperience(_value);
        _release?.Invoke(this);
    }
}
