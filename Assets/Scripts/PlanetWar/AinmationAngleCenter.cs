using UnityEngine;
using UnityEngine.UI;

public sealed class AinmationAngleCenter : MonoBehaviour
{
    public float radius = 50f;
    public float angularSpeed = 2f;
    public bool forceIgnoreLayout = true;
    private Vector3 originLocalPosition;
    private float theta;
    private RectTransform rectTransform;
    private LayoutElement layoutElement;

    private void OnEnable()
    {
        rectTransform = transform as RectTransform;
        originLocalPosition = transform.localPosition;
        theta = 0f;
        if (rectTransform != null && forceIgnoreLayout)
        {
            layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
        }
    }

    private void OnDisable()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originLocalPosition;
            if (layoutElement != null) layoutElement.ignoreLayout = false;
        }
        else transform.localPosition = originLocalPosition;
    }

    private void Update()
    {
        theta += angularSpeed * Time.deltaTime;
        Vector3 next = originLocalPosition + new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0f) * radius;
        if (rectTransform != null) rectTransform.anchoredPosition = next;
        else transform.localPosition = next;
    }
}
