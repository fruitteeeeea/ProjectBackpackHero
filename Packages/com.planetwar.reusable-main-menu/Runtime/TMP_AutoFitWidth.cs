using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public class TMP_AutoFitWidth : MonoBehaviour
{
    private TMP_Text _tmpText;
    private RectTransform _rectTransform;
    private string _lastText;

    [Header("适配配置")]
    [Tooltip("宽度额外偏移（避免文本紧贴容器边缘，单位：像素）")]
    public float widthOffset = 4f;  // 可根据需求调整（如 2~8 像素）
    [Tooltip("是否在文本变化时实时适配")]
    public bool fitOnTextChange = true;
    public float minWidth = 0f;
    public float maxWidth = 0f;
    public bool clampToParent = true;

    private void Awake()
    {
        _tmpText = GetComponent<TMP_Text>();
        _rectTransform = GetComponent<RectTransform>();

        _lastText = _tmpText != null ? _tmpText.text : null;
    }

    private void Start()
    {
        // 初始化时适配一次宽度
        FitWidthToText();
    }

    private void OnEnable()
    {
        FitWidthToText();
    }

    /// <summary>
    /// 手动触发宽度适配（外部调用，如动态修改文本后）
    /// </summary>
    public void FitWidthToText()
    {
        if (_tmpText == null)
        {
            return;
        }
        var txt = _tmpText.text;
        if (string.IsNullOrEmpty(txt))
        {
            var w = Mathf.Max(0f, minWidth);
            if (clampToParent && _rectTransform.parent is RectTransform p0)
            {
                w = Mathf.Min(w, p0.rect.width);
            }
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            return;
        }

        // 关键：计算文本的理想宽高（不换行、完全显示的尺寸）
        // 参数：widthConstraint（宽度约束，设为 0 表示无约束，即计算单行最大宽度）
        //      heightConstraint（高度约束，设为 float.MaxValue 表示无约束）
        Vector2 preferredSize = _tmpText.GetPreferredValues(0, float.MaxValue);

        // 设置容器宽度 = 文本理想宽度 + 额外偏移（避免紧贴边缘）
        float targetWidth = preferredSize.x + widthOffset;
        if (maxWidth > 0f) targetWidth = Mathf.Min(targetWidth, maxWidth);
        if (minWidth > 0f) targetWidth = Mathf.Max(targetWidth, minWidth);
        if (clampToParent && _rectTransform.parent is RectTransform parent)
        {
            targetWidth = Mathf.Min(targetWidth, parent.rect.width);
        }
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
    }

    private void LateUpdate()
    {
        if (!fitOnTextChange || _tmpText == null) return;
        var t = _tmpText.text;
        if (!string.Equals(t, _lastText))
        {
            _lastText = t;
            FitWidthToText();
        }
    }

    private void OnDestroy()
    {
    }

    private void OnValidate()
    {
        FitWidthToText();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (clampToParent)
        {
            FitWidthToText();
        }
    }
}
