using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Stage
{
    /// <summary>
    /// 자식들을 주기 <c>_period</c>의 격자로 반복시켜 끝없는 바닥처럼 보이게 한다.
    /// 각 자식은 카메라를 중심으로 한 주기 폭의 창 안으로 주기 단위만큼만 옮겨지므로,
    /// 되돌아가면 같은 자리에 같은 소품이 있다.
    ///
    /// 큰 타일맵을 까는 대신 이렇게 한 것은 판이 끝없이 이어지는 구조라 어디까지 깔아도
    /// 끝이 있고, 넓게 깔수록 씬 파일만 커지기 때문이다.
    /// </summary>
    public sealed class WrapAroundCamera : MonoBehaviour
    {
        [Title("반복")]
        [SerializeField, LabelText("반복 주기 (unit)"), Min(1f)] private float _period = 32f;

        private Transform _camera;
        private Transform[] _children;
        private Vector2[] _origins;

        private void Awake()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError($"[{nameof(WrapAroundCamera)}] 메인 카메라를 찾지 못했다. 배경이 따라오지 않는다.", this);
                return;
            }

            _camera = cam.transform;

            _children = new Transform[transform.childCount];
            _origins = new Vector2[_children.Length];
            for (int i = 0; i < _children.Length; i++)
            {
                _children[i] = transform.GetChild(i);
                _origins[i] = _children[i].position;
            }
        }

        // 창 가장자리가 카메라에서 주기의 절반만큼 떨어져 있어, 주기가 화면보다 넓으면 옮기는 순간이 화면 밖이다.
        private void LateUpdate()
        {
            if (_camera == null) return;

            Vector2 center = _camera.position;
            float half = _period * 0.5f;

            for (int i = 0; i < _children.Length; i++)
            {
                Vector2 origin = _origins[i];
                float x = center.x + Mathf.Repeat(origin.x - center.x + half, _period) - half;
                float y = center.y + Mathf.Repeat(origin.y - center.y + half, _period) - half;

                Transform child = _children[i];
                child.position = new Vector3(x, y, child.position.z);
            }
        }
    }
}
