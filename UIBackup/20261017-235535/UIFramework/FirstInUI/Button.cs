using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Button : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    public Cursor cursorLeft;
    public Cursor cursorRight;
    Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    public void OnClick()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
        {
            //SceneManager.LoadScene(next);
        }
        else
        {
            // 选择循环回到第一个场景或记录错误
            //SceneManager.LoadScene(0);
            // 或者：Debug.LogWarning("没有更多场景可加载");
        }
    }
    // 处理鼠标悬停事件
    public void OnPointerEnter(PointerEventData eventData)
    {
        cursorLeft.SetBtnState(false);
        cursorRight.SetBtnState(false);
    }
    // 处理鼠标离开事件
    public void OnPointerExit(PointerEventData eventData)
    {
        cursorLeft.SetBtnState(true);
        cursorRight.SetBtnState(true);
    }
}
