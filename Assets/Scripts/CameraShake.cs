using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 _originalPos;
    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
      
    }

    // 外部から呼ぶ関数（duration = 揺れる秒数、magnitude = 揺れの激しさ）
    public void Shake(float duration, float magnitude)
    {
        // もし既に揺れている途中に新しくダメージを受けたら、前の揺れをキャンセルして定位置に戻す
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            transform.localPosition = _originalPos;
        }
        else
        {
            _originalPos = transform.localPosition;
        }

        // 新しい揺れをスタート
        _shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // X軸とY軸で、ランダムなズレを計算する
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // カメラの位置を少しずらす
            transform.localPosition = new Vector3(_originalPos.x + x, _originalPos.y + y, _originalPos.z);

            elapsed += Time.deltaTime;
            yield return null; // 次のフレームまで待つ
        }

        // 揺れ終わったら、カメラを元の位置に戻す
        transform.localPosition = _originalPos;
        _shakeCoroutine = null;
    }
}