using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ItemView отвечает ТОЛЬКО за визуальное представление предмета
// Анимации, прозрачность, иконки - все здесь
public class ItemView : MonoBehaviour
{
    [Header("Компоненты")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private RectTransform iconRect;

    [Header("Настройки анимации")]
    [SerializeField] private float moveDuration = 0.15f;
    [SerializeField] private float rotationDuration = 0.2f;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float completeRotationDuration = 0.1f;

    private Coroutine activeCoroutine;
    private Coroutine rotationCompletionCoroutine;
    private Quaternion? pendingRotation = null;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    // Обновление иконки предмета
    public void UpdateIcon(Sprite sprite, bool rotateIfVertical = false)
    {
        if (iconImage == null || sprite == null) return;

        iconImage.sprite = sprite;

        // Автоматический поворот для вертикальных спрайтов
        if (rotateIfVertical && iconRect != null)
        {
            if (iconRect.sizeDelta.y > iconRect.sizeDelta.x)
            {
                iconRect.localRotation = Quaternion.Euler(0, 0, 90);
                iconRect.sizeDelta = new Vector2(iconRect.sizeDelta.y, iconRect.sizeDelta.x);
            }
        }
    }

    // Очистка иконки
    public void ClearIcon()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
        }
    }

    // Начало визуализации перетаскивания
    public void StartDragVisuals()
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
        }
        transform.SetAsLastSibling();
    }

    // Конец визуализации перетаскивания
    public void EndDragVisuals()
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1.0f;
        }
    }

    // Плавное перемещение к целевой позиции
    public void SmoothMoveTo(Vector3 targetLocalPosition)
    {
        StopActiveCoroutine();
        activeCoroutine = StartCoroutine(SmoothMoveCoroutine(targetLocalPosition));
    }

    // Плавный поворот
    public void SmoothRotateTo(Quaternion targetRotation)
    {
        pendingRotation = targetRotation;
        StopActiveCoroutine();
        activeCoroutine = StartCoroutine(SmoothRotateCoroutine(targetRotation));
    }

    // Эффект тряски (при неудачном размещении)
    public void PlayShakeEffect(System.Action onComplete = null)
    {
        StopActiveCoroutine();
        activeCoroutine = StartCoroutine(ShakeCoroutine(onComplete));
    }

    // Мгновенная установка позиции
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    // Мгновенная установка локальной позиции
    public void SetLocalPosition(Vector3 localPosition)
    {
        transform.localPosition = localPosition;
    }

    // Установка родителя
    public void SetParent(Transform parent)
    {
        transform.SetParent(parent);
    }

    // --- Корутины для анимаций ---

    private IEnumerator SmoothMoveCoroutine(Vector3 targetLocalPos)
    {
        Vector3 startLocalPos = transform.localPosition;
        float elapsed = 1;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            yield return null;
        }

        transform.localPosition = targetLocalPos;
        activeCoroutine = null;
    }

    private IEnumerator SmoothRotateCoroutine(Quaternion targetRotation)
    {
        Quaternion startRotation = transform.rotation;
        float elapsed = 0;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
        pendingRotation = null;
        activeCoroutine = null;
    }

    private IEnumerator ShakeCoroutine(System.Action onComplete)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0;

        while (elapsed < shakeDuration)
        {
            float offset = Mathf.Sin(elapsed * 50f) * 10f;
            transform.position = new Vector3(originalPos.x + offset, originalPos.y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
        activeCoroutine = null;
        onComplete?.Invoke();
    }

    private void StopActiveCoroutine()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
            
            // Плавно завершить незавершенный поворот
            if (pendingRotation.HasValue)
            {
                if (rotationCompletionCoroutine != null)
                {
                    StopCoroutine(rotationCompletionCoroutine);
                }
                rotationCompletionCoroutine = StartCoroutine(CompleteRotationCoroutine(pendingRotation.Value));
                pendingRotation = null;
            }
        }
    }

    private IEnumerator CompleteRotationCoroutine(Quaternion targetRotation)
    {
        Quaternion startRotation = transform.rotation;
        float elapsed = 0;

        while (elapsed < completeRotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / completeRotationDuration);
            // Используем более быструю интерполяцию для плавного завершения
            float smoothT = t * t * (3f - 2f * t); // SmoothStep
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
            yield return null;
        }

        transform.rotation = targetRotation;
        rotationCompletionCoroutine = null;
    }

    private void OnDestroy()
    {
        StopActiveCoroutine();
        if (rotationCompletionCoroutine != null)
        {
            StopCoroutine(rotationCompletionCoroutine);
            rotationCompletionCoroutine = null;
        }
    }
}
