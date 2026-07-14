using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 같은 GameObject의 Button 클릭 시 지정 씬을 로드한다. 인스펙터 onClick 배선 없이
// 런타임에 리스너를 붙여, 코드/도구로 버튼을 만들 때 배선 부담을 없앤다.
[RequireComponent(typeof(Button))]
public class SceneLoadButton : MonoBehaviour
{
    public string sceneName;

    void Start()
    {
        Button b = GetComponent<Button>();
        if (b != null) b.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(sceneName)) SceneLoader.Load(sceneName);
        });
    }
}
