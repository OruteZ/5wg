using System;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

namespace FiveWG.Prototype
{
    /// <summary>
    /// Attack 입력을 누르고 있는 동안 마우스 방향으로 투사체를 발사한다. 투사체는 ObjectPool로 재사용한다.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement2D))]
    public sealed class PlayerShooter2D : MonoBehaviour
    {
        [Title("입력")]
        [SerializeField, Required("Player 액션맵을 가진 InputActionAsset이 필요하다.")]
        private InputActionAsset _inputActions;

        [SerializeField, LabelText("Attack 액션 경로")]
        private string _attackActionPath = "Player/Attack";

        [Title("발사")]
        [SerializeField, Required("투사체 프리팹")] private Projectile2D _projectilePrefab;
        [SerializeField, LabelText("발사구 (비우면 자기 자신)")] private Transform _muzzle;
        [SerializeField, LabelText("발사 간격 (sec)")] private float _fireInterval = 0.15f;

        [Title("풀")]
        [SerializeField, LabelText("기본 용량")] private int _poolDefaultCapacity = 32;
        [SerializeField, LabelText("최대 크기")] private int _poolMaxSize = 256;

        [ShowInInspector, ReadOnly, LabelText("활성 투사체 수")]
        private int ActiveProjectileCount => _pool?.CountActive ?? 0;

        private InputAction _attackAction;
        private ObjectPool<Projectile2D> _pool;
        private Action<Projectile2D> _releaseCallback;
        private Camera _camera;
        private Transform _projectileRoot;
        private float _fireCooldown;

        private void Awake()
        {
            _camera = Camera.main;
            if (_muzzle == null) _muzzle = transform;

            _attackAction = _inputActions == null
                ? null
                : _inputActions.FindAction(_attackActionPath, throwIfNotFound: false);

            if (_attackAction is null)
            {
                Debug.LogError($"[{nameof(PlayerShooter2D)}] '{_attackActionPath}' 액션을 찾지 못했다.", this);
            }

            // 하이라키가 투사체로 더러워지지 않도록 부모 하나를 만들어 모아둔다.
            _projectileRoot = new GameObject("ProjectilePool").transform;

            // 델리게이트를 한 번만 만들어 재사용한다(발사마다 GC 할당 방지).
            _releaseCallback = ReleaseProjectile;

            _pool = new ObjectPool<Projectile2D>(
                createFunc: CreateProjectile,
                actionOnGet: OnGetProjectile,
                actionOnRelease: OnReleaseProjectile,
                actionOnDestroy: OnDestroyProjectile,
                collectionCheck: true,
                defaultCapacity: _poolDefaultCapacity,
                maxSize: _poolMaxSize);
        }

        private void OnEnable() => _attackAction?.Enable();

        private void OnDisable() => _attackAction?.Disable();

        private void OnDestroy()
        {
            // 풀에 남아 있는 비활성 투사체까지 actionOnDestroy로 정리된다.
            _pool?.Dispose();
            if (_projectileRoot != null) Destroy(_projectileRoot.gameObject);
        }

        private void Update()
        {
            _fireCooldown -= Time.deltaTime;

            bool wantsToFire = _attackAction is not null && _attackAction.IsPressed();
            if (!wantsToFire || _fireCooldown > 0f || _projectilePrefab == null) return;

            _fireCooldown = _fireInterval;
            Fire();
        }

        private void Fire()
        {
            Vector2 origin = _muzzle.position;
            Projectile2D projectile = _pool.Get();
            projectile.Launch(origin, GetAimDirection(origin));
        }

        /// <summary>마우스가 있으면 커서 방향, 없으면(패드 등) 오른쪽을 기본 방향으로 쓴다.</summary>
        private Vector2 GetAimDirection(Vector2 origin)
        {
            if (Mouse.current is null || _camera == null) return Vector2.right;

            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z));

            Vector2 direction = (Vector2)world - origin;
            return direction.sqrMagnitude > 0.0001f ? direction : Vector2.right;
        }

        private Projectile2D CreateProjectile()
        {
            Projectile2D projectile = Instantiate(_projectilePrefab, _projectileRoot);
            projectile.SetReleaseCallback(_releaseCallback);
            return projectile;
        }

        private void ReleaseProjectile(Projectile2D projectile) => _pool.Release(projectile);

        private static void OnGetProjectile(Projectile2D projectile) => projectile.gameObject.SetActive(true);

        private static void OnReleaseProjectile(Projectile2D projectile) => projectile.gameObject.SetActive(false);

        private static void OnDestroyProjectile(Projectile2D projectile)
        {
            if (projectile != null) Destroy(projectile.gameObject);
        }
    }
}
