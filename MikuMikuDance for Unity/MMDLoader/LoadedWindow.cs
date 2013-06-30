#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;

public class LoadedWindow : EditorWindow 
{
	/// <summary>
	/// ���b�Z�[�W�p�e�L�X�g
	/// </summary>
	public string Text { get; set; }

	const int width = 400;

	const int height = 300;

	/// <summary>
	/// ������
	/// </summary>
	/// <returns>�E�B���h�E</returns>
	public static LoadedWindow Init()
	{
		var window = EditorWindow.GetWindow<LoadedWindow>("PMD file loaded!") as LoadedWindow;
		var pos = window.position;
		pos.height = LoadedWindow.height;
		pos.width = LoadedWindow.width;
		window.position = pos;
		return window;
	}

	void OnGUI()
	{
		EditorGUI.TextArea(new Rect(0, 0, LoadedWindow.width, LoadedWindow.height - 30), this.Text);

		if (GUI.Button(new Rect(0, height - 30, LoadedWindow.width, 30), "OK"))
			Close();
	}
}
#endif