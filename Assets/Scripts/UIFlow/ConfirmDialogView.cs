using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ConfirmDialogView : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text messageLabel;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    public event Action Confirmed;
    public event Action Cancelled;

    private void OnEnable()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(HandleCancelClicked);
        }
    }

    private void OnDisable()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(HandleCancelClicked);
        }
    }

    public void Show(
        string title,
        string message)
    {
        if (titleLabel != null)
        {
            titleLabel.text = title;
        }

        if (messageLabel != null)
        {
            messageLabel.text = message;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void HandleConfirmClicked()
    {
        Confirmed?.Invoke();
    }

    private void HandleCancelClicked()
    {
        Cancelled?.Invoke();
    }
}
