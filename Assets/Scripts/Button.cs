using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Button : MonoBehaviour {

    // 버튼을 눌렀을때 실행할 메소드 설정
    // 게임 시작 버튼을 눌렀을 때
    public void OnClickStartBtn() {

        // 씬을 전환할 때
        // SceneManager 클래스 사용
        SceneManager.LoadScene("MainScene");
        Debug.Log("시작버튼클릭됨");
        

    }
    // 게임 종료 버튼을 눌렀을때
    public void OnClickExitBtn() {

#if UNITY_EDITOR // 실행시키고 있는 프로그램이 유니티 에디터라면?
        // 실행상태를 해제합니다.
        UnityEditor.EditorApplication.isPlaying = false;

#else   //유니티 에디터가 아니라면
        Application.Quit();
#endif
    }
} 