using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class QuitButtonController : MonoBehaviour
{

    private void Awake()
    {
        Button quitButton = GetComponent<Button>();
        quitButton.gameObject.SetActive(false);

#if UNITY_STANDALONE_WIN
        quitButton.gameObject.SetActive(true);
        quitButton.onClick.AddListener(QuitApplication);
#endif
    }

    private void QuitApplication()
    {
#if UNITY_STANDALONE_WIN
        Application.Quit();
#endif
    }
}
